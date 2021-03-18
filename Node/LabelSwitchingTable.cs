using System.Collections.Generic;
using Utility;

namespace Node
{
    public class LabelSwitchingTable
    {
        public List<LabelRecord> Records { get; set; }

        public LabelSwitchingTable()
        {
            Records = new List<LabelRecord>();
        }
    } 
}
