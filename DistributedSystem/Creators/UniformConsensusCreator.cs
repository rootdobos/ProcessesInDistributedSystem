using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
using DistributedSystem.Layers;
namespace DistributedSystem.Creators
{
    public class UniformConsensusCreator : IAbstractionCreatable
    {
        public string ID { set { _ID = value; } }

        public IAbstractionable Create()
        {
            string topic = Utilities.GetNameInParantheses(_ID);
            return new UniformConsensus(topic);
        }
        private string _ID;
    }
}
