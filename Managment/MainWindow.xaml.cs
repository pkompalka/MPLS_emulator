using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Net;
using Utility;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Managment
{

    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private NetworkManager networkManager { get; set; }

        private List<IPRecord> IPRecordList { get; set; }

        private List<LabelRecord> FECRecordList { get; set; }

        private ObservableCollection<LabelRecord> fecObservable;
        public ObservableCollection<LabelRecord> FECObservable
        {
            get
            {
                return fecObservable;
            }
            set
            {
                fecObservable = value;
                NotifyPropertyChanged(nameof(FECObservable));
            }
        }

        private ObservableCollection<IPRecord> ipObservable;
        public ObservableCollection<IPRecord> IPObservable
        {
            get
            {
                return ipObservable;
            }
            set
            {
                ipObservable = value;
                NotifyPropertyChanged(nameof(IPObservable));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            networkManager = new NetworkManager();
            IPRecordList = new List<IPRecord>();
            FECRecordList = new List<LabelRecord>();

            logBox.Document.Blocks.Remove(logBox.Document.Blocks.FirstBlock);
            NewLog("Start managera", this);

            try
            {
                networkManager.ReadConfig();
            }
            catch (Exception)
            {

            }

            IPRecordList = networkManager.NodeToIPTable.Values.SelectMany(a => a).ToList();
            FECRecordList = networkManager.NodeToFECTable.Values.SelectMany(a => a).ToList();

            FECObservable = new ObservableCollection<LabelRecord>(FECRecordList);
            IPObservable = new ObservableCollection<IPRecord>(IPRecordList);
            FECgrid.ItemsSource = FECObservable;
            IPgrid.ItemsSource = IPObservable;

            networkManager.Start(this);
        }

        private bool CheckFECandPortCorrectness(string inFEC, string outFEC, string inPort, string outPort)
        {
            var tmp1 = int.TryParse(inFEC, out int m);
            var tmp2 = int.TryParse(outFEC, out int n);
            var tmp3 = int.TryParse(inPort, out int o);
            var tmp4 = int.TryParse(inFEC, out int p);
            if (tmp1 && tmp2 && tmp3 && tmp4)
            {
                if ((m < 0) || (n < 0) || (o < 0) || (p < 0))
                {
                    return false;
                }
            }
            return tmp1 && tmp2 && tmp3 && tmp4;
        }

        private bool CheckDestCorrectness(string destinationAddress)
        {
            var isIP = IPAddress.TryParse(destinationAddress, out IPAddress address);
            return isIP;
        }

        private bool CheckIPPortCorrectness(string outPort)
        {
            var tmp = int.TryParse(outPort, out int n);
            return tmp;
        }

        private void FECRecord_AddButton_Click(object sender, RoutedEventArgs e)
        {
            var routerName = FECRouterTBox.Text;
            var inFEC = inFECTbox.Text;
            var outFEC = outFECTbox.Text;
            var inPort = inPortTbox.Text;
            var outPort = outPortTbox.Text;

            if (inFEC.Length == 0 || outFEC.Length == 0
               || inPort.Length == 0 || outPort.Length == 0)
            {
                return;
            }

            var CorrectFECRule = CheckFECandPortCorrectness(inFEC, outFEC, inPort, outPort);
            if (!CorrectFECRule)
            {
                inFECTbox.Text = "Złe dane";
                inPortTbox.Text = "Złe dane";
                outPortTbox.Text = "Złe dane";
                outFECTbox.Text = "Złe dane";
                return;
            }

            short shortInFEC = Convert.ToInt16(inFEC);
            short shortOutFEC = Convert.ToInt16(outFEC);
            ushort shortInPort = Convert.ToUInt16(inPort);
            ushort shortOutPort = Convert.ToUInt16(outPort);

            var findRecord = FECRecordList.Find(x => (x.NameRouter.Equals(routerName)) && (x.InFEC.Equals(shortInFEC))
            && x.InPort.Equals(shortInPort));
            if (findRecord != null)
            {
                inFECTbox.Text = "Rekord istnieje";
                inPortTbox.Text = "Rekord istnieje";
                outPortTbox.Clear();
                outFECTbox.Clear();
                return;
            }

            var myRecord = new LabelRecord(routerName, shortInFEC, shortOutFEC, shortInPort, shortOutPort);
            if (networkManager.NodeToFECTable.ContainsKey(routerName))
            {
                networkManager.NodeToFECTable[routerName].Add(myRecord);
            }
            else
            {
                List<LabelRecord> tmplist = new List<LabelRecord>
                {
                    myRecord
                };
                networkManager.NodeToFECTable.Add(routerName, tmplist);
            }

            FECRecordList.Add(myRecord);
            FECObservable.Add(myRecord);
            networkManager.SendRecord(routerName, myRecord, "FecRecordAdded");
            FECRouterTBox.Clear();
            inFECTbox.Clear();
            outFECTbox.Clear();
            inPortTbox.Clear();
            outPortTbox.Clear();
        }

        private void FECRecord_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int index = FECgrid.SelectedIndex;
            if (index == -1)
            {
                return;
            }

            var selectedEntry = FECgrid.SelectedItems[0] as LabelRecord;
            var routerName = selectedEntry.NameRouter;
            var inFEC = selectedEntry.InFEC;
            var outFEC = selectedEntry.OutFEC;
            var inPort = selectedEntry.InPort;
            var outPort = selectedEntry.OutPort;

            try
            {
                var findRecord = FECRecordList.Find(x => (x.InFEC.Equals(inFEC) && x.OutFEC.Equals(outFEC)
                && x.InPort.Equals(inPort) && x.OutPort.Equals(outPort)));
                
                networkManager.SendRecord(routerName, findRecord, "FecRecordRemove");

                FECRecordList.Remove(findRecord);
                FECObservable.Remove(findRecord);
                
                var findRecord2 = networkManager.NodeToFECTable[routerName].Find(x => (x.InFEC.Equals(inFEC) && x.OutFEC.Equals(outFEC)
                && x.InPort.Equals(inPort) && x.OutPort.Equals(outPort)));
                networkManager.NodeToFECTable[routerName].Remove(findRecord2);
            }
            catch (Exception)
            {
                NewLog($"Router {routerName} nie jest połączony z zarządzaniem", this);
            }
        }

        private void IPRecord_AddButton_Click(object sender, RoutedEventArgs e)
        {
            var routerName = IPRouterTbox.Text;
            var destAddress = IPDestAdressTbox.Text;
            var outPort = IPOutPortTbox.Text;
            var fecfirst = IPFirstbox.Text;
            if (routerName.Length == 0 || destAddress.Length == 0 || outPort.Length == 0 || fecfirst.Length == 0)
            {
                return;
            }

            if (!CheckDestCorrectness(destAddress))
            {
                IPDestAdressTbox.Text = "Złe IP";
                return;
            }

            if (!CheckIPPortCorrectness(outPort))
            {
                IPOutPortTbox.Text = "Zły port";
                return;
            }

            var findRecord = IPRecordList.Find(x => x.NameRouter.Equals(routerName) &&
            x.DestinationAddress.Equals(IPAddress.Parse(destAddress)) && x.FecFirst.Equals(fecfirst));
            if (findRecord != null)
            {
                IPOutPortTbox.Text = "Rekord istnieje";
                IPDestAdressTbox.Text = "Rekord istnieje";
                return;
            }

            IPAddress tmpAddress = IPAddress.Parse(destAddress);
            ushort shortOutPort = Convert.ToUInt16(outPort);
            short tmpfirst = Convert.ToInt16(fecfirst);
            var myRecord = new IPRecord(routerName, tmpAddress, shortOutPort, tmpfirst);
            
            if (networkManager.NodeToIPTable.ContainsKey(routerName))
            {
                networkManager.NodeToIPTable[routerName].Add(myRecord);
            }
            else
            {
                List<IPRecord> tmplist = new List<IPRecord>
                {
                    myRecord
                };
                networkManager.NodeToIPTable.Add(routerName, tmplist);
            }

            IPRecordList.Add(myRecord);
            IPObservable.Add(myRecord);
            
            networkManager.SendRecord(routerName, myRecord, "IpRecordAdded");

            IPRouterTbox.Clear();
            IPDestAdressTbox.Clear();
            IPOutPortTbox.Clear();
            IPFirstbox.Clear();
        }

        private void IPRecord_DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int index = IPgrid.SelectedIndex;

            if (index == -1) 
            {
                return;
            }
            var selectedEntry = IPgrid.SelectedItems[0] as IPRecord;
            var routerName = selectedEntry.NameRouter;
            var destAddress = selectedEntry.DestinationAddress;
            var outPort = selectedEntry.PortOut;
            var first = selectedEntry.FecFirst;
            try
            {
                var findRecord = IPRecordList.Find(x => x.NameRouter.Equals(routerName) &&
                x.DestinationAddress.Equals(destAddress) && x.PortOut.Equals(outPort) && x.FecFirst.Equals(first));
                
                networkManager.SendRecord(routerName, findRecord, "IpRecordRemove");

                IPRecordList.Remove(findRecord);
                IPObservable.Remove(findRecord);
                
                var item = networkManager.NodeToIPTable[routerName].Find(x => x.NameRouter.Equals(routerName) &&
                x.DestinationAddress.Equals(destAddress) && x.PortOut.Equals(outPort) && x.FecFirst.Equals(first));
                networkManager.NodeToIPTable[routerName].Remove(item);
            }
            catch (Exception)
            {
                NewLog($"Router {routerName} nie jest połączony z zarządzaniem", this);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void NewLog(string info, MainWindow tmp)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                string timeNow = DateTime.Now.ToString("h:mm:ss") + "." + DateTime.Now.Millisecond.ToString();
                string restLog = info;
                string fullLog = timeNow + " " + restLog;
                tmp.logBox.Document.Blocks.Add(new Paragraph(new Run(fullLog)));
                tmp.logBox.ScrollToEnd();
            }));
        }
    }
}