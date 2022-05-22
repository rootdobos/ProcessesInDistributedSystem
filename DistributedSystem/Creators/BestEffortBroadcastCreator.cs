using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
using DistributedSystem.Layers;
namespace DistributedSystem.Creators
{
    public class BestEffortBroadcastCreator : IAbstractionCreatable
    {
        public string ID { set { _ID = value; } }

        public IAbstractionable Create()
        {
            return new BestEffortBroadcast();
        }
        private string _ID;
    }
}
