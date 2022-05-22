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
    public class EventualLeaderDetectorInitializer : IInitializer
    {
        public string InitableID
        {
            get { return "eld"; }
        }
        public EventualLeaderDetectorInitializer(List<ProcessId> processes, string systemID)
        {
            _Processes = processes;
            _SystemID = systemID;
        }
        public void Init(IAbstractionable layer)
        {
            ((EventualLeaderDetector)layer).Init(_Processes, _SystemID);
        }

        List<ProcessId> _Processes;
        string _SystemID;
    }
}
