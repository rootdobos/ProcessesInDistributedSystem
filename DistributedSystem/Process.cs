using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
using MultiLayerCommunication;
using MultiLayerCommunication.Interfaces;
using DistributedSystem.Creators;
namespace DistributedSystem
{
    public class Process:IProcess
    {

        public event EventHandler<MessageEventArgs> DeliverEvent;
        public event EventHandler<MessageEventArgs> SendEvent;
        public List<object> Subscribed
        {
            get
            {
                return _Subscribed;
            }
        }
        public List<object> Subscriptions
        {
            get
            {
                return _Subscriptions;
            }
        }

        public Process(string HubIp, int hubPort,string MyIp,  string owner,int listeningport, int index)
        {
             ListeningPort = listeningport;
             Index = index;
             Owner = owner;
            HubIP = HubIp;
            HubPort = hubPort;
             EventQueue = new Queue<Message>();
            _TCPCommunicator = new TCPCommunicator(index, HubIp, hubPort, MyIp, listeningport);
            //_Executor = new PipelineExecutor(this,_TCPCommunicator);
            InitAbstractionFactory();
            InitDescriptor();
            CommunicationSystem basesystem = new CommunicationSystem("base", this, _TCPCommunicator, _AbstractionFactory,_Descriptor);
            _Systems = new Dictionary<string, CommunicationSystem>();
            _Systems.Add("base", basesystem);
            //_TCPCommunicator.SetExecutor(_Executor);

        }
        private void InitAbstractionFactory()
        {
            _AbstractionFactory = new AbstractionFactory();
            _AbstractionFactory.RegisterCreator("pl",new PerfectLinkCreator());
            _AbstractionFactory.RegisterCreator("beb", new BestEffortBroadcastCreator());
            _AbstractionFactory.RegisterCreator("nnar", new NNAtomicRegisterCreator());
            _AbstractionFactory.RegisterCreator("epfd", new EventuallyPerfectFailureDetectorCreator());
            _AbstractionFactory.RegisterCreator("eld", new EventualLeaderDetectorCreator());
            _AbstractionFactory.RegisterCreator("ec", new EpochChangeCreator());
            _AbstractionFactory.RegisterCreator("uc", new UniformConsensusCreator());
            _AbstractionFactory.RegisterCreator("ep", new EpochConsensusCreator());
        }

        private void InitDescriptor()
        {
            _Descriptor = new PipelineExecutorDescriptor();

            List<IAdditionalPipelineContainable> additionalPipelines = new List<IAdditionalPipelineContainable>();
            additionalPipelines.Add(new AdditionalPipelines.NNAtomicRegisterAdditionalPipelines());
            additionalPipelines.Add(new AdditionalPipelines.UniformConsensusAdditionalPipelines());

            _Descriptor.AddAdditionalPipelineNames(additionalPipelines);
            _Descriptor.AddInternalCreatables(new string[] { "ep[" });
            _Descriptor.AddUniqueLayersName(new string[] { "nnar", "uc" });
            _Descriptor.AddUniqueIDSetter(new UniqueNameSetters.EventuallyPerfectFailureDetectorUniqueNameSetter() );
        }
        private void AddInitializers(List<ProcessId> processes, string systemID,PipelineExecutor executor)
        {
            _Descriptor.ClearInitializers();
            _Descriptor.AddInitializer(new Initializers.BestEffortBroadcastInitializer(processes));
            _Descriptor.AddInitializer(new Initializers.EpochChangeInitializer(processes, OwnerProcessId, systemID));
            _Descriptor.AddInitializer(new Initializers.EventualLeaderDetectorInitializer(processes, systemID));
            _Descriptor.AddInitializer(new Initializers.EventuallyPerfectFailureDetectorInitializer(processes,  systemID));
            _Descriptor.AddInitializer(new Initializers.NNAtomicRegisterInitializer(processes, OwnerProcessId, systemID));
            _Descriptor.AddInitializer(new Initializers.UniformConsensusInitializer(processes, OwnerProcessId, systemID, executor));
        }
        public void Run()
        {
            Message message = new Message();

            message.Type = Message.Types.Type.PlSend;

            message.PlSend = new PlSend();
            message.PlSend.Destination = new ProcessId();
            message.PlSend.Destination.Host = HubIP;
            message.PlSend.Destination.Port = HubPort;
            message.FromAbstractionId = MyID;

            Message registrationMessage = MessageComposer.ProcessRegistrationMessage(ListeningPort, Owner, Index);
            message.PlSend.Message = registrationMessage;
            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = message;
            CommunicationSystem basesystem = _Systems["base"];
            basesystem.Send("app.pl", eventArgs);
            while (true)
            {
                Thread.Sleep(10);
            }
        }

