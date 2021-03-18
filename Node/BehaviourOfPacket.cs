using System;
using Utility;

namespace Node
{
    public class BehaviourOfPacket
    {
        private TablesInNode RoutingTables { get; set; }

        private NetworkNodeWindow Window { get; set; }

        public BehaviourOfPacket(NetworkNodeWindow window)
        {
            Window = window;
        }
        
        public MPLSPacket RoutePacketMPLS(MPLSPacket packet, TablesInNode getTables, NetworkNodeWindow window)
        {
            Window = window;
            RoutingTables = getTables;

            if (packet.TimeToLive == 0)
            {
                NewLog("Pakiet odrzucony, time to live = 0", Window);
                return null;
            }

            MPLSPacket routedPacket = null;

            if(packet.TimeToLive == 254)
            {
                foreach (IPRecord record in RoutingTables.IpTable.Records)
                {
                    if (packet.DestinationAddress.Equals(record.DestinationAddress))
                    {
                        LabelMPLS firstLabel = new LabelMPLS();
                        firstLabel.Number = record.FecFirst;
                        firstLabel.TimeToLive = 255;
                        packet.MPLSLabelStack.LabelsStack.Push(firstLabel);
                        NewLog("Nałożono etykietę (PUSH)", Window);
                    }
                }
            }

            if (packet.MPLSLabelStack.IsStackEmpty() == true)
            {
                routedPacket = IPRouting(packet);
            }
            else
            {
                routedPacket = FECRouting(packet);
            }

            if (routedPacket == null)
            {
                NewLog("Pakiet odrzucony, brak odpowiedniego rekordu", Window);
                return null;
            }
            return routedPacket;
        }
        
        private MPLSPacket IPRouting(MPLSPacket packet)
        {
            bool ipFlag = false;
            
            if (packet.MPLSLabelStack.IsStackEmpty() == false)
            {
                packet.MPLSLabelStack.LabelsStack.Pop();
                NewLog("Zdjęto etykietę z pakietu (POP)", Window);
            }

            foreach (IPRecord record in RoutingTables.IpTable.Records)
            { 
                if (packet.DestinationAddress.Equals(record.DestinationAddress))
                {
                    packet.Port = record.PortOut;
                    --packet.TimeToLive;
                    ipFlag = true;
                    NewLog($"Pakiet wysłany przez port {packet.Port} przez IP", Window);
                    break;
                }
            }

            if (ipFlag == false)
            {
                NewLog("Brak zgodnego rekordu IP", Window);
                return null;
            }

            return packet;
        }
        
        private MPLSPacket FECRouting(MPLSPacket packet)
        {
            int tmpFEC = packet.MPLSLabelStack.LabelsStack.Peek().Number;
            if (tmpFEC == 0 && packet.TimeToLive != 254)
            {
                packet.MPLSLabelStack.LabelsStack.Pop();
                NewLog("Etykieta pakieta zdjęta (POP)", Window);
                packet = IPRouting(packet);
            }
            else
            {
                bool fecFlag = false;
                int tmpPortIn = packet.Port;
                packet.MPLSLabelStack.LabelsStack.Pop();
                bool myFlagLast = false;

                foreach (LabelRecord record in RoutingTables.LSTable.Records)
                {
                    if (tmpFEC == record.InFEC && tmpPortIn == record.InPort)
                    {
                        packet.Port = record.OutPort;
                        --packet.TimeToLive;
                        if (record.OutFEC == 0 && packet.TimeToLive != 254)
                        {
                            NewLog("Etykieta pakietu zdjęta (POP)", Window);
                            packet = IPRouting(packet);
                            myFlagLast = true;
                            break;
                        }
                        LabelMPLS newLabel = new LabelMPLS(record.OutFEC);
                        packet.MPLSLabelStack.LabelsStack.Push(newLabel);
                        if (packet.TimeToLive != 253)
                        {
                            NewLog("Etykieta pakieta zmieniona (SWAP)", Window);
                        }
                        fecFlag = true;
                        NewLog($"Pakiet wysłany przez port {packet.Port} z etykietą {packet.MPLSLabelStack.LabelsStack.Peek().Number}", Window);
                        break;
                    }
                }

                if(myFlagLast == false)
                {
                    if (fecFlag == false)
                    {
                        NewLog("Brak pasującej etykiety", Window);
                        packet = IPRouting(packet);
                    }
                }
            }

            return packet;
        }

        private void NewLog(string info, NetworkNodeWindow tmp)
        {
            Window.Dispatcher.Invoke(() => Window.NewLog(info, tmp));
        }
    }
}
