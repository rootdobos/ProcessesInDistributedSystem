using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;

namespace DistributedSystem.Layers
{
    public class EventualLeaderDetector : LayerBase, IAbstractionable
    {

        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;
        public ProcessId Leader
        {
            get { return _Leader; }
        }

        public EventualLeaderDetector()
        {
            MyID = "eld";
            _Leader = null;
        }
        public void Init(List<ProcessId> processes, string systemID)
        {
            _Processes = processes;
            ProcessId maxRank = Utilities.MaxRank(_Processes);
            _Leader = maxRank;
            _Suspected = new List<ProcessId>();
            _SystemID = systemID;
        }
        public void ChangeLeader()
        {
            List<ProcessId> alive= Utilities.Subtraction(_EPFD.Processes, _EPFD.Suspected);
            ProcessId maxRank = Utilities.MaxRank(alive);
            if(Utilities.AreEqualProcesses(_Leader,maxRank))
            {
                _Leader = maxRank;
                Trust();
            }
            //trigger trust leader
        }
        public void Trust()
        {
            Message message = new Message();
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + "uc." + "ec";
            message.Type = Message.Types.Type.EldTrust;
            message.EldTrust = new EldTrust();
            message.EldTrust.Process = _Leader;

            EventHandler<IMessageArgumentable> handler = DeliverEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);

        }
        public void Send(object sender, IMessageArgumentable messageArgs)
        {
           // throw new NotImplementedException();
        }

        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.EpfdSuspect && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                _Suspected.Add(message.EpfdSuspect.Process);
            }
            else if (message.Type == Message.Types.Type.EpfdRestore && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                _Suspected.Remove(message.EpfdRestore.Process);
            }
        }

        private EventuallyPerfectFailureDetector _EPFD;
        ProcessId _Leader;
        List<ProcessId> _Processes;
        List<ProcessId> _Suspected;
        string _SystemID;

    }
}
