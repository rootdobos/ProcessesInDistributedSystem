using Google.Protobuf.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem.Layers
{
    public class NNAtomicRegister : LayerBase, IAbstractionable
    {

        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;

        public  NNAtomicRegister(string register)
        {
            _Register = register;
            MyID = "nnar" + "[" + register + "]";
        }

        public void Init(List<ProcessId> processes, int selfRank, string systemID)
        {
            _selfRank = selfRank;
            _ValueStruct = new ValueStruct();
            _Acks = 0;
            _Writeval = 0;
            _RID = 0;
            _ReadList = new ValueStruct[processes.Count];
            for (int i = 0; i < _ReadList.Length; i++)
                _ReadList[i] = null;

            _Readval = 0;
            _Reading = false;
            _SystemID = systemID;
            _Processes = processes;
        }
        public void Read(Message m)
        {
            _RID++;
            _Acks = 0;
            for (int i = 0; i < _ReadList.Length; i++)
                _ReadList[i] = null;
            _Reading = true;

            BEBBroadcastRead();
        }
        public void SendValue(Message m)
        {
            ProcessId receiver = m.BebDeliver.Sender;
            int readID = m.BebDeliver.Message.NnarInternalRead.ReadId;
            Message message = new Message();
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + MyID + ".pl";
            message.SystemId = _SystemID;
            message.Type = Message.Types.Type.PlSend;
            message.PlSend = new PlSend();
            message.PlSend.Destination = receiver;
            message.PlSend.Message = new Message();
            message.PlSend.Message.Type = Message.Types.Type.NnarInternalValue;
            message.PlSend.Message.NnarInternalValue = new NnarInternalValue();
            message.PlSend.Message.FromAbstractionId= "app." + MyID;
            message.PlSend.Message.ToAbstractionId= "app." + MyID;
            message.PlSend.Message.NnarInternalValue.ReadId = readID;
            message.PlSend.Message.NnarInternalValue.Timestamp = _ValueStruct.ts;
            message.PlSend.Message.NnarInternalValue.WriterRank = _ValueStruct.wr;
            message.PlSend.Message.NnarInternalValue.Value = new Value();
            message.PlSend.Message.NnarInternalValue.Value.Defined = true;
            message.PlSend.Message.NnarInternalValue.Value.V = (int)_ValueStruct.val;

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }

        public void DeliverValue(Message m)
        {
            int rid = m.PlDeliver.Message.NnarInternalValue.ReadId;
            int ts = m.PlDeliver.Message.NnarInternalValue.Timestamp;            
            int wr = m.PlDeliver.Message.NnarInternalValue.WriterRank;            
            int val = m.PlDeliver.Message.NnarInternalValue.Value.V;            
            ProcessId sender = m.PlDeliver.Sender;
            if(rid==_RID)
            {
                _ReadList[Utilities.GetIndexByPort(_Processes,sender.Port)] = new ValueStruct(ts, wr, val);
                if(NumberOfNotNullElements(_ReadList)> _ReadList.Length/2.0)
                {
                    ValueStruct max = GetValueStructWithHighestTimeStamp(_ReadList);
                    _Readval = max.val;
                    for (int i = 0; i < _ReadList.Length; i++)
                        _ReadList[i] = null;
                    if (_Reading)
                        BEBBroadcastWrite(rid, max.ts, max.wr, (int)max.val);
                    else
                        BEBBroadcastWrite(rid, max.ts+1,_selfRank, (int)_Writeval);
                }
            }
        }
        public void BEBBroadcastWrite(int rid, int maxts, int rank, int val)
        {
            Message message = new Message();
            //message.SystemId = m.SystemId;
            //message.FromAbstractionId = Utilities.AddMyAbstractionId(message.FromAbstractionId, MyID);
            //message.ToAbstractionId = m.ToAbstractionId;
            message.Type = Message.Types.Type.BebBroadcast;
            message.SystemId = _SystemID;
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + MyID + ".beb.pl";
            message.BebBroadcast = new BebBroadcast();
            message.BebBroadcast.Message = new Message();
            message.BebBroadcast.Message.FromAbstractionId = "app." + MyID;
            message.BebBroadcast.Message.ToAbstractionId = "app." + MyID;
            message.BebBroadcast.Message.Type = Message.Types.Type.NnarInternalWrite;

            message.BebBroadcast.Message.NnarInternalWrite = new NnarInternalWrite();
            message.BebBroadcast.Message.NnarInternalWrite.ReadId = rid;
            message.BebBroadcast.Message.NnarInternalWrite.Timestamp = maxts;
            message.BebBroadcast.Message.NnarInternalWrite.WriterRank = rank;
            message.BebBroadcast.Message.NnarInternalWrite.Value = new Value();
            message.BebBroadcast.Message.NnarInternalWrite.Value.Defined = true;
            message.BebBroadcast.Message.NnarInternalWrite.Value.V = val;


            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }
        public void BEBBroadcastRead()
        {
            Message message = new Message();
            //message.SystemId = m.SystemId;
           // message.FromAbstractionId = Utilities.AddMyAbstractionId(message.FromAbstractionId, MyID);
           // message.ToAbstractionId = m.ToAbstractionId;
            message.Type = Message.Types.Type.BebBroadcast;
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + MyID + ".beb.pl";
            message.SystemId = _SystemID;
            message.BebBroadcast = new BebBroadcast();
            message.BebBroadcast.Message = new Message();
            message.BebBroadcast.Message.FromAbstractionId = "app." + MyID;
            message.BebBroadcast.Message.ToAbstractionId = "app." + MyID;
            message.BebBroadcast.Message.Type = Message.Types.Type.NnarInternalRead;

            message.BebBroadcast.Message.NnarInternalRead = new NnarInternalRead();
            message.BebBroadcast.Message.NnarInternalRead.ReadId = _RID;

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }
        public void Send(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if(message.Type== Message.Types.Type.NnarRead)
            {
                Read(message);
            }
            if(message.Type == Message.Types.Type.NnarWrite)
            {
                Write(message);
            }
        }

        public void Write(Message m)
        {
            _RID++;
            _Writeval = m.NnarWrite.Value.V;
            _Acks = 0;
            for (int i = 0; i < _ReadList.Length; i++)
                _ReadList[i] = null;
            BEBBroadcastRead();
        }
        public void DeliverWrite(Message m)
        {
            int rid=  m.BebDeliver.Message.NnarInternalWrite.ReadId;
            int ts=  m.BebDeliver.Message.NnarInternalWrite.Timestamp;
            int wr=  m.BebDeliver.Message.NnarInternalWrite.WriterRank;
            int val=  m.BebDeliver.Message.NnarInternalWrite.Value.V;
            if (ts > _ValueStruct.ts || (ts == _ValueStruct.ts && wr > _ValueStruct.wr))
                _ValueStruct = new ValueStruct(ts, wr, val);

            ProcessId receiver = m.BebDeliver.Sender;;
            Message message = new Message();
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + MyID + ".pl";
            message.Type = Message.Types.Type.PlSend;
            message.SystemId = _SystemID;
            message.PlSend = new PlSend();
            message.PlSend.Destination = receiver;
            message.PlSend.Message = new Message();
            message.PlSend.Message.FromAbstractionId = "app." + MyID;
            message.PlSend.Message.ToAbstractionId = "app." + MyID;
            message.PlSend.Message.Type = Message.Types.Type.NnarInternalAck;
            message.PlSend.Message.NnarInternalAck = new NnarInternalAck();
            message.PlSend.Message.NnarInternalAck.ReadId = rid;

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }

        public void DeliverAck(Message m)
        {
            if (m.PlDeliver.Message.NnarInternalAck.ReadId == _RID)
            {
                _Acks++;
                if (_Acks > _ReadList.Length / 2.0)
                {
                    _Acks = 0;

                    Message message = new Message();
                    message.FromAbstractionId = MyID;
                    message.ToAbstractionId = "app";
                    message.SystemId = m.SystemId;
                    if (_Reading)
                    {
                        _Reading = false;
                        message.Type = Message.Types.Type.NnarReadReturn;

                        message.NnarReadReturn = new NnarReadReturn();
                        message.NnarReadReturn.Value = new Value();
                        message.NnarReadReturn.Value.Defined = true;
                        message.NnarReadReturn.Value.V = (int)_Readval;
                    }
                    else
                    {
                        message.Type = Message.Types.Type.NnarWriteReturn;

                        message.NnarWriteReturn = new NnarWriteReturn();
                    }
                    EventHandler<IMessageArgumentable> handler = DeliverEvent;
                    MessageEventArgs args = new MessageEventArgs();
                    args.Message = message;
                    handler?.Invoke(this, args);
                }
            }
        }

        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;

            if(message.Type==Message.Types.Type.BebDeliver && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                if (message.BebDeliver.Message.Type == Message.Types.Type.NnarInternalRead)
                    SendValue(message);
                if (message.BebDeliver.Message.Type == Message.Types.Type.NnarInternalWrite)
                    DeliverWrite(message);
            }
            if (message.Type == Message.Types.Type.PlDeliver && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                if (message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalValue)
                    DeliverValue(message);
                if (message.PlDeliver.Message.Type == Message.Types.Type.NnarInternalAck)
                    DeliverAck(message);
            }


            //message.ToAbstractionId = Utilities.RemoveMyAbstractionId(message.ToAbstractionId,MyID);
            //EventHandler<MessageEventArgs> handler = DeliverEvent;
            //MessageEventArgs args = new MessageEventArgs();
            //args.Message = m;
            //handler?.Invoke(this, args);
        }


        string _Register;
        ValueStruct _ValueStruct;
        int _Acks;
        int? _Writeval;
        int _RID;
        ValueStruct[] _ReadList;
        int? _Readval;
        bool _Reading;
        List<ProcessId> _Processes;
        int _selfRank;

        string _SystemID;

        private class ValueStruct
        {
            public ValueStruct(){ }
            public ValueStruct(int _ts, int _wr, int _val)
            {
                ts = _ts;
                wr = _wr;
                val = _val;
            }
            public int ts = 0;
            public int wr = 0;
            public int? val = 0;
        }
        private int NumberOfNotNullElements(ValueStruct[] list)
        {
            int number = 0;
            foreach(ValueStruct val in list)
            {
                if (val != null)
                    number++;
            }
            return number;
        }
        private ValueStruct GetValueStructWithHighestTimeStamp(ValueStruct[] list)
        {
            ValueStruct highest = new ValueStruct();
            foreach(ValueStruct element in list)
            {   
                if(element!=null)
                if(element.ts> highest.ts || (element.ts==highest.ts && element.wr>highest.wr))
                {
                    highest = element;
                }
            }
            return highest;
        }
    }
}
