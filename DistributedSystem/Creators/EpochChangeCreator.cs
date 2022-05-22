using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
using DistributedSystem.Layers;
namespace DistributedSystem.Creators
{
    public class EpochChangeCreator : IAbstractionCreatable
    {
        public string ID { set { _ID = value; } }

        public IAbstractionable Create()
        {
            return new EpochChange();
        }
        private string _ID;
    }
}
