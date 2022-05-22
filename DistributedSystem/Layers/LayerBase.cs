using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Layers
{
    public class LayerBase
    {
        public string MyID { get; set; }
        public List<object> SubscribedToSend
        { get
            {
                return _SubscribedTOSend;
            }
        }
       public List<object> SubscribedToDeliver
        { get
            {
                return _SubscribedToDeliver;
            }
        }

        List<object> _SubscribedTOSend = new List<object>();
        List<object> _SubscribedToDeliver = new List<object>();
    }
}
