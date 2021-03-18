using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Utility;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.Concurrent;

namespace CableCloud
{
    class StateObject
    {
        public Socket workSocket { get; set; } = null;
        public const int BufferSize = 1024;
        public byte[] Buffer { get; set; } = new byte[BufferSize];
        public StringBuilder sb { get; set; } = new StringBuilder();
    }

    public partial class Cloud : INotifyPropertyChanged
    {
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private IPAddress CloudIPAddress;
        private int CloudPort;
        IPEndPoint CloudEP;
        private const int Backlog = 100;
        private Socket AsyncServer;

        public Dictionary<string, string> IsCableWorkingDictionary { get; set; }
        public Dictionary<string, string> NodeAddressDictionary { get; set; }
        public Dictionary<string, string> NodesConnectedDictionary { get; set; }
        
        private List<CableLinkingNodes> connectedPairs;
        public List<CableLinkingNodes> ConnectedPairs
        {
            get
            {
                return connectedPairs;
            }
            set
            {
                connectedPairs = value;
                NotifyPropertyChanged("ConnectedPairs");
            }
        }
        
        private ConcurrentDictionary<Socket, string> SocketToNodeName;
        private ConcurrentDictionary<string, Socket> NodeNameToSocket;

        private ICommand buttonDelete;
        public ICommand ButtonDelete
        {
            get
            {
                return buttonDelete ?? (buttonDelete = new DelegateCommand(x => buttonDeleteMethod()));
            }
            set
            {
                buttonDelete = value;
            }
        }

        private int toChange;
        public int ToChange
        {
            get
            {
                return toChange;
            }
            set
            {
                toChange = value;
                NotifyPropertyChanged(nameof(ToChange));
            }
        }

        private string whatChanged;
        public string WhatChanged
        {
            get
            {
                return whatChanged;
            }
            set
            {
                whatChanged = value;
                NotifyPropertyChanged(nameof(WhatChanged));
            }
        }

        private string logText = DateTime.Now.ToString("h:mm:ss") + "." + DateTime.Now.Millisecond.ToString() + " Start chmury kablowej";
        public string LogText
        {
            get
            {
                return logText;
            }
            set
            {
                logText = value;
                NotifyPropertyChanged(nameof(LogText));
            }
        }
        
        public Cloud()
        {
            CableCloudConfig();
            SocketToNodeName = new ConcurrentDictionary<Socket, string>();
            NodeNameToSocket = new ConcurrentDictionary<string, Socket>();
            Task.Run(() => StartAsyncListener()); 
        }

        public void CableCloudConfig()
        {
            XmlReader xmlReader = new XmlReader("ConfigurationCloud.xml");
            CloudIPAddress = IPAddress.Parse(xmlReader.GetAttributeValue(0, "cloud", "CLOUD_IP_ADDRESS"));
            CloudPort = Convert.ToInt32(xmlReader.GetAttributeValue(0, "cloud", "CLOUD_PORT"));
            CloudEP = new IPEndPoint(CloudIPAddress, CloudPort); 
            
            NodeAddressDictionary = new Dictionary<string, string>();

            int numberofhost = xmlReader.GetNumberOfItems("host");
            for(int a=0; a<numberofhost; a++)
            {
                var name = xmlReader.GetAttributeValue(a, "host", "NAME");
                var ip = xmlReader.GetAttributeValue(a, "host", "IP_ADDRESS");
                NodeAddressDictionary.Add(name, ip);
            }

            int numberofrouter = xmlReader.GetNumberOfItems("router");
            for (int a = 0; a < numberofrouter; a++)
            {
                var name = xmlReader.GetAttributeValue(a, "router", "NAME");
                var ip = xmlReader.GetAttributeValue(a, "router", "IP_ADDRESS");
                NodeAddressDictionary.Add(name, ip);
            }
            
            NodesConnectedDictionary = new Dictionary<string, string>();
            IsCableWorkingDictionary = new Dictionary<string, string>();
            ConnectedPairs = new List<CableLinkingNodes>();

            int numberofconnection = xmlReader.GetNumberOfItems("connect");
            for (int a = 0; a < numberofconnection; a++)
            {
                var node_name1 = xmlReader.GetAttributeValue(a, "connect", "NODE1");
                var node_name2 = xmlReader.GetAttributeValue(a, "connect", "NODE2");
                var port1 = xmlReader.GetAttributeValue(a, "connect", "PORT1");
                var port2 = xmlReader.GetAttributeValue(a, "connect", "PORT2");
                var Isworking = xmlReader.GetAttributeValue(a, "connect", "ISWORKING");

                NodesConnectedDictionary.Add(node_name1 + ":" + port1, node_name2 + ":" + port2);
                NodesConnectedDictionary.Add(node_name2 + ":" + port2, node_name1 + ":" + port1);

                var connection1 = node_name1 + ":" + port1;
                var connection2 = node_name2 + ":" + port2;
                IsCableWorkingDictionary.Add(connection1 + "-" + connection2, Isworking);
                IsCableWorkingDictionary.Add(connection2 + "-" + connection1, Isworking);

                ConnectedPairs.Add(new CableLinkingNodes(node_name1, port1, node_name2, port2, Isworking));
            } 
        }