        private  void ProcessMessage(Message message , bool fromMe=false)
        {
            if (Utilities.IsMyMessage(message.ToAbstractionId, MyID) || fromMe == true)
                {
                switch (message.Type)
                {
                    case Message.Types.Type.BebDeliver:
                        ProcessBebDeliver(message);
                        break;
                    case Message.Types.Type.PlDeliver:
                        ProcessPlDeliver(message);
                        break;
                    case Message.Types.Type.NetworkMessage:
                        ProcessNetworkMessage(message);
                        break;
                    case Message.Types.Type.ProcInitializeSystem:
                        ProcessInitializeSystemMessage(message);
                        break;
                    case Message.Types.Type.ProcDestroySystem:
                        ProcessDestroySystemMessage(message);
                        break;
                    case Message.Types.Type.AppValue:
                        ProcessAppvalue(message);
                        break;
                    case Message.Types.Type.AppBroadcast:
                        AppBroadcast(message);
                        break;
                    case Message.Types.Type.AppRead:
                        ProcessAppRead(message);
                        break;
                    case Message.Types.Type.AppWrite:
                        ProcessAppWrite(message);
                        break;
                    case Message.Types.Type.NnarReadReturn:
                        ProcessNNARReadReturn(message);
                        break;
                    case Message.Types.Type.NnarWriteReturn:
                        ProcessNNARWriteReturn(message);
                        break;
                    case Message.Types.Type.AppPropose:
                        ProcessAppPropose(message);
                        break;
                    case Message.Types.Type.UcDecide:
                        ProcessUcDecide(message);
                        break;
                    default:
                        Console.WriteLine("Unknown message type");
                        break;
                }
            }
        }

