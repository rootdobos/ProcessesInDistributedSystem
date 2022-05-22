using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Communication;
using MultiLayerCommunication.Interfaces;
namespace DistributedSystem
{
    public class MessageEventArgs:EventArgs,IMessageArgumentable
    {
        public Message Message { get; set; }
        public string EndHost { get; set; }
        public int EndPort { get; set; }
    }
}
