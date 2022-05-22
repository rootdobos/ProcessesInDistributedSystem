using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
namespace DistributedSystem.AdditionalPipelines
{
    class NNAtomicRegisterAdditionalPipelines : IAdditionalPipelineContainable
    {
        public string BaseID
        { get
            {
                return "nnar";
            }
        }


        public List<string> AdditionalPipelines(string id)
        {
            List<string> output = new List<string>();
            output.Add("app." + id + ".pl");
            output.Add("app." + id + ".beb" + ".pl");
            return output;
        }

    }
}
