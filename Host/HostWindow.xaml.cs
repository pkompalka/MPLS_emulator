using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Net;
using System.Net.Sockets;
using Utility;

namespace Host
{
    public partial class HostWindow : MetroWindow, INotifyPropertyChanged
    {
        public HostLogWindow LogWindow { get; set; }

        public DistantHosts whereToSend { get; set; }

        public MPLSPacket myPacket { get; set; }

        public MPLSSocket mySocket { get; set; }

        public Hosts hostsConfiguration { get; set; }
        
        public bool myFlag { get; set; }

        public bool FlagSending { get; set; }

        public bool FlagListening { get; set; }

        public string SelectedPeriod { get; set; }
        
        public int PacketNumber { get; set; }

        public double myPeriod { get; set; }
        
        public int HostNumber { get; set; }

        private string periodText;
        public string PeriodText
        {
            get { return periodText; }
            set
            {
                periodText = value;
                NotifyPropertyChanged("PeriodText");
            }
        }

        public HostWindow(int n)
        {
            InitializeComponent();
            this.DataContext = this;
            FlagSending = false;
            FlagListening = false;
            myFlag = false;
            PacketNumber = 0;
            myPeriod = 1000;
            PeriodText = "1";
            sendButton.IsEnabled = true;
            stopButton.IsEnabled = false;

            HostNumber = n;
            Title = "Host" + HostNumber.ToString();
            hostsConfiguration = new Hosts();
            hostsConfiguration = HostWindow.ReadConfig(n);

            hostsConfiguration.DistantHosts.ForEach(distanthost => whereCombo.Items.Add(distanthost));

            Dispatcher.Invoke(new Action(() =>
            {
                LogWindow = new HostLogWindow();
                LogWindow.Title = "Logi host " + HostNumber.ToString();
                LogWindow.Show();
            }));
            LogWindow.logBox.Document.Blocks.Remove(LogWindow.logBox.Document.Blocks.FirstBlock);
            NewLog("Tworzenie hosta", LogWindow);
           
            Task.Run(() => MyConnect(LogWindow));;
        }
        
        public static Hosts ReadConfig(int numberhost)
        {
            Hosts hostsConfiguration = new Hosts();
            XmlReader xmlReader = new XmlReader("ConfigurationHost.xml");
            numberhost--;
            hostsConfiguration.CloudIPAddress = IPAddress.Parse(xmlReader.GetAttributeValue(numberhost, "host", "CLOUD_IP_ADDRESS"));
            hostsConfiguration.CloudPort = Convert.ToUInt16(xmlReader.GetAttributeValue(numberhost, "host", "CLOUD_PORT"));
            hostsConfiguration.HostName = xmlReader.GetAttributeValue(numberhost, "host", "NAME");
            hostsConfiguration.IPAddress = IPAddress.Parse(xmlReader.GetAttributeValue(numberhost, "host", "IP_ADDRESS"));
            hostsConfiguration.OutPort = Convert.ToUInt16(xmlReader.GetAttributeValue(numberhost, "host", "OUT_PORT"));
            hostsConfiguration.DistantHosts = xmlReader.GetItemsForSelectedDistantHost(hostsConfiguration.HostName, "HOST_NAME", "IP_ADDRESS");
            return hostsConfiguration;
        }

        private void MyConnect(HostLogWindow tmp)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                NewLog($"Próba połączenia z chmurą kablową w {hostsConfiguration.CloudIPAddress} port {hostsConfiguration.CloudPort}", tmp);
            }));
            try
            {
                mySocket = new MPLSSocket(hostsConfiguration.CloudIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                mySocket.Connect(new IPEndPoint(hostsConfiguration.CloudIPAddress, hostsConfiguration.CloudPort));
                mySocket.Send(Encoding.ASCII.GetBytes("HELLO "+hostsConfiguration.HostName));
                
                FlagListening = true;
                Dispatcher.Invoke(new Action(() =>
                {
                    NewLog("Udane połączenie z chmurą kablową", tmp); 
                }));
                
                Task.Run(() => { MyListen(tmp); });
            }
            catch (Exception)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    NewLog("Nieudane połączenie z chmurą kablową", tmp);
                }));
            }
        }

        private void MyListen(HostLogWindow tmp)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                NewLog("Rozpoczęto nasłuchiwanie", tmp);
            }));

            while (FlagListening == true)
            {
                while (!mySocket.Connected || mySocket == null)
                {
                    MyConnect(LogWindow);
                }

                try
                {
                    myPacket = mySocket.Receive();

                    if (myPacket != null)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            NewLog($"Otrzymano pakiet nr {myPacket.ID} wiadomość {myPacket.Payload} o time to live = {myPacket.TimeToLive} z portu {myPacket.Port}", tmp);
                        }));
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.Shutdown || e.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            NewLog("Połączenie z chmurą zerwane", tmp);
                        }));
                        continue;
                    }

                    else
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            NewLog("Nie można połączyć się z chmurą", tmp);
                        }));
                    }
                }
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            PacketNumber = 1;
            FlagSending = true;
            //sendButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            NewLog($"Rozpoczęto wysyłanie", LogWindow);
            Task.Run(async () =>
            {
                while (FlagSending == true)
                {
                    MPLSPacket myPacket = new MPLSPacket();

                    myPacket.ID = PacketNumber;
                    PacketNumber++;
                    myPacket.SourceAddress = hostsConfiguration.IPAddress;
                    myPacket.Port = hostsConfiguration.OutPort;
                    myPacket.TimeToLive = --myPacket.TimeToLive;

                    Dispatcher.Invoke(() =>
                    {
                        myPacket.DestinationAddress = ((DistantHosts)whereCombo.SelectedItem).DistantIPAddress;
                        myPacket.Payload = messageText.Text;
                    });

                    mySocket.Send(myPacket);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        NewLog($"Wysłano pakiet nr {myPacket.ID} wiadomość {myPacket.Payload} o time to live = {myPacket.TimeToLive + 1} z portu {myPacket.Port}", LogWindow);
                    }));

                    await Task.Delay(TimeSpan.FromMilliseconds(myPeriod));
                }
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            NewLog("Koniec wysyłania", LogWindow);
            FlagSending = false;
            stopButton.IsEnabled = false;
            sendButton.IsEnabled = true;
        }

        private void WhereCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            whereToSend = (DistantHosts)whereCombo.SelectedItem;
        }

        private void fastButton_Click(object sender, RoutedEventArgs e)
        {
            myPeriod = 500;
            PeriodText = "0,5";
            NewLog("Czas 0,5", LogWindow);
        }

        private void normalButton_Click(object sender, RoutedEventArgs e)
        {
            myPeriod = 1000;
            PeriodText = "1";
            NewLog("Czas 1", LogWindow);
        }

        private void slowButton_Click(object sender, RoutedEventArgs e)
        {
            myPeriod = 5000;
            PeriodText = "5";
            NewLog("Czas 5", LogWindow);
        }
        
        static void NewLog(string info, HostLogWindow tmp2)
        {
            HostLogWindow tmp = tmp2;
            string timeNow = DateTime.Now.ToString("h:mm:ss") + "." + DateTime.Now.Millisecond.ToString();
            string restLog = info;
            string fullLog = timeNow + " " + restLog;
            tmp.logBox.Document.Blocks.Add(new Paragraph(new Run(fullLog)));
            tmp.logBox.ScrollToEnd();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
