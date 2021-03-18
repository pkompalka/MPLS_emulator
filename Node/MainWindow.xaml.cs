using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace Node
{
    public partial class MainWindow : MetroWindow
    {
        public int NodeNumber { get; set; }

        public NetworkNodeWindow NodeWindow { get; set; }
        XmlReader reader = new XmlReader("ConfigurationNode.xml");

        public MainWindow()
        {
            InitializeComponent();
        }

        private bool CheckNodeName(string number)
        {
            var isNumber = int.TryParse(number, out int x);
            return isNumber;
        }

        private bool CheckNodeNumber(int number)
        {
            int numberofnodes = reader.GetNumberOfItems("router");
            if (number > 0 && number <= numberofnodes)
            {
                return true;
            }
            else return false;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (numberBox.Text.Length == 0)
            {
                return;
            }
            if (CheckNodeName(numberBox.Text) == false)
            {
                numberBox.Text = "To nie nr!";
                return;
            }
            var tmp = Convert.ToInt32(numberBox.Text);
            if (CheckNodeNumber(tmp) == false)
            {
                numberBox.Text = "Zly nr!";
                return;
            }
            NodeNumber = tmp;
            numberBox.Clear();

            Dispatcher.Invoke(new Action(() =>
            {
                NodeWindow = new NetworkNodeWindow(NodeNumber);
                NodeWindow.Title = "Router " + NodeWindow.ToString();
                NodeWindow.Show();
            }));
        }
    }
}