using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utility;

namespace Managment
{
    class StateObject
    {
        public Socket WorkSocket { get; set; } = null;
        public const int BufferSize = 1024;
        public byte[] Buffer { get; set; } = new byte[BufferSize];
        public StringBuilder sb { get; set; } = new StringBuilder();
    }

    class NetworkManager
    {
        private ManualResetEvent AllDone = new ManualResetEvent(false);
        private IPAddress MSAdress = IPAddress.Parse("127.0.0.1");
        private Socket AsyncServer;
        private const int MSPort = 60120;
        private const int Backlog = 100;
        public Dictionary<string, List<IPRecord>> NodeToIPTable { get; set; } = new Dictionary<string, List<IPRecord>>();
        public Dictionary<string, List<LabelRecord>> NodeToFECTable { get; set; } = new Dictionary<string, List<LabelRecord>>();
        private ConcurrentDictionary<string, Socket> NodeToSocket;
        public ConcurrentDictionary<Socket, string> SocketToNode { get; set; }

        public MainWindow Window;

        public NetworkManager()
        {
            NodeToSocket = new ConcurrentDictionary<string, Socket>();
            SocketToNode = new ConcurrentDictionary<Socket, string>();
        }

        public void Start(MainWindow myWindow)
        {
            Window = myWindow;
            Task.Run(action: () => StartAsyncServer());
        }

        private void StartAsyncServer()
        {
            AsyncServer = new Socket(MSAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);   
            NewLog("Oczekiwanie na połączenia", Window);
            try
            {
                AsyncServer.Bind(new IPEndPoint(MSAdress, MSPort));
                AsyncServer.Listen(Backlog);
                while (true)
                {
                    AllDone.Reset();
                    AsyncServer.BeginAccept(new AsyncCallback(AcceptCallback), AsyncServer);
                    AllDone.WaitOne();
                }
            }
            catch (Exception)
            {
                NewLog("Błąd startu serwera ", Window);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        { 
            AllDone.Set();
            
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            StateObject state = new StateObject();
            state.WorkSocket = handler;
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;
            int bytesRead;
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (Exception)
            {
                Socket outSocket;
                string outString;

                var routerName = SocketToNode[handler];
                NodeToSocket.TryRemove(routerName, out outSocket);
                SocketToNode.TryRemove(handler, out outString);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                return;
            }
            state.sb.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));
            var content = state.sb.ToString().Split(' ');

            
            if (content[0].Equals("HELLO"))
            {
                var routerName = content[1];

                while (true)
                {
                    var success = NodeToSocket.TryAdd(routerName, handler);
                    if (success)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }

                while (true)
                {
                    var success = SocketToNode.TryAdd(handler, routerName);
                    if (success)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
                NewLog($"Połączono z {content[1]}", Window);
                SendDefaultTables(routerName, handler);
            }
            else if (content[0].StartsWith("C"))
            {

            }
            else
            {
                NewLog("Niepoprawne żądanie", Window);
                return;
            }
            state.sb.Clear();
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        private void SendDefaultTables(string routerName, Socket handler)
        {
            byte[] responseMessage = Encoding.ASCII.GetBytes("HELLO");
            handler.Send(responseMessage);

            NodeToFECTable.Keys.ToList().FindAll(key => key.Equals(routerName)).ForEach(key =>
            {
                NodeToFECTable[key].ForEach(record => SendRecord(routerName, record, "FecRecordAdded"));
            });

            NodeToIPTable.Keys.ToList().FindAll(key => key.Equals(routerName)).ForEach(key =>
            {
                NodeToIPTable[key].ForEach(record => SendRecord(routerName, record, "IpRecordAdded"));
            });

        }

        public void SendRecord(string routerName, LabelRecord record, string TypeOfAction)
        {
            var socket = NodeToSocket[routerName];
            byte[] data = Encoding.ASCII.GetBytes($"{TypeOfAction} {record.InFEC} {record.InPort} {record.OutFEC} {record.OutPort}");
            socket.Send(data);
            Thread.Sleep(100);
            NewLog($"Wysyłano uaktualnienie do {routerName} reguły FEC do tablicy FEC {record.InFEC} {record.InPort} {record.OutFEC} {record.OutPort}",Window);
        }

        public void SendRecord(string routerName, IPRecord record, string TypeOfAction)
        {
            var socket = NodeToSocket[routerName];
            byte[] data = Encoding.ASCII.GetBytes($"{TypeOfAction} {record.DestinationAddress} {record.PortOut} {record.FecFirst}");
            socket.Send(data);
            Thread.Sleep(100);
            NewLog($"Wysyłano uaktualnienie do {routerName} reguły IP do tablicy IP {record.DestinationAddress} {record.PortOut} {record.FecFirst}", Window);
        }
        
        public List<LabelRecord> ReadConfigFEC()
        {
            XmlReader xmlodczyt = new XmlReader("ConfigManager.xml");
            List<LabelRecord> listfec = new List<LabelRecord>();
            int numberoffecline = xmlodczyt.GetNumberOfItems("addFEC");
            for (int i = 0; i < numberoffecline; i++)
            {
                short fecin = Convert.ToInt16(xmlodczyt.GetAttributeValue(i, "addFEC", "FECIN"));
                short fecout = Convert.ToInt16(xmlodczyt.GetAttributeValue(i, "addFEC", "FECOUT"));
                ushort portin = Convert.ToUInt16(xmlodczyt.GetAttributeValue(i, "addFEC", "PORTIN"));
                ushort portout = Convert.ToUInt16(xmlodczyt.GetAttributeValue(i, "addFEC", "PORTOUT"));
                string name = xmlodczyt.GetAttributeValue(i, "addFEC", "NAME");
                listfec.Add(new LabelRecord(name, fecin, fecout, portin, portout));
            }
            return listfec;
        }

        public List<IPRecord> ReadConfigIP()
        {
            XmlReader xmlodczyt = new XmlReader("ConfigManager.xml");
            List<IPRecord> listip = new List<IPRecord>();
            int numberofipline = xmlodczyt.GetNumberOfItems("addIP");
            for (int i = 0; i < numberofipline; i++)
            {
                ushort outport = Convert.ToUInt16(xmlodczyt.GetAttributeValue(i, "addIP", "OUT_PORT"));
                string name = xmlodczyt.GetAttributeValue(i, "addIP", "NAME");
                IPAddress destIpAddress = IPAddress.Parse(xmlodczyt.GetAttributeValue(i, "addIP", "DES_IP_ADDRESS"));
                short fecfirst = Convert.ToInt16(xmlodczyt.GetAttributeValue(i, "addIP", "FECFIRST"));
                listip.Add(new IPRecord(name, destIpAddress, outport,fecfirst));
            }
            return listip;
        }

        public void ReadConfig()
        {
            var listIpRec = ReadConfigIP();
            var listFecRec = ReadConfigFEC();
            var routersInIPTable = (from list in listIpRec orderby list.NameRouter select list.NameRouter).Distinct();
            var routersInFECTable = (from list in listFecRec orderby list.NameRouter select list.NameRouter).Distinct();

            foreach (var key in routersInIPTable)
            {
                var tmpList = listIpRec.FindAll(IPRecord => IPRecord.NameRouter == key);
                NodeToIPTable.Add(key, tmpList);
            }

            foreach (var key in routersInFECTable)
            {
                var tmpList = listFecRec.FindAll(FECRecord => FECRecord.NameRouter == key);
                NodeToFECTable.Add(key, tmpList);
            }
        }

        private void NewLog(string info, MainWindow tmp)
        {
            Window.Dispatcher.Invoke(() => Window.NewLog(info, tmp));
        }
    }
}