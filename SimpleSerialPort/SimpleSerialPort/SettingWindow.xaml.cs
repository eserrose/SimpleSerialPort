using System;
using System.Collections.Generic;
using System.IO;
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

        readonly private string iniFilePath = AppDomain.CurrentDomain.BaseDirectory + "/ssp.ini";

        public SettingWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            string[] config;

            if (!File.Exists(iniFilePath))
            {
                saveCurrentConfig();
                return;
            }

            using (StreamReader sr = File.OpenText(iniFilePath))
            {
                config = File.ReadAllLines(iniFilePath);
            }

            RefreshPorts();

            if (PortBox.Items.Contains(config[0]))
                PortBox.Text = config[0];

            BaudBox.Text = config[1]; 
            DatabitsBox.Text = config[2];
            ParityBox.Text = config[3];
            StopbitsBox.Text = config[4];
            HandshakeBox.Text = config[5];

            optsGroup.Children.OfType<RadioButton>().FirstOrDefault(radio => radio.Content.ToString() == config[6]).IsChecked = true;

            foreach (var (box,i) in optsGroup.Children.OfType<CheckBox>().Select((box, i) => (box, i)))
            {
                if(config[i + 7] == "1")
                {
                    box.IsChecked = true;
                }
            }

        }

        private string getCurrentConfig()
        {
            string config = PortBox.Text + "\n" + BaudBox.Text + "\n" + DatabitsBox.Text + "\n" + ParityBox.Text + "\n" + StopbitsBox.Text + "\n" + HandshakeBox.Text + "\n";
            config += optsGroup.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Content.ToString() + "\n";
            config += string.Join("\n", optsGroup.Children.OfType<CheckBox>().Select(box => (box.IsChecked == true ? "1" : "0")));

            return config;
        }

        private void saveCurrentConfig()
        {
            using (StreamWriter sw = File.CreateText(iniFilePath))
            {
                sw.Write(getCurrentConfig());
            }
        }

        public void RefreshPorts(object sender, RoutedEventArgs e)
        {
            RefreshPorts();
        }

        public void RefreshPorts()
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

            if(monospacecheck.IsChecked == true)
            {
                mainwin.rxBox.FontFamily = new FontFamily("Courier New");
            }

            saveCurrentConfig();

            this.Close();
        }

        private void Cancel_Btn_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Take a snapshot of every value during initialization, and restore them upon clicking Cancel
            this.Close();
        }

        private void LogBtnClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                logpath.Text = dialog.SelectedPath;
                logpath.IsEnabled = true;
            }

        }

        private void LogPathUp(object sender, RoutedEventArgs e)
        {
            if(logpath.Text == "")
                logpath.IsEnabled = false;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            this.Topmost = (ontopcheck.IsChecked == true);
        }

    }
}