        public void SubscribeToDeliver(object sender, IMessageArgumentable args)
        {
            if (((MessageEventArgs)args).Message.SystemId == String.Empty)
                ((MessageEventArgs)args).Message.SystemId = "base";
            ProcessMessage(((MessageEventArgs)args).Message);
        }
        private void ProcessPlDeliver(Message m)
        {
            m.PlDeliver.Message.SystemId = m.SystemId;
            ProcessMessage(m.PlDeliver.Message,true);
        }
        private void ProcessBebDeliver(Message m)
        {
            m.BebDeliver.Message.SystemId = m.SystemId;
            ProcessMessage(m.BebDeliver.Message, true);
        }
        private  void ProcessNetworkMessage(Message m)
        {
            m.NetworkMessage.Message.SystemId = m.SystemId;
            ProcessMessage(m.NetworkMessage.Message, true);
        }
        private  void ProcessInitializeSystemMessage(Message m)
        {
            List<ProcessId>  processes = new List<ProcessId>();
            string systemID = m.SystemId;
            foreach (ProcessId process in m.ProcInitializeSystem.Processes)
            {
                processes.Add(process);
                if (process.Port == ListeningPort)
                {
                    OwnerProcessId = process;
                }
            }
            CommunicationSystem system = new CommunicationSystem(systemID, this, _TCPCommunicator, _AbstractionFactory, _Descriptor);
            AddInitializers(processes,system.SystemID,system.Executor);
            _Systems.Add(systemID, system);
            Console.WriteLine( "{0} CommunicationSystem Initialization received",  Index);
            // _System
        }
        private void ProcessDestroySystemMessage(Message m)
        {
            string systemID = m.SystemId;
            _Systems.Remove(systemID);
        }
        private void ProcessAppPropose(Message m)
        {
            string systemID = m.SystemId;

            string topicName = m.AppPropose.Topic;
            int value = m.AppPropose.Value.V;

            Console.WriteLine();
            Message message = new Message();
            message.SystemId = systemID;
            message.FromAbstractionId = MyID;
            message.Type = Message.Types.Type.UcPropose;
            message.UcPropose = new UcPropose();
            message.UcPropose.Value = new Value();
            message.UcPropose.Value.V = value;
            message.UcPropose.Value.Defined = true;

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = message;
            Console.WriteLine("App Received Rank: "+OwnerProcessId.Rank +" Propose Value: " + message.UcPropose.Value.V);
            string pipelineID = "app.uc[" + topicName + "].ec.pl";


            CommunicationSystem system = _Systems[systemID];
            system.Send(pipelineID, eventArgs);
        }
        private void AppBroadcast(Message m)
        {
            string systemID = m.SystemId;
            int value= m.AppBroadcast.Value.V;
            Console.WriteLine("{0} Broadcasting {1}", Index,value);

            Message message = new Message();
            message.SystemId = systemID;
            message.FromAbstractionId = MyID;
            message.ToAbstractionId = "app.beb.pl";
            message.Type = Message.Types.Type.BebBroadcast;
            message.BebBroadcast = new BebBroadcast();
            message.BebBroadcast.Message = new Message();
            Message appValue = MessageComposer.AppValue(value, MyID, "app");
            message.BebBroadcast.Message = appValue;
           MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = message;

            CommunicationSystem system= _Systems[systemID];
            system.Send("app.beb.pl", eventArgs);
        }
        private void ProcessAppvalue(Message m)
        {
            string systemID = m.SystemId;

            int receivedValue = m.AppValue.Value.V;
            Console.WriteLine("{0} Received value:{1}", Index, receivedValue);
            Message reply = new Message();
            reply.SystemId = systemID;
            reply.Type = Message.Types.Type.PlSend;
            reply.PlSend = new PlSend();
            reply.PlSend.Destination = new ProcessId();
            reply.PlSend.Destination.Host = HubIP;
            reply.PlSend.Destination.Port = HubPort;
            reply.FromAbstractionId = MyID;


            Message appValue = MessageComposer.AppValue(receivedValue, MyID, "hub");


            reply.PlSend.Message = appValue;

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = reply;

            CommunicationSystem system = _Systems[systemID];
            system.Send("app.pl", eventArgs);

        }
        private void ProcessNNARWriteReturn(Message m)
        {
            string systemID = m.SystemId;

            string register = Utilities.GetRegisterName(m.FromAbstractionId);
            Console.WriteLine("{0} Register {1} write finished", Index, register);
            Message messageTOhub = new Message();
            messageTOhub.SystemId = systemID;
            messageTOhub.Type = Message.Types.Type.PlSend;
            messageTOhub.PlSend = new PlSend();
            messageTOhub.PlSend.Destination = new ProcessId();
            messageTOhub.PlSend.Destination.Host = HubIP;
            messageTOhub.PlSend.Destination.Port = HubPort;
            messageTOhub.FromAbstractionId = MyID;


            Message appWriteReturn = new Message();
            appWriteReturn.FromAbstractionId = MyID;
            appWriteReturn.ToAbstractionId = "hub";
            appWriteReturn.Type = Message.Types.Type.AppWriteReturn;
            appWriteReturn.AppWriteReturn = new AppWriteReturn();
            appWriteReturn.AppWriteReturn.Register = register;


            messageTOhub.PlSend.Message = appWriteReturn;

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = messageTOhub;

            CommunicationSystem system = _Systems[systemID];
            system.Send("app.pl", eventArgs);
        }
        private void ProcessNNARReadReturn(Message m)
        {
            string systemID = m.SystemId;

            int readValue= m.NnarReadReturn.Value.V;
            string register = Utilities.GetRegisterName(m.FromAbstractionId);
            Console.WriteLine("{0} Read value:{1} from register {2}", Index, readValue,register);
            Message messageTOhub = new Message();
            messageTOhub.SystemId = systemID;
            messageTOhub.Type = Message.Types.Type.PlSend;
            messageTOhub.PlSend = new PlSend();
            messageTOhub.PlSend.Destination = new ProcessId();
            messageTOhub.PlSend.Destination.Host = HubIP;
            messageTOhub.PlSend.Destination.Port = HubPort;
            messageTOhub.FromAbstractionId = MyID;


            Message appReadReturn = new Message();
            appReadReturn.FromAbstractionId = MyID;
            appReadReturn.ToAbstractionId = "hub";
            appReadReturn.Type = Message.Types.Type.AppReadReturn;
            appReadReturn.AppReadReturn = new AppReadReturn();
            appReadReturn.AppReadReturn.Register = register;
            appReadReturn.AppReadReturn.Value = new Value();
            appReadReturn.AppReadReturn.Value.Defined = true;
            appReadReturn.AppReadReturn.Value.V = readValue;


            messageTOhub.PlSend.Message = appReadReturn;

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = messageTOhub;

            CommunicationSystem system = _Systems[systemID];
            system.Send("app.pl", eventArgs);
        }
        private void ProcessAppRead(Message m)
        {
            string systemID = m.SystemId;

            string register=m.AppRead.Register;
            Console.WriteLine("{0} Starts reading register {1}", Index, register);
            Message message = new Message();
            message.Type = Message.Types.Type.NnarRead;
            message.NnarRead = new NnarRead();
            string pipelineID = "app.nnar[" + register + "].beb.pl";

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = message;

            CommunicationSystem system = _Systems[systemID];
            system.Send(pipelineID, eventArgs);
        }
        private void ProcessAppWrite(Message m)
        {

            string systemID = m.SystemId;

            string register = m.AppWrite.Register;
            int value = m.AppWrite.Value.V;

            Console.WriteLine("{0} Starts writing value {1} in register {2}", Index,value, register);
            Message message = new Message();
            message.Type = Message.Types.Type.NnarWrite;
            message.NnarWrite = new NnarWrite();
            message.NnarWrite.Value = new Value();
            message.NnarWrite.Value.Defined = true;
            message.NnarWrite.Value.V = value;
            string pipelineID = "app.nnar[" + register + "].beb.pl";

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = message;

            CommunicationSystem system = _Systems[systemID];
            system.Send(pipelineID, eventArgs);
        }
        private void ProcessUcDecide(Message m)
        {
            string systemID = m.SystemId;

            //string register = m.AppWrite.Register;
            int value = m.UcDecide.Value.V;

            //Console.WriteLine("{0} Starts writing value {1} in register {2}", Index, value, register);
            Message messageTOhub = new Message();
            messageTOhub.SystemId = systemID;
            messageTOhub.Type = Message.Types.Type.PlSend;
            messageTOhub.PlSend = new PlSend();
            messageTOhub.PlSend.Destination = new ProcessId();
            messageTOhub.PlSend.Destination.Host = HubIP;
            messageTOhub.PlSend.Destination.Port = HubPort;
            messageTOhub.FromAbstractionId = MyID;


            Message appDecide = new Message();
           appDecide.FromAbstractionId = MyID;
           appDecide.ToAbstractionId = "hub";
           appDecide.Type = Message.Types.Type.AppDecide;
           appDecide.AppDecide = new AppDecide();
           appDecide.AppDecide.Value = new Value();
           appDecide.AppDecide.Value.Defined = true;
            appDecide.AppDecide.Value.V = value;


            messageTOhub.PlSend.Message = appDecide;

            MessageEventArgs eventArgs = new MessageEventArgs();
            eventArgs.Message = messageTOhub;

            CommunicationSystem system = _Systems[systemID];
            system.Send("app.pl", eventArgs);
        }
        private Dictionary<string,CommunicationSystem> _Systems;
        public Queue<Message> EventQueue;
        TCPCommunicator _TCPCommunicator;
        private static readonly object _Locker = new object();
        private static readonly string MyID = "app";

        public string HubIP;
        public int HubPort;
        public int ListeningPort;
        public int Index;
        public string Owner;
        public ProcessId OwnerProcessId;
        List<object> _Subscribed = new List<object>();
        List<object> _Subscriptions = new List<object>();

        private AbstractionFactory _AbstractionFactory;
        private PipelineExecutorDescriptor _Descriptor;
    }
}