        public void StartAsyncListener()
        {
            AsyncServer = new Socket(CloudIPAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            try
            {
                AsyncServer.Bind(CloudEP);
                AsyncServer.Listen(Backlog);
                while (true)
                {
                    allDone.Reset();  
                    AsyncServer.BeginAccept(new AsyncCallback(AcceptCallback), AsyncServer); 
                    allDone.WaitOne();
                }
            }
            catch (Exception)
            {
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
 
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            
            int bytesRead;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (Exception)
            {
                string outString;
                Socket outSocket;
                
                var nodeName = SocketToNodeName[handler];
                SocketToNodeName.TryRemove(handler, out outString);
                NodeNameToSocket.TryRemove(nodeName, out outSocket);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }

            state.sb.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));
            var content = state.sb.ToString().Split(' ');
            if (content[0].Equals("HELLO"))
            {
                int index = content[1].IndexOf("C");
                if (index > 0)
                {
                    content[1] = content[1].Substring(0, index);
                }
                while (true)
                {
                    var success = SocketToNodeName.TryAdd(handler, content[1]);
                    if (success)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                while (true)
                {
                    var success = NodeNameToSocket.TryAdd(content[1], handler);
                    if (success)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                NewLog($"Połączono z {content[1]}");
            }
            else if (content[0].Equals("C"))
            {

            }
            else
            {
                ProcessPacket(state, handler, ar);
               
            }
            state.sb.Clear();
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                handler.EndSend(ar);
              
            }
            catch (Exception)
            {
            }
        }

        private void SendPacket(Socket handler, byte[] byteData)
        {
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void ProcessPacket(StateObject state, Socket handler, IAsyncResult ar)
        {
            var receivedpacket = MPLSPacket.BytesToPacket(state.Buffer);
            string myNodeName = SocketToNodeName[handler];
            string myNodeIP = NodeAddressDictionary[myNodeName];
            string myNodePort = Convert.ToString(receivedpacket.Port);
            NewLog($"Otrzymano pakiet z {myNodeIP} port {myNodePort}");
           
            try
            {
                string myNodeNext = NextCableNode(myNodeName, myNodePort);
                string myPortNext = NextCablePort(myNodeName, myNodePort);     
                string myIPNext = NodeAddressDictionary[myNodeNext];
                string myIsCableWorking = GetWorkingCable(myNodeName, myNodePort, myNodeNext, myPortNext);
                if (myIsCableWorking.Equals("BROKEN")) 
                {
                   NewLog($"Połączenie między {myNodeIP} port {myNodePort} a {myIPNext} port {myPortNext} nie działa");
                }
                else if (myIsCableWorking.Equals("WORKING"))
                {
                    receivedpacket.Port = ushort.Parse(myPortNext);
                    Socket tmpsocket = NodeNameToSocket[myNodeNext];    
                    SendPacket(tmpsocket, receivedpacket.PacketToBytes());
                    NewLog($"Wysłano pakiet do {myIPNext} port {myPortNext}");
                }
                else
                {
                    NewLog($"Blad w działaniu łącza między {myIPNext}:{myPortNext}");
                }
            }
            catch (Exception)
            {
                string addr = NodeAddressDictionary[myNodeName];
                NewLog($"Połączenie nieudane {addr}");
            }
        }
        
        public string NextCableNode(string node, string port)
        {
            var parts = NodesConnectedDictionary[node + ":" + port].Split(':');
            return parts[0];
        }

        public string NextCablePort(string node, string port)
        {
            var parts = NodesConnectedDictionary[node + ":" + port].Split(':');
            return parts[1];
        }

        public string GetWorkingCable(string n1, string p1, string n2, string p2)
        {
            var connectedNodes = n1 + ":" + p1 + "-" + n2 + ":" + p2;
            return IsCableWorkingDictionary[connectedNodes];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void buttonDeleteMethod()
        {
            if (ConnectedPairs[ToChange - 1].IsWorking == "WORKING")
            {
                ConnectedPairs[ToChange - 1].IsWorking = "BROKEN";
                WhatChanged = ConnectedPairs[ToChange - 1].Node1 + " - " + ConnectedPairs[ToChange - 1].Node2 + " popsute";

                var changedNodes = $"{ConnectedPairs[ToChange - 1].Node1}:{ConnectedPairs[ToChange - 1].Port1}-" +
                    $"{ConnectedPairs[ToChange - 1].Node2}:{ConnectedPairs[ToChange - 1].Port2}";
                IsCableWorkingDictionary[changedNodes] = "BROKEN";

                var changedNodes2 = $"{ConnectedPairs[ToChange - 1].Node2}:{ConnectedPairs[ToChange - 1].Port2}-" +
                   $"{ConnectedPairs[ToChange - 1].Node1}:{ConnectedPairs[ToChange - 1].Port1}";
                IsCableWorkingDictionary[changedNodes2] = "BROKEN";

            }
            else
            {
                ConnectedPairs[ToChange - 1].IsWorking = "WORKING";
                WhatChanged = ConnectedPairs[ToChange - 1].Node1 + " - " + ConnectedPairs[ToChange - 1].Node2 + " dobre";

                var changedNodes = $"{ConnectedPairs[ToChange - 1].Node1}:{ConnectedPairs[ToChange - 1].Port1}-" +
                    $"{ConnectedPairs[ToChange - 1].Node2}:{ConnectedPairs[ToChange - 1].Port2}";
                IsCableWorkingDictionary[changedNodes] = "WORKING";

                var changedNodes2 = $"{ConnectedPairs[ToChange - 1].Node2}:{ConnectedPairs[ToChange - 1].Port2}-" +
                   $"{ConnectedPairs[ToChange - 1].Node1}:{ConnectedPairs[ToChange - 1].Port1}";
                IsCableWorkingDictionary[changedNodes2] = "WORKING";
            }
        }

        public void NewLog(string info)
        {
            string timeNow = DateTime.Now.ToString("h:mm:ss") + "." + DateTime.Now.Millisecond.ToString();
            string restLog = info;
            string fullInfo = timeNow + " " + restLog;
            LogText = LogText + "\n\n" + fullInfo;
        }
    }
}

