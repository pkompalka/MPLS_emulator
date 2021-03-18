using System.Net.Sockets;
using System.Text;

namespace Utility
{
    public class MPLSSocket : Socket
    {
        public MPLSSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) : base(addressFamily, socketType, protocolType)
        {
            ReceiveTimeout = 999999999;
        }

        public MPLSPacket Receive()
        {
            byte[] receiveBuffer = new byte[256];
            int receivedBytes = Receive(receiveBuffer);

            if (Encoding.ASCII.GetString(receiveBuffer, 0, receivedBytes).Substring(0, 1).Equals("C"))
            {
                return null;
            }

            return MPLSPacket.BytesToPacket(receiveBuffer);
        }

        public int Send(MPLSPacket packet)
        {
            return Send(packet.PacketToBytes());
        }
    }
}