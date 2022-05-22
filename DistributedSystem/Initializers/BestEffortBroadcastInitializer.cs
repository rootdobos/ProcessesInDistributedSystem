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
    public class BestEffortBroadcastInitializer : IInitializer
    {
        public string InitableID
        {
            get { return "beb"; }
        }
        public BestEffortBroadcastInitializer(List<ProcessId> processes)
        {
            _Processes = processes;
        }
        public void Init(IAbstractionable layer)
        {
            ((BestEffortBroadcast)layer).Init(_Processes);
        }

        List<ProcessId> _Processes;
    }
}
