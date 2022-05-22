using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
using Google.Protobuf.Communication;
using DistributedSystem.Layers;
namespace DistributedSystem.Initializers
{
    public class NNAtomicRegisterInitializer : IInitializer
    {
        public string InitableID
        {
            get { return "nnar"; }
        }
        public NNAtomicRegisterInitializer(List<ProcessId> processes, ProcessId ownerprocess, string systemID)
        {
            _Processes = processes;
            _OwnerProcess = ownerprocess;
            _SystemID = systemID;
        }
        public void Init(IAbstractionable layer)
        {
            ((NNAtomicRegister)layer).Init(_Processes, _OwnerProcess.Rank, _SystemID);
        }

        List<ProcessId> _Processes;
        ProcessId _OwnerProcess;
        string _SystemID;
    }
}
