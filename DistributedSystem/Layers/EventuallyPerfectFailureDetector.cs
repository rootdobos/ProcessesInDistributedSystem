using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem.Layers
{
    public class EventuallyPerfectFailureDetector : LayerBase, IAbstractionable
    {

        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;
        public EventuallyPerfectFailureDetector()
        {
            MyID = "epfd";
        }
        public EventuallyPerfectFailureDetector(List<ProcessId> processes,string systemID)
        {
            MyID = "epfd";
            Init(processes,systemID);
        }
        public void Init(List<ProcessId> processes, string systemID, int delta=100)
        {
            _Processes = processes;
            _Alive = Utilities.Clone(processes);
            _Suspected = new List<ProcessId>();
            _DeltaTime = delta;
            _Delay = _DeltaTime;
            _SystemID = systemID;
            StartTimer(_Delay);

        }

        private void _Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timeout();
        }

        public void Send(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;


            //EventHandler<MessageEventArgs> handler = SendEvent;
            //MessageEventArgs args = new MessageEventArgs();
            //args.Message = message;
            //handler?.Invoke(this, args);
        }

        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.PlDeliver && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                if(message.PlDeliver.Message.Type== Message.Types.Type.EpfdInternalHeartbeatRequest)
                    SendHeartBeatReply(message.PlDeliver.Sender);
                else if (message.PlDeliver.Message.Type == Message.Types.Type.EpfdInternalHeartbeatReply)
                {
                    lock (LOCKER)
                    {
                        Utilities.CheckIfContainsProcessId(_Alive, message.PlDeliver.Sender);
                        _Alive.Add(message.PlDeliver.Sender);
                    }
                }
            }
        }
        

        public List<ProcessId> Suspected
        { get { return _Suspected; } }
        public List<ProcessId> Processes
        { get { return _Processes; } }

        public EventuallyPerfectFailureDetector(List<ProcessId> processes,int deltaTime=100)
        {
            _Processes = processes;
            _Alive = Utilities.Clone(_Processes);
            _Suspected = new List<ProcessId>();
            _Delay = deltaTime;
            _DeltaTime = deltaTime;
            //starttimer
        }

        public void Timeout()
        {
            lock (LOCKER)
            {
                if (Utilities.IsIntersection(_Alive, _Suspected))
                    _Delay += _DeltaTime;
                foreach (ProcessId process in _Processes)
                {
                    if (!_Alive.Contains(process) && !_Suspected.Contains(process))
                    {
                        _Suspected.Add(process);
                        DeliverSuspected(process);
                    }
                    else if (_Alive.Contains(process) && _Suspected.Contains(process))
                    {
                        _Suspected.Remove(process);
                        DeliverRestored(process);
                    }
                    SendHeartBeatRequest(process);
                }
                _Alive.Clear();
            }
            StartTimer(_Delay);
        }
        public void DeliverSuspected(ProcessId process)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.EpfdSuspect;
            m.EpfdSuspect = new EpfdSuspect();
            m.EpfdSuspect.Process = process;

            EventHandler<IMessageArgumentable> handler = DeliverEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }

        public void DeliverRestored(ProcessId process)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.EpfdRestore;
            m.EpfdRestore = new EpfdRestore();
            m.EpfdRestore.Process = process;

            EventHandler<IMessageArgumentable> handler = DeliverEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }

        private void SendHeartBeatRequest(ProcessId id)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.PlSend;
            m.PlSend = new PlSend();
            m.PlSend.Destination = id;
            m.FromAbstractionId =   MyID;
            m.ToAbstractionId = MyID + ".pl";
            m.SystemId = _SystemID;
            m.PlSend.Message = new Message();
            m.PlSend.Message.Type = Message.Types.Type.EpfdInternalHeartbeatRequest;
            m.PlSend.Message.FromAbstractionId = MyID;
            m.PlSend.Message.ToAbstractionId = MyID;
            m.PlSend.Message.EpfdInternalHeartbeatRequest = new EpfdInternalHeartbeatRequest();

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }
        private void SendHeartBeatReply(ProcessId id)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.PlSend;
            m.PlSend = new PlSend();
            m.PlSend.Destination = id;
            m.FromAbstractionId = MyID;
            m.ToAbstractionId = MyID + ".pl";
            m.SystemId = _SystemID;
            m.PlSend.Message = new Message();
            m.PlSend.Message.Type = Message.Types.Type.EpfdInternalHeartbeatReply;
            m.PlSend.Message.FromAbstractionId = MyID;
            m.PlSend.Message.ToAbstractionId = MyID;
            m.PlSend.Message.EpfdInternalHeartbeatReply = new EpfdInternalHeartbeatReply();

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = m;
            handler?.Invoke(this, args);
        }
        private void StartTimer(int delay)
        {
            _Timer = new Timer(_Delay);
            _Timer.Elapsed += _Timer_Elapsed;
            _Timer.AutoReset = false;
            _Timer.Enabled = true;
        }
        //Deliver heartbeatRequest
        //Deliver HeartbeatReply
        List<ProcessId> _Alive;
        List<ProcessId> _Suspected;
        List<ProcessId> _Processes;
        Timer _Timer;
        int _Delay;
        int _DeltaTime;

        string _SystemID;
        object LOCKER = new object();
    }
}
