using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
using DistributedSystem.Layers;
namespace DistributedSystem.Creators
{
    public class NNAtomicRegisterCreator : IAbstractionCreatable
    {
        public string ID { set { _ID = value; } }

        public IAbstractionable Create()
        {
            string register = Utilities.GetRegisterName(_ID);
            return new NNAtomicRegister(register);
        }
        private string _ID;
    }
}
