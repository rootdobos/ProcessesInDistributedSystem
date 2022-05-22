using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
namespace DistributedSystem.AdditionalPipelines
{
    class UniformConsensusAdditionalPipelines:IAdditionalPipelineContainable
    {
        public string BaseID
        {
            get
            {
                return "uc";
            }
        }


        public List<string> AdditionalPipelines(string id)
        {
            List<string> output = new List<string>();
            output.Add("app." + id + ".ec" + ".eld" + ".epfd" + ".pl");
            output.Add("app." + id + ".ec" + ".beb" + ".pl");
            output.Add("app." + id + ".ec" + ".pl");
            return output;
        }
    }
}
