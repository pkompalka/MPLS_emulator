using System.Collections.Generic;
using System.Net;
using Utility;

namespace Node
{
    public class NetworkNode
    {
        public List<NetworkNode> ListNode = new List<NetworkNode>();

        public IPAddress CloudIP { get; set; }

        public ushort CloudPort { get; set; }

        public IPAddress ManagementIP { get; set; }
        
        public ushort ManagementPort { get; set; }

        public string NodeName { get; set; }

        public BehaviourOfPacket PacketLogic { get; set; }

        public TablesInNode RoutingTables { get; set; }

        public MPLSSocket NodeSocket { get; set; }

        public NetworkNode networknode;

        public NetworkNodeWindow Window { get; set; }

        public NetworkNode(NetworkNodeWindow myWindow)
        {
            Window = myWindow;
            PacketLogic = new BehaviourOfPacket(myWindow);
            RoutingTables = new TablesInNode(myWindow);
        }
    }
}
