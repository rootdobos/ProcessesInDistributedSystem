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
    public class EpochChangeInitializer : IInitializer
    {
        public string InitableID
        {
            get { return "ec"; }
        }
        public EpochChangeInitializer(List<ProcessId> processes, ProcessId ownerprocess, string systemID)
        {
            _Processes = processes;
            _OwnerProcess = ownerprocess;
            _SystemID = systemID;
        }
        public void Init(IAbstractionable layer)
        {
            ((EpochChange)layer).Init(Utilities.MaxRank(_Processes), _OwnerProcess, _Processes.Count, _SystemID);
        }

        List<ProcessId> _Processes;
        ProcessId  _OwnerProcess;
        string _SystemID;
    }
}
