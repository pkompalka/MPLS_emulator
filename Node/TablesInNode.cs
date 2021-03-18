using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Utility;

namespace Node
{
    public class TablesInNode
    {
        private Agent myAgent { get; set; }
        private NetworkNodeWindow Window { get; set; }
        public IPTable IpTable { get; set; }
        public LabelSwitchingTable LSTable { get; set; }

        public TablesInNode(NetworkNodeWindow myWindow)
        {
            Window = myWindow;
            LSTable = new LabelSwitchingTable();
            IpTable = new IPTable();
        }

        public void LoadTablesFromConfig(NetworkNode Config, NetworkNodeWindow window)
        {
            myAgent = new Agent(Config, window);
            myAgent.TablesUpdate += TableEventAction;
            window.Dispatcher.Invoke(() => window.Title = Config.NodeName);
        }

        public void StartManagementAgent()
        {
            Task.Run(() => myAgent.StartAgent());
        }

        private void TableEventAction(object sender, TableEvent e)
        {
            switch (e.Action)
            {
                case "FecRecordAdded":
                    LSTable.Records.Add(e.FRecord);
                    NewLog($"Dodana reguła FEC", Window);
                    break;

                case "FecRecordRemove":
                    int indexFec = LSTable.Records.FindIndex(f => (f.InFEC.Equals(e.FRecord.InFEC)) &&
                    (f.InPort.Equals(e.FRecord.InPort)) && 
                    (f.OutFEC.Equals(e.FRecord.OutFEC)) && (f.OutPort.Equals(e.FRecord.OutPort)));

                    LSTable.Records.RemoveAt(indexFec);
                    NewLog("Usunięta reguła FEC", Window);
                    break;

                case "IpRecordAdded":
                    IpTable.Records.Add(e.IRecord);
                    NewLog($"Dodana reguła IP", Window);
                    break;

                case "IpRecordRemove":
                    int indexIp = IpTable.Records.FindIndex(x => (x.PortOut == e.IRecord.PortOut) &&
                    (x.DestinationAddress.Equals(e.IRecord.DestinationAddress)) && (x.NameRouter == e.IRecord.NameRouter));

                    IpTable.Records.RemoveAt(indexIp);
                    NewLog("Usunięta reguła IP", Window);
                    break;
            }
        }

        private void NewLog(string info, NetworkNodeWindow tmp)
        {
            Window.Dispatcher.Invoke(() => Window.NewLog(info, tmp));
        }
    }

    public class TableEvent : EventArgs
    {
        public string Action { get; set; }

        public IPRecord IRecord { get; set; }
        public LabelRecord FRecord { get; set; }

        public TableEvent(string action, IPRecord record)
        {
            Action = action;
            IRecord = record;
        }
        public TableEvent(string action, LabelRecord record)
        {
            Action = action;
            FRecord = record;
        }
    }
}