using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem.Layers
{
    public class BestEffortBroadcast : LayerBase, IAbstractionable
    {


        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;
        public BestEffortBroadcast()
        {
            MyID = "beb";
        }
        public BestEffortBroadcast(List<ProcessId> processes)
        {
            MyID = "beb";
            Init(processes);
        }
        public void Init(List<ProcessId> processes )
        {
            _Processes = processes;
        }

        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.PlDeliver && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                Message m = new Message();
                m.Type = Message.Types.Type.BebDeliver;
                m.ToAbstractionId = Utilities.RemoveMyAbstractionId(message.ToAbstractionId, MyID);
                m.FromAbstractionId = message.FromAbstractionId;
                m.SystemId = message.SystemId;
                m.BebDeliver = new BebDeliver();
                m.BebDeliver.Sender = message.PlDeliver.Sender;
                m.BebDeliver.Message = new Message();
                m.BebDeliver.Message = message.PlDeliver.Message;

                EventHandler<IMessageArgumentable> handler = DeliverEvent;
                MessageEventArgs args = new MessageEventArgs();
                args.Message = m;
                handler?.Invoke(this, args);
            }
        }

        public void Send(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;

            if (message.Type == Message.Types.Type.BebBroadcast)
                Broadcast(message);

            //EventHandler<MessageEventArgs> handler = SendEvent;
            //MessageEventArgs args = new MessageEventArgs();
            //args.Message = message;
            //handler?.Invoke(this, args);
        }

        public void Broadcast(Message message)
        {
            foreach (ProcessId process in _Processes)
            {
                Message m = new Message();
                m.Type = Message.Types.Type.PlSend;
                m.PlSend = new PlSend();
                m.PlSend.Destination = process;
                m.FromAbstractionId = Utilities.AddMyAbstractionId(message.FromAbstractionId, MyID);
                m.ToAbstractionId = message.ToAbstractionId;
                m.SystemId = message.SystemId;
                m.PlSend.Message = message.BebBroadcast.Message;

                EventHandler<IMessageArgumentable> handler = SendEvent;
                MessageEventArgs args = new MessageEventArgs();
                args.Message = m;
                handler?.Invoke(this, args);
            }
        }
        //public void Deliver(Message message)
        //{
        //    throw new NotImplementedException();
        //}

        List<ProcessId> _Processes;
        PerfectLink _PerfectLink;

    }
}
