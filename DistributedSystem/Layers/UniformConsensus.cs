using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;
using MultiLayerCommunication;

namespace DistributedSystem.Layers
{
    public class UniformConsensus :LayerBase, IAbstractionable
    {

        public event EventHandler<IMessageArgumentable> DeliverEvent;
        public event EventHandler<IMessageArgumentable> SendEvent;

        public UniformConsensus(string topic)
        {
             _Topic= topic;
            MyID = "uc" + "[" + _Topic + "]";


        }
        public void Init(ProcessId l0, ProcessId self,int n,PipelineExecutor executor,string systemID)
        {
            

            _NumberOfProcesses = n;
            _Executor = executor;
            _Val = new Value();
            _Val.Defined = false;
           _Proposed = false;
            _Decided = false;
            _ActualEpochLeader = new EpochLeader(0,l0);
            _NewEpochLeader = new EpochLeader(0, null);
            _Self = self;
            _SystemID = systemID;

            string uniqueDepKey = MyID + ".ec";
            IAbstractionable ecLayer = _Executor.Factory.Produce("ec");
            _Executor.UniqueDependencyLayers.Add(uniqueDepKey, ecLayer);
            _Executor.RunInit(ecLayer, "ec");

            AddNewEpochConsensus(0, l0, new EpochConsensus.StateStruct(0, 0, false));
        }
        private void AddNewEpochConsensus(int ets, ProcessId leader, EpochConsensus.StateStruct state)
        {
            string epID = "ep[" + ets.ToString() + "]";
            string uniqueDepKey = MyID +"." +epID;
            IAbstractionable epLayer = _Executor.Factory.Produce(epID);
            if (_Executor.UniqueDependencyLayers.ContainsKey(uniqueDepKey))
            {
                _Executor.UniqueDependencyLayers.Remove(uniqueDepKey);
                _Executor.Pipelines.Remove("app." + MyID + "." + epID + ".beb.pl");
                _Executor.Pipelines.Remove("app." + MyID + "." + epID + ".pl");
            }

            _Executor.UniqueDependencyLayers.Add(uniqueDepKey, epLayer);
            ((EpochConsensus)epLayer).Init(state, _NumberOfProcesses, _SystemID,ets);
            ((EpochConsensus)epLayer).OwnerUC = MyID;
            string pipelineID = "app." + MyID + "." + epID + ".beb.pl";
            _Executor.GeneratePipeline(pipelineID);
            pipelineID = "app." + MyID + "." + epID + ".pl";
            _Executor.GeneratePipeline(pipelineID);

        }
        public void Deliver(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if (message.Type == Message.Types.Type.EcStartEpoch )//&& Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                _NewEpochLeader= new EpochLeader(message.EcStartEpoch.NewTimestamp, message.EcStartEpoch.NewLeader);
                SendEpEtsAbort();
            }
            else if (message.Type == Message.Types.Type.EpAborted && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                int ts = int.Parse(Utilities.GetNameInParantheses(message.FromAbstractionId));

                if (ts == _ActualEpochLeader.EpochTs)
                {
                    _ActualEpochLeader = new EpochLeader(_NewEpochLeader.EpochTs, _NewEpochLeader.Process.Clone());
                    CheckLeader();
                    _Proposed = false;
                    AddNewEpochConsensus(_ActualEpochLeader.EpochTs, _ActualEpochLeader.Process.Clone(),
                        new EpochConsensus.StateStruct( message.EpAborted.Value.V, message.EpAborted.ValueTimestamp, message.EpAborted.Value.Defined));
                    //Initialize a new instance ep.ets of epoch consensus with timestamp ets,
                    //leader l, and state state;
                }
            }
            else if (message.Type == Message.Types.Type.EpDecide && Utilities.IsMyMessage(message.ToAbstractionId, MyID))
            {
                int ts = int.Parse(Utilities.GetNameInParantheses(message.FromAbstractionId));
                if(ts == _ActualEpochLeader.EpochTs && _Decided==false)
                {
                    _Decided = true;

                    Message m = new Message();

                    m.Type = Message.Types.Type.UcDecide;
                    m.FromAbstractionId = "app." + MyID;
                    m.ToAbstractionId = "app";
                    m.SystemId = _SystemID;
                    m.UcDecide = new UcDecide();
                    m.UcDecide.Value = new Value();
                    m.UcDecide.Value.V = message.EpDecide.Value.V;
                    m.UcDecide.Value.Defined = message.EpDecide.Value.Defined;

                    EventHandler<IMessageArgumentable> handler = DeliverEvent;
                    MessageEventArgs args = new MessageEventArgs();
                    args.Message = m;
                    handler?.Invoke(this, args);

                }
            }
        }
        public void SendEpEtsAbort()
        {
            Message message = new Message();

            message.Type = Message.Types.Type.EpAbort;
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + MyID + ".ep["+_ActualEpochLeader.EpochTs+"]";
            message.SystemId = _SystemID;
            message.EpAbort = new EpAbort();


            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }
        public void CheckLeader()
        {
            if(Utilities.AreEqualProcesses(_ActualEpochLeader.Process,_Self) && _Val.Defined && !_Proposed)
            {
                _Proposed = true;
                SendPropose();
                //trigger ep.ets,propose val
            }
        }
        public void SendPropose()
        {
            Message message = new Message();
            message.Type = Message.Types.Type.EpPropose;
            message.FromAbstractionId = "app." + MyID;
            message.ToAbstractionId = "app." + MyID + ".ep[" + _ActualEpochLeader.EpochTs + "]";
            message.SystemId = _SystemID;
            message.EpPropose = new EpPropose();

            message.EpPropose.Value = new Value();
            message.EpPropose.Value.V = _Val.V;
            message.EpPropose.Value.Defined = _Val.Defined;

            EventHandler<IMessageArgumentable> handler = SendEvent;
            MessageEventArgs args = new MessageEventArgs();
            args.Message = message;
            handler?.Invoke(this, args);
        }
        public void Send(object sender, IMessageArgumentable messageArgs)
        {
            Message message = ((MessageEventArgs)messageArgs).Message;
            if(message.Type == Message.Types.Type.UcPropose)
            {
                _Val = message.UcPropose.Value;
                CheckLeader();
            }
        }

        PipelineExecutor _Executor;

        Value _Val;
        bool _Proposed;
        bool _Decided;
        EpochLeader _ActualEpochLeader;
        EpochLeader _NewEpochLeader;
        ProcessId _Self;
        string _Topic;
        int _NumberOfProcesses;
        string _SystemID;
        public class EpochLeader
        {
            public ProcessId Process;
            public int EpochTs;
            public EpochLeader()
            { }
            public EpochLeader( int epochTs, ProcessId process)
            {
                Process = process;
                EpochTs = epochTs;
            }
        }
    }
}
