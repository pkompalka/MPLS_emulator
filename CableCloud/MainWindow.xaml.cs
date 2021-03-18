using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace CableCloud
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public Cloud myCableCloud;

        public MainWindow()
        {
            InitializeComponent();
            myCableCloud = new Cloud();
            DataContext = myCableCloud;
        }

        private bool AutoScroll = true;

        private void ScrollViewer_ScrollChanged(Object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {   
                if (myScroll.VerticalOffset == myScroll.ScrollableHeight)
                {   
                    AutoScroll = true;
                }
                else
                {  
                    AutoScroll = false;
                }
            }
            
            if (AutoScroll && e.ExtentHeightChange != 0)
            { 
                myScroll.ScrollToVerticalOffset(myScroll.ExtentHeight);
            }
        }
    }
}
