using System;

namespace Utility
{
    public class LabelRecord
    {
        public string NameRouter { get; set; }
        public short InFEC { get; set; }
        public short OutFEC { get; set; }
        public ushort InPort { get; set; }
        public ushort OutPort { get; set; }
        
        public LabelRecord(string name, short fecin, short fecout, ushort poprtin, ushort portout)
        {
            NameRouter = name;
            InFEC = fecin;
            OutFEC = fecout;
            InPort = poprtin;
            OutPort = portout;
        }

        public LabelRecord(string data)
        {
            string[] parts = data.Split(' ');
            InFEC = Convert.ToInt16(parts[0]);
            InPort = Convert.ToUInt16(parts[1]);
            OutFEC = Convert.ToInt16(parts[2]);
            OutPort = Convert.ToUInt16(parts[3]);
        }
    }
}
