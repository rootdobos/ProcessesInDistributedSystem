using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem.Layers
{
    public class EpochConsensus : LayerBase, IAbstractionable
    {

        public string OwnerUC;

        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;

        public EpochConsensus(string epoch)
        {
            _EpochTS = int.Parse(epoch);
            MyID = "ep" + "[" + epoch + "]";
        }

        public void Init(StateStruct state, int n, string systemID, int epochTimeStamp)
        {
            _ValState = state;
            //_States = new StateStruct[n];
            _States = new Dictionary<string, StateStruct>();
            _N = n;
            _Accepted = 0;
            _SystemID = systemID;
            _EpochTS = epochTimeStamp;
            _Tmpval = new Value();
            _Tmpval.Defined = false;
        }



        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.BebDeliver && Utilities.IsMyMessage(message.BebDeliver.Message.ToAbstractionId, MyID))
            {
                if (message.BebDeliver.Message.Type == Message.Types.Type.EpInternalRead)
                    PlSendState(message.BebDeliver.Sender);
                else if(message.BebDeliver.Message.Type == Message.Types.Type.EpInternalWrite)
                {
                    _ValState = new StateStruct(_EpochTS, message.BebDeliver.Message.EpInternalWrite.Value.V);
                    SendAccept(message.BebDeliver.Sender);
                }
                else if (message.BebDeliver.Message.Type == Message.Types.Type.EpInternalDecided)
                {
                    DeliverDecided(message.BebDeliver.Message.EpInternalDecided.Value.V);
                }
            }
            else if (message.Type == Message.Types.Type.PlDeliver && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {

                if (message.PlDeliver.Message.Type == Message.Types.Type.EpInternalState)   //leader
                {
                    string key = message.PlDeliver.Sender.Host + ":" + message.PlDeliver.Sender.Port ;
                    //Console.WriteLine("Received State: " + message.PlDeliver.Message.EpInternalState.Value.V+ " From: "+ key + " With Rank: "+ message.PlDeliver.Sender.Rank);
                    if(!_States.ContainsKey(key))
                    {
                        _States.Add(key, new StateStruct());
                    }
                    _States[key] = new StateStruct();
                    _States[key].Val.V = message.PlDeliver.Message.EpInternalState.Value.V;
                    _States[key].Val.Defined = message.PlDeliver.Message.EpInternalState.Value.Defined;
                    _States[key].ValTs = message.PlDeliver.Message.EpInternalState.ValueTimestamp;
                    CheckStates();

                }
                if (message.PlDeliver.Message.Type == Message.Types.Type.EpInternalAccept)   //leader
                {
                    _Accepted++;
                    CheckAccepted();

                }
            }
        }
        public void DeliverDecided(int value)
        {
            Message message = new Message();
            message.Type = Message.Types.Type.EpDecide;
            //from, to s ilyenek
            message.FromAbstractionId = MyID;
            message.EpDecide = new EpDecide();
            message.EpDecide.Ets = _ValState.ValTs;
            message.EpDecide.Value = new Value();
            message.EpDecide.Value.V = value;
            message.EpDecide.Value.Defined = true;


            EventHandler<IMessageArgumentable> handler = DeliverEvent ;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }
        public void SendAccept(ProcessId process)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.PlSend;
            m.PlSend = new PlSend();
            m.PlSend.Destination = process;
            m.FromAbstractionId = "app."+OwnerUC+"." + MyID;
            m.ToAbstractionId = "app." + OwnerUC + "." + MyID +".pl";
            m.SystemId = _SystemID;
            m.PlSend.Message = new Message();
            m.PlSend.Message.Type = Message.Types.Type.EpInternalAccept;
            m.PlSend.Message.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            m.PlSend.Message.ToAbstractionId = "app." + OwnerUC + "." + MyID;
            m.PlSend.Message.EpInternalAccept = new EpInternalAccept();

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }
        public void CheckAccepted()
        {
            if(_Accepted>_N/2+1)
            {
                _Accepted = 0;
                BebBroadcastDecided();
            }

        }
        public void BebBroadcastDecided()
        {
            Message m = new Message();
            m.Type = Message.Types.Type.BebBroadcast;
            m.BebBroadcast = new BebBroadcast();
            m.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            m.ToAbstractionId = "app." + OwnerUC + "." + MyID + ".beb.pl";
            m.SystemId = _SystemID;
            m.BebBroadcast.Message = new Message();
            m.BebBroadcast.Message.Type = Message.Types.Type.EpInternalDecided;

            m.BebBroadcast.Message.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            m.BebBroadcast.Message.ToAbstractionId = "app." + OwnerUC + "." + MyID;

            m.BebBroadcast.Message.EpInternalDecided = new EpInternalDecided();

            m.BebBroadcast.Message.EpInternalDecided.Value = new Value();
            m.BebBroadcast.Message.EpInternalDecided.Value.V = _Tmpval.V;
            m.BebBroadcast.Message.EpInternalDecided.Value.Defined = true;

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }
        public void CheckStates()
        {
            int counter = 0;
            foreach(StateStruct state in _States.Values)
            {
                if (state != null)
                    counter++;
            }
            if(counter>_N/2+1)
            {
                StateStruct highest = HighestState();
                if (highest.Val.Defined)
                {
                    _Tmpval.Defined = true;
                    _Tmpval.V = highest.Val.V;
                }
                _States = new Dictionary<string, StateStruct>();


                Message m = new Message();
                m.Type = Message.Types.Type.BebBroadcast;
                m.BebBroadcast = new BebBroadcast();
                m.FromAbstractionId = "app." + OwnerUC + "." + MyID;
                m.ToAbstractionId = "app." + OwnerUC + "." + MyID + ".beb.pl";
                m.SystemId = _SystemID;
                m.BebBroadcast.Message = new Message();

                m.BebBroadcast.Message.FromAbstractionId = "app." + OwnerUC + "." + MyID;
                m.BebBroadcast.Message.ToAbstractionId = "app." + OwnerUC + "." + MyID;

                m.BebBroadcast.Message.Type = Message.Types.Type.EpInternalWrite;
                m.BebBroadcast.Message.EpInternalWrite = new EpInternalWrite();


                m.BebBroadcast.Message.EpInternalWrite.Value = new Value();
                m.BebBroadcast.Message.EpInternalWrite.Value.V = _Tmpval.V;
                m.BebBroadcast.Message.EpInternalWrite.Value.Defined = true;

                EventHandler<IMessageArgumentable> handler = SendEvent;
                MessageEventArgs args = new MessageEventArgs();
                args.Message = m;
                handler?.Invoke(this, args);


            }
        }
        public StateStruct HighestState()
        {
            StateStruct s = null;
            foreach (StateStruct state in _States.Values)
            {
                if (state != null)
                {
                        s = state;
                    break;
                }
            }
            foreach(StateStruct state in _States.Values)
            {
                if (state != null)
                {
                    if (state.ValTs > s.ValTs)
                        s = state;
                }
            }
            return s;
        }
        public void PlSendState( ProcessId receiver)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.PlSend;
            m.PlSend = new PlSend();
            m.PlSend.Destination = receiver;
            m.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            m.ToAbstractionId = "app." + OwnerUC + "." + MyID + ".pl";
            m.SystemId = _SystemID;
            m.PlSend.Message = new Message();
            m.PlSend.Message.Type = Message.Types.Type.EpInternalState;
            m.PlSend.Message.EpInternalState = new EpInternalState();

            m.PlSend.Message.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            m.PlSend.Message.ToAbstractionId = "app." + OwnerUC + "." + MyID;

            m.PlSend.Message.EpInternalState.Value = new Value();
            m.PlSend.Message.EpInternalState.Value.V = _ValState.Val.V;
            m.PlSend.Message.EpInternalState.Value.Defined = _ValState.Val.Defined;
            m.PlSend.Message.EpInternalState.ValueTimestamp = _ValState.ValTs;

            //Console.WriteLine(MyID + " send state: " + _ValState.Val.V);

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }

        public void Send(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if(message.Type== Message.Types.Type.EpPropose )
            {
                EpPropose(message.EpPropose.Value.V);
            }
            else if (message.Type == Message.Types.Type.EpAbort )
            {
                EpAborted();
            }
        }
        public void EpAborted()
        {
            Message message = new Message();
            message.Type = Message.Types.Type.EpAborted;
            message.FromAbstractionId = MyID;
            //from, to s ilyenek
            message.EpAborted = new EpAborted();
            message.EpAborted.Value = new Value();
            message.EpAborted.Value.V = _ValState.Val.V;
            message.EpAborted.ValueTimestamp = _ValState.ValTs;
            message.EpAborted.Value.Defined = _ValState.Val.Defined;


            EventHandler<IMessageArgumentable> handler = DeliverEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }
        public void EpPropose(int value)    //leader
        {

            _Tmpval.Defined = true;
            _Tmpval.V = value;
            Message message = new Message();

            //Console.WriteLine(MyID+ " Read Internal States");

            message.Type = Message.Types.Type.BebBroadcast;
            message.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            message.ToAbstractionId = "app." + OwnerUC + "." + MyID + ".beb.pl";
            message.SystemId = _SystemID;
            message.BebBroadcast = new BebBroadcast();
            message.BebBroadcast.Message = new Message();
            message.BebBroadcast.Message.FromAbstractionId = "app." + OwnerUC + "." + MyID;
            message.BebBroadcast.Message.ToAbstractionId = "app." + OwnerUC + "." + MyID;
            message.BebBroadcast.Message.Type = Message.Types.Type.EpInternalRead;

            message.BebBroadcast.Message.EpInternalRead = new EpInternalRead();

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }


        StateStruct _ValState;
        Value _Tmpval;
        //StateStruct[] _States;
        Dictionary<string,StateStruct> _States;
        int _Accepted;
        int _N;
        string _SystemID;
        int _EpochTS;
        public class StateStruct
        {
            public int ValTs;
            public Value Val;

            public StateStruct()
            {
                Val = new Value();
            }

            public StateStruct(int v, int ts, bool defined=true)
            {
                
                ValTs = ts;
                Val = new Value();
                Val.V = v;
                Val.Defined = defined;
            }
            public StateStruct Clone()
            {
                return new StateStruct(Val.V, ValTs, Val.Defined);
            }
        }
    }
}
