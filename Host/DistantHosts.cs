using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;

namespace Host
{
    public class DistantHosts
    {
        public IPAddress DistantIPAddress { get; set; }

        public string DistantName { get; set; }

        public DistantHosts()
        {

        }

        public DistantHosts(IPAddress iphost, string name)
        {
            DistantName = name;
            DistantIPAddress = iphost;
        }

        public override string ToString()
        {
            return $"{DistantName}, {DistantIPAddress.ToString()}";
        }
    }
}
