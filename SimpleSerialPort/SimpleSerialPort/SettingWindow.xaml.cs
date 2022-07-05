using System;
using System.Collections.Generic;
using System.IO.Ports;
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
using System.Windows.Shapes;

namespace SimpleSerialPort
{
    /// <summary>
    /// Interaction logic for SettingWindow.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        MainWindow mainwin = ((MainWindow)Application.Current.MainWindow);
        private SerialPort ThePort = ((MainWindow)Application.Current.MainWindow).ThePort;

        public SettingWindow()
        {
            InitializeComponent();
        }

        public void RefreshPorts(object sender, RoutedEventArgs e)
        {
            PortBox.Items.Clear();                              //Clears existing ports
            string[] portNames = SerialPort.GetPortNames();     // Reads all available comPorts
            foreach (var portName in portNames)
            {
                PortBox.Items.Add(portName);                   // Adds Ports to combobox
            }
            //DebugBox.SelectedIndex = 0;
        }

        private void OK_Btn_Click(object sender, RoutedEventArgs e)
        {
            string connectionStr = PortBox.Text + ": " + BaudBox.Text + " bps, " + DatabitsBox.Text + ParityBox.Text[0] + (StopbitsBox.Text == "One" ? '1' : StopbitsBox.Text == "Two" ? '2' : StopbitsBox.Text == "None" ? '0' : "1.5");
            connectionStr += ", Handshake: " + HandshakeBox.Text;
            mainwin.ConnectionBox.Text = connectionStr;

            if(PortBox.Text != "")
            {
                mainwin.DebugConnectBtn.IsEnabled = true;
            }

            this.Close();
        }

        
    }
}
