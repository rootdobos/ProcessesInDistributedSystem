using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
namespace DistributedSystem
{
    public  static class MessageComposer
    {
        //public MessageComposer(string owner, int port, int index)
        //{
        //    _ListeningPort = port;
        //    _Index = index;
        //    _Owner = owner;
        //}
        public static Message NetworkMessage(int listeningPort, string owner, int index)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.NetworkMessage;
            m.NetworkMessage = new NetworkMessage();
            m.NetworkMessage.SenderListeningPort = listeningPort;
            m.NetworkMessage.SenderHost = owner;
            return m;
        }

        public static Message  ProcessRegistrationMessage(int listeningPort,string owner, int index)
        {
            Message m = new Message();
            m.Type = Message.Types.Type.ProcRegistration;
            m.ProcRegistration = new ProcRegistration();
            m.ProcRegistration.Owner = owner;
            m.ProcRegistration.Index = index;
            return m;
        }

        public static Message AppValue(int value, string fromid, string toid)
        {
            Message appValue = new Message();
            appValue.Type = Message.Types.Type.AppValue;
            appValue.AppValue = new AppValue();
            appValue.AppValue.Value = new Value();
            appValue.AppValue.Value.Defined = true;
            appValue.AppValue.Value.V = value;
            appValue.FromAbstractionId = fromid;
            appValue.ToAbstractionId = toid;
            return appValue;
        }
        //public static int ListeningPort;
        //public static string Owner;
        //public static int Index;
    }
}
