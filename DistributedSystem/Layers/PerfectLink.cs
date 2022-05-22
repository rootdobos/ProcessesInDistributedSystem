using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem.Layers
{
    public class PerfectLink : LayerBase, IAbstractionable
    {

        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;

        public PerfectLink()
        {
            MyID = "pl";
        }
        public void Send(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.PlSend)
            {
                string abstractionID = message.FromAbstractionId;


                Message m = new Message();
                m.Type = Message.Types.Type.NetworkMessage;
                m.FromAbstractionId= Utilities.AddMyAbstractionId(abstractionID, MyID);
                m.ToAbstractionId = message.ToAbstractionId;
                m.SystemId = message.SystemId;
                m.NetworkMessage = new NetworkMessage();
                m.NetworkMessage.Message = message.PlSend.Message;
                m.NetworkMessage.Message = message.PlSend.Message;
                m.NetworkMessage.Message = message.PlSend.Message;

                EventHandler<IMessageArgumentable> handler = SendEvent;
                MessageEventArgs args = new MessageEventArgs();
                args.Message = m;
                args.EndHost = message.PlSend.Destination.Host;
                args.EndPort = message.PlSend.Destination.Port; 
                handler?.Invoke(this, args);
            }
        }


        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.NetworkMessage && Utilities.IsMyMessage(message.ToAbstractionId,MyID))
            {
                Message m = new Message();
                m.Type = Message.Types.Type.PlDeliver;
                m.ToAbstractionId = Utilities.RemoveMyAbstractionId(message.ToAbstractionId, MyID);
                m.FromAbstractionId = message.FromAbstractionId;
                m.SystemId = message.SystemId;
                m.PlDeliver = new PlDeliver();
                m.PlDeliver.Sender = new ProcessId();
                m.PlDeliver.Sender.Host = message.NetworkMessage.SenderHost;
                m.PlDeliver.Sender.Port = message.NetworkMessage.SenderListeningPort;
                m.PlDeliver.Message = new Message();
                m.PlDeliver.Message = message.NetworkMessage.Message;

                //message.ToAbstractionId = Utilities.RemoveMyAbstractionId(message.ToAbstractionId,MyID);
                EventHandler<IMessageArgumentable> handler = DeliverEvent;
                MessageEventArgs args = new MessageEventArgs();
                args.Message = m;
                handler?.Invoke(this, args);
            }
        }
       
    }
}
