using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Host
{
    public class Hosts
    {
        public string HostName { get; set; }

        public IPAddress IPAddress { get; set; }
        
        public ushort OutPort { get; set; }

        public IPAddress CloudIPAddress { get; set; }

        public ushort CloudPort { get; set; }
        
        public List<DistantHosts> DistantHosts { get; set; }

        public Hosts()
        {
            DistantHosts = new List<DistantHosts>();
        }
    }
}
