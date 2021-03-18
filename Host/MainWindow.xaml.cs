using MahApps.Metro.Controls;
using System;
using System.Windows;

namespace Host
{
    public partial class MainWindow : MetroWindow
    {
        public int HostNumber { get; set; }

        public HostWindow HostWindow { get; set; }

        XmlReader reader = new XmlReader("ConfigurationHost.xml");

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private bool CheckHostName(string number)
        {
            var isNumber = int.TryParse(number, out int x);
            return isNumber;
        }

        private bool CheckHostNumber(int number)
        {
            int numberofhosts = reader.GetNumberOfItems("host");
            if (number > 0 && number <= numberofhosts)
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
            if (CheckHostName(numberBox.Text) == false)
            {
                numberBox.Text = "To nie nr!";
                return;
            }
            var tmp = Convert.ToInt32(numberBox.Text);
            if (CheckHostNumber(tmp) == false)
            {
                numberBox.Text = "Zly nr!";
                return;
            }

            HostNumber = tmp;
            numberBox.Clear();
            Dispatcher.Invoke(new Action(() =>
            {
                HostWindow = new HostWindow(HostNumber);
                HostWindow.Title = "Host " + HostNumber.ToString();
                HostWindow.Show();
            }));
        }
    }
}