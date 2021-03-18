using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Utility;

namespace Node
{
    class Agent
    {
        public NetworkNodeWindow Window { get; set; }

        public NetworkNode NodeConfig { get; set; }
        
        public Socket AgentSocket { get; set; }
        
        public event EventHandler<TableEvent> TablesUpdate;

        public Agent(NetworkNode networkNode, NetworkNodeWindow myWindow)
        {
            NodeConfig = networkNode;
            Window = myWindow;
        }

        public void StartAgent()
        {
            while (true)
            {
                ConnectToManagement(NodeConfig);

                if (AgentSocket == null)
                {
                    NewLog("Połączenie z socketem nieudane, ponawianie", Window);
                    continue;
                }

                while (true)
                {
                    try
                    {   
                        byte[] buffer = new byte[512];
                        int bytesToRead = AgentSocket.Receive(buffer);
                        
                        var message = Encoding.ASCII.GetString(buffer, 0, bytesToRead);

                        Task.Run(() =>
                        {
                            var action = message.Split(null)[0];
                            var data = message.Replace($"{action} ", "");
                            switch (action)
                            {
                                case "FecRecordAdded":
                                    var fecAdded = new TableEvent(action, new LabelRecord(data));
                                    OnTablesUpdate(fecAdded);
                                    break;
                                case "IpRecordAdded":
                                    var ipAdded = new TableEvent(action, new IPRecord(data));
                                    OnTablesUpdate(ipAdded);
                                    break;
                                case "FecRecordRemove":
                                    var fecRemoved = new TableEvent(action, new LabelRecord(data));
                                    OnTablesUpdate(fecRemoved);
                                    break;
                                case "IpRecordRemove":
                                    var ipRemoved = new TableEvent(action, new IPRecord(data));
                                    OnTablesUpdate(ipRemoved);
                                    break;
                            }
                        });
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode != SocketError.TimedOut)
                        {
                            if(e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                            {

                                NewLog("Zerwano połączenie z zarządzaniem", Window);
                                break;
                            } 
                            else
                            {
                                NewLog($"{e.Source}: {e.SocketErrorCode}", Window);
                            }
                        }
                        
                    }
                }
            }
        }

        private void ConnectToManagement(NetworkNode ConfigNode)
        {
            while (true)
            {
                Socket socket = new Socket(ConfigNode.ManagementIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = 10000
                };

                try
                {
                    var beginConnect = socket.BeginConnect(new IPEndPoint(ConfigNode.ManagementIP, ConfigNode.ManagementPort), null, null);
                    bool tmp = beginConnect.AsyncWaitHandle.WaitOne(5000, true);
                    if (tmp)
                    {
                        socket.EndConnect(beginConnect);
                    }
                    else
                    {
                        socket.Close();
                        NewLog("Nieudane połączenie z zarządzaniem - timeout", Window);
                        continue;
                    }
                }
                catch (Exception)
                {
                    NewLog("Ponawianie połączenia",Window);
                }
                
                try
                {
                    NewLog("Próba połączenia z zarządzaniem",Window);
                    socket.Send(Encoding.ASCII.GetBytes($"HELLO {ConfigNode.NodeName}"));
                    byte[] buffer = new byte[256];
                    int bytesToRead = socket.Receive(buffer);

                    var message = Encoding.ASCII.GetString(buffer, 0, bytesToRead);

                    if (message.Contains("HELLO"))
                    {
                        NewLog("Uzyskano połączenie z zarządzaniem", Window);
                        AgentSocket = socket;
                        break;
                    }
                    else
                    {
                    }
                }
                catch (Exception)
                {
                    NewLog("Nieudane połączenie z zarządzaniem", Window);
                }
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    var tmp = AgentSocket != null && AgentSocket.Connected;
                    if (tmp)
                    {
                        AgentSocket.Send(Encoding.ASCII.GetBytes("C"));
                        await Task.Delay(5000); //ms
                    }
                    else
                    {
                        NewLog("Zerwanie połączenia z zarządzaniem",Window);
                        break;
                    }
                }
            });
        }

        protected virtual void OnTablesUpdate(TableEvent e)
        {
            TablesUpdate?.Invoke(this, e);
        }

        private void NewLog(string info, NetworkNodeWindow tmp)
        {
            Window.Dispatcher.Invoke(() => Window.NewLog(info, tmp));
        }

    }
}
