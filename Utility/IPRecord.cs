using System;
using System.Net;

namespace Utility
{
    public class IPRecord
    {
        public string NameRouter { get; set; }
        public ushort PortOut { get; set; }
        public short FecFirst { get; set; }
        public IPAddress DestinationAddress { get; set; }

        public IPRecord(string name, IPAddress iP, ushort outport, short fec)
        {
            NameRouter = name;
            DestinationAddress = iP;
            PortOut = outport;
            FecFirst = fec;
        }

        public IPRecord(string data)
        {
            string[] parts = data.Split(' ');
            DestinationAddress = IPAddress.Parse(parts[0]);
            PortOut = Convert.ToUInt16(parts[1]);
            FecFirst = Convert.ToInt16(parts[2]);
        }
    }
}
