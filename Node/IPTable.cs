using System.Collections.Generic;
using Utility;

namespace Node
{
    public class IPTable
    {
        public List<IPRecord> Records { get; set; }

        public IPTable()
        {
            Records = new List<IPRecord>();
        }
    }
}