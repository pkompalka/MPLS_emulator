using MahApps.Metro.Controls;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

using Utility;

namespace Node
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class NetworkNodeWindow : MetroWindow
    {     
        public MPLSSocket mySocket { get; set; }
        public NetworkNode networknode;
        public int NodeNumber { get; set; }

        public NetworkNodeWindow(int n)
        {
            InitializeComponent();
            logBox.Document.Blocks.Remove(logBox.Document.Blocks.FirstBlock);
            NodeNumber = n;
            Title = "Router " + NodeNumber.ToString();
            networknode = new NetworkNode(this);
            Task.Run(() => MyStart(NodeNumber));
        }

        private void MyStart(int nodeNr)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                NewLog("Tworzenie routera", this);
            }));
            
            try
            {   
                XmlReader xmlReader = new XmlReader("ConfigurationNode.xml");

                nodeNr--;
                networknode.CloudIP = IPAddress.Parse(xmlReader.GetAttributeValue(nodeNr, "router", "CLOUD_IP_ADDRESS"));
                networknode.CloudPort = Convert.ToUInt16(xmlReader.GetAttributeValue(nodeNr, "router", "CLOUD_PORT"));
                networknode.ManagementIP = IPAddress.Parse(xmlReader.GetAttributeValue(nodeNr, "router", "MANAGEMENT_IP_ADDRESS"));
                networknode.ManagementPort = Convert.ToUInt16(xmlReader.GetAttributeValue(nodeNr, "router", "MANAGEMENT_PORT"));
                networknode.NodeName = xmlReader.GetAttributeValue(nodeNr, "router", "NAME");
                networknode.ListNode.Add(networknode);
            }
            catch (Exception)
            {
                return;
            }
            networknode.RoutingTables.LoadTablesFromConfig(networknode, this);
            networknode.RoutingTables.StartManagementAgent();

           ConnectToCloud();
            
            Task.Run(async () =>
            {
                while (true)
                {
                    if (!mySocket.Connected || mySocket == null)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            NewLog("Zerwane połączenie z chmurą", this);
                        }));
                       
                        break;
                    }
                    else
                    {
                        mySocket.Send(Encoding.ASCII.GetBytes("C"));
                        await Task.Delay(5000);
                    }
                }
            });

            while (true)
            {
                while (!mySocket.Connected || mySocket == null)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        NewLog("Ponawianie połączenia z chmurą", this);
                    }));
                
                    mySocket = new MPLSSocket(networknode.ManagementIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    mySocket.Connect(new IPEndPoint(networknode.CloudIP, networknode.CloudPort));
                    mySocket.Send(Encoding.ASCII.GetBytes($"HELLO {networknode.NodeName}"));

                    Dispatcher.Invoke(new Action(() =>
                    {
                        NewLog("Ponownie połączony z chmurą", this);
                    }));
                  
                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            if (!mySocket.Connected || mySocket == null)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    NewLog("Zerwane połączenie z chmurą", this);
                                }));
                              
                                break;
                            }
                            else
                            {
                                mySocket.Send(Encoding.ASCII.GetBytes("C"));
                                await Task.Delay(5000);
                            }
                        }
                    });
                }

                try
                {
                    MPLSPacket receivedPacket = mySocket.Receive();
                    Dispatcher.Invoke(new Action(() =>
                    {
                        NewLog($"Otrzymano pakiet nr {receivedPacket.ID} wiadomość {receivedPacket.Payload} o time to live = {receivedPacket.TimeToLive} z portu {receivedPacket.Port}", this);
                    }));
                    
                    Task.Run(() =>
                    { 
                        MPLSPacket routedPacket = null;
                        routedPacket = networknode.PacketLogic.RoutePacketMPLS(receivedPacket, networknode.RoutingTables, this);
                        if (routedPacket == null)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                NewLog("Pakiet nie został obsłużony poprawnie", this);
                            }));
                            return;
                        }

                        try
                        {
                            mySocket.Send(routedPacket);
                        }
                        catch (Exception e)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                NewLog($"Pakiet nr {routedPacket.ID} nie został wysłany poprawnie: {e.Message}", this);
                            }));
                            
                        }
                    });
                }

                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.TimedOut)
                    {
                        if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                NewLog("Zerwane połączenie z chmurą", this);
                            }));
                          
                            continue;
                        }

                        else
                        {
                            NewLog($"{e.Source}: {e.SocketErrorCode}", this);
                        }
                    }
                }
            }
        }

        private void ConnectToCloud()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                NewLog("Próba połączenia z chmurą kablową", this);
            }));

            try
            {
                MPLSSocket socket = new MPLSSocket(networknode.ManagementIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(new IPEndPoint(networknode.CloudIP, networknode.CloudPort));

                socket.Send(Encoding.ASCII.GetBytes($"HELLO {networknode.NodeName}"));
                Dispatcher.Invoke(new Action(() =>
                {
                    NewLog("Uzyskano połączenie z chmurą kablową", this);
                }));

                mySocket = socket;

                Task.Run(async () =>
                {
                    while (true)
                    {
                        var tmp = mySocket != null && mySocket.Connected;
                        if (tmp)
                        {
                            mySocket.Send(Encoding.ASCII.GetBytes("C"));

                            await Task.Delay(5000);
                        }
                        else
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                NewLog("Zerwane połączenie z chmurą", this);
                            }));

                            break;
                        }
                    }
                });
            }
            catch (Exception)
            {
                NewLog("Nieudane połączenie z chmurą", this);
            }
        }

        public void NewLog(string info, NetworkNodeWindow tmp)
        {
            string timeNow = DateTime.Now.ToString("h:mm:ss") + "." + DateTime.Now.Millisecond.ToString();
            string restLog = info;
            string fullLog = timeNow + " " + restLog;
            tmp.logBox.Document.Blocks.Add(new Paragraph(new Run(fullLog)));
            tmp.logBox.ScrollToEnd();
        }
    }
}
