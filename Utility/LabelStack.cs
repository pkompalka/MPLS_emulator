using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    public class LabelStack
    {
        public Stack<LabelMPLS> LabelsStack { get; set; }
        public const byte StackEmptyFlag = 0;
        public const byte StackNotEmptyFlag = 255;

        public LabelStack()
        {
            LabelsStack = new Stack<LabelMPLS>();
        }

        public bool IsStackEmpty()
        {
            if (LabelsStack.Count == 0)
                return true;
            else
                return false;
        }

        public int GetStackLength()
        {
            if (IsStackEmpty() == false)
                return (1 + LabelsStack.Count * 4);
            else
                return 1;
        }

        public List<byte> StackToBytes()
        {
            List<byte> myBytes = new List<byte>();

            byte stackFlag;

            if (IsStackEmpty())
            {
               stackFlag = StackEmptyFlag;

            } else  stackFlag = StackNotEmptyFlag;

            myBytes.AddRange(new List<byte>() { stackFlag });

            List<LabelMPLS> tmpLabelList = LabelsStack.ToList();
            int labelsNumber = tmpLabelList.Count;

            for (int i = 0; i < labelsNumber; i++)
            {
                myBytes.AddRange(tmpLabelList[i].LabelToBytes());
                if (i != labelsNumber - 1)
                {
                    myBytes.Add(0);
                }
                else
                {
                    myBytes.Add(255);
                }
            }
            return myBytes;
        }
        
        public static LabelStack BytesToStack(byte[] bytes)
        {
            LabelStack tmpStack = new LabelStack();

            if (bytes[0] == StackNotEmptyFlag)
            {
                var index = 1;
                while (true)
                {
                    var label = new LabelMPLS
                    {
                        Number = (short)((bytes[index + 1] << 8) + bytes[index]),
                        TimeToLive = bytes[index + 2]
                    };
                    var bottomOfStack = bytes[index + 3];

                    tmpStack.LabelsStack.Push(label);
                    if (bottomOfStack == 255)
                    {
                        break;
                    }

                    index = index + 4;
                }
            }
            return tmpStack;
        }
    }
}