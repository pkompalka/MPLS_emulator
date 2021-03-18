using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;


namespace Utility
{
    public class MPLSPacket
    {      
        public int ID { get; set; }       
        public ushort TimeToLive { get; set; }      
        public IPAddress SourceAddress { get; set; }
        public IPAddress DestinationAddress { get; set; }       
        public ushort Port { get; set; }
        public string Payload { get; set; }

        private const int DefaultTTL = 255;
        public const int PacketHeaderLength = 22;
        public int PacketLength { get; set; }
        public LabelStack MPLSLabelStack { get; set; }
        
        public MPLSPacket()
        {
            MPLSLabelStack = new LabelStack();
            TimeToLive = DefaultTTL;
        }
        
        public byte[] PacketToBytes()
        {
            List<byte> packetbytes = new List<byte>();
            PacketLength = PacketHeaderLength + Payload.Length;

            packetbytes.AddRange(MPLSLabelStack.StackToBytes());
            packetbytes.AddRange(BitConverter.GetBytes(ID));
            packetbytes.AddRange(BitConverter.GetBytes(PacketLength));
            packetbytes.AddRange(BitConverter.GetBytes(TimeToLive));
            packetbytes.AddRange(SourceAddress.GetAddressBytes());
            packetbytes.AddRange(DestinationAddress.GetAddressBytes());
            packetbytes.AddRange(BitConverter.GetBytes(Port));
            packetbytes.AddRange(Encoding.ASCII.GetBytes(Payload ?? ""));

            return packetbytes.ToArray();
        }
        
        public static MPLSPacket BytesToPacket(byte[] bytes)
        {
            try
            {
                MPLSPacket packet = new MPLSPacket();
                packet.MPLSLabelStack = LabelStack.BytesToStack(bytes);
                var stackLength = packet.MPLSLabelStack.GetStackLength();
                
                packet.ID = BitConverter.ToInt32(bytes, stackLength);
                packet.PacketLength = BitConverter.ToInt32(bytes, stackLength + 4);
                packet.TimeToLive = (ushort)((bytes[stackLength + 9] << 8) + bytes[stackLength + 8]);

                packet.SourceAddress = new IPAddress(new byte[]
                    {bytes[stackLength + 10], bytes[stackLength + 11], bytes[stackLength + 12], bytes[stackLength + 13]});
                packet.DestinationAddress = new IPAddress(new byte[]
                {
                    bytes[stackLength + 14], bytes[stackLength + 15], bytes[stackLength + 16], bytes[stackLength + 17]
                });

                packet.Port = (ushort)((bytes[stackLength + 19] << 8) + bytes[stackLength + 18]);

                var usefulPayload = new List<byte>();
                usefulPayload.AddRange(bytes.ToList()
                    .GetRange(stackLength + 20, packet.PacketLength - PacketHeaderLength));

                packet.Payload = Encoding.ASCII.GetString(usefulPayload.ToArray());
                return packet;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }  
}
