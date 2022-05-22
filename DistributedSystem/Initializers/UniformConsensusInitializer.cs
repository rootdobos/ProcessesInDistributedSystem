using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication;
using MultiLayerCommunication.Interfaces;
using Google.Protobuf.Communication;
using DistributedSystem.Layers;
namespace DistributedSystem.Initializers
{
    public class UniformConsensusInitializer : IInitializer
    {
        public string InitableID
        {
            get { return "uc"; }
        }
        public UniformConsensusInitializer(List<ProcessId> processes, ProcessId ownerprocess, string systemID, PipelineExecutor executor)
        {
            _Processes = processes;
            _OwnerProcess = ownerprocess;
            _SystemID = systemID;
            _Executor = executor;
        }
        public void Init(IAbstractionable layer)
        {
            ((UniformConsensus)layer).Init(Utilities.MaxRank(_Processes), _OwnerProcess, _Processes.Count, _Executor, _SystemID);
        }

        List<ProcessId> _Processes;
        ProcessId _OwnerProcess;
        string _SystemID;
        PipelineExecutor _Executor;
    }
}
