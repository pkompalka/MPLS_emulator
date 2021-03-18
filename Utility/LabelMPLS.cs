using System;
using System.Collections.Generic;

namespace Utility
{
    public class LabelMPLS
    {
        public short Number { get; set; }      
        public byte TimeToLive { get; set; }

        public LabelMPLS()
        {
        }

        public LabelMPLS(short number)
        {
            Number = number;
        }
        
        public List<byte> LabelToBytes()
        {
            List<byte> labelBytes = new List<byte>();

            labelBytes.AddRange(BitConverter.GetBytes(Number));
            labelBytes.Add(TimeToLive);
            return labelBytes;
        }
        
        public LabelMPLS BytesToLabel(byte[] bytes)
        {
            LabelMPLS labelB = new LabelMPLS();

            var tmpBytes = new List<byte>
            {
                bytes[0],
                bytes[1]
            };

            var arrayBytes = tmpBytes.ToArray();
            labelB.Number = BitConverter.ToInt16(arrayBytes, 0);
            labelB.TimeToLive = bytes[2];

            return labelB;
        }
    }
}