
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiLayerCommunication.Interfaces;
using DistributedSystem.Layers;
namespace DistributedSystem.UniqueNameSetters
{
    public class EventuallyPerfectFailureDetectorUniqueNameSetter : IUniqueIDSetter
    {
        public void Set(IAbstractionable layer, string[] ids)
        {
            if (layer is EventuallyPerfectFailureDetector)
            {
                string id = "";
                int i = 0;
                while (ids[i] != "epfd")
                {
                    id += ids[i] + ".";
                    i++;
                }
                id += "epfd";

                ((EventuallyPerfectFailureDetector)layer).MyID = id;
            }
        }
    }
}
