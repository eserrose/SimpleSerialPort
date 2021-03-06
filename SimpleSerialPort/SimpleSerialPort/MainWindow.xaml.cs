using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.IO.Ports;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Linq;

namespace SimpleSerialPort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly private string cmdLogPath = AppDomain.CurrentDomain.BaseDirectory + "/log/commands";
        public string LogPath = AppDomain.CurrentDomain.BaseDirectory + "log";

        private SettingWindow settingWindow;

        public SerialPort ThePort = new SerialPort();
        byte[] rxBuffer;
        bool enableScroll = true; //todo: add this functionality
        int readCtr = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UART_Connect_Click(object sender, RoutedEventArgs e)
        {
            if (ThePort.IsOpen)
            {
                ThePort.Close();
                DebugStatus.Fill = Brushes.Red;
                DebugConnectBtn.Content = "Connect";
                return;
            }

            ThePort.PortName = settingWindow.PortBox.Text;
            ThePort.BaudRate = Int32.Parse(settingWindow.BaudBox.Text);
            ThePort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), settingWindow.HandshakeBox.Text);
            ThePort.Parity = (Parity) Enum.Parse(typeof(Parity), settingWindow.ParityBox.Text);
            ThePort.DataBits = Int32.Parse(settingWindow.DatabitsBox.Text);
            ThePort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), settingWindow.StopbitsBox.Text);

            ThePort.ReadBufferSize = 20971520;
            ThePort.ReadTimeout = 200;
            ThePort.WriteTimeout = 150;
            ThePort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            rxBuffer = new byte[5000000];

            try
            {
                ThePort.Open();
            }
            catch
            {
                LogError("Could not connect\n");
            }

            if (ThePort.IsOpen)
            {
                DebugStatus.Fill = Brushes.Green;
                DebugConnectBtn.Content = "Disconnect";
            }
            else MessageBox.Show("Could not connect to " + settingWindow.PortBox.Text);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingWindow != null && PresentationSource.FromVisual(settingWindow) != null)
                return;
               
            settingWindow = new SettingWindow();
            settingWindow.Show();
        }
        private void ClearText_Click(object sender, RoutedEventArgs e)
        {
            rxBox.Document.Blocks.Clear();
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int readCount = sp.BytesToRead;
            int rxBufferCount = 0;

            for (int i = 0; i < readCount; i++)
            {
                rxBuffer[rxBufferCount++] = Convert.ToByte(sp.ReadByte());
            }

            string indata;
            if(hexOutput())
                indata = BitConverter.ToString(rxBuffer, 0, rxBufferCount);
            else
                indata = Encoding.Default.GetString(rxBuffer, 0, (int) rxBufferCount);

            string color = "blue"; //TODO: Add customization and dark mode

            RichTextBoxDelegate(indata, color);

            if (saveLog())
            {
                saveReceived(indata);
            }
          
        }
        private string GetDelimiter()
        {
            string checkedText = settingWindow?.optsGroup.Children.OfType<RadioButton>()
                                      .FirstOrDefault(r => r.IsChecked == true)?.Content.ToString();
            switch (checkedText)
            {
                case "Append Nothing":
                    return "";
                case "Append CR":
                    return "\r";
                case "Append LF":
                    return "\n";
                case "Append CR-LF":
                    return "\r\n";
            }

            return "\n";
        }

        private bool encodeB64()
        {
            return Dispatcher.Invoke(() => settingWindow.b64check.IsChecked == true);
        }

        private bool saveLog()
        {
            return Dispatcher.Invoke(() => !((LogPath = settingWindow.logpath.Text) == "") );
        }

        private bool localEcho()
        {
            return Dispatcher.Invoke(() => settingWindow?.echocheck.IsChecked == true);
        }

        private bool hexOutput()
        {
            return Dispatcher.Invoke(() => settingWindow?.hexcheck.IsChecked == true);
        }

        public void OnDebugTX(object sender, RoutedEventArgs e)
        {
            string delimiter = GetDelimiter();
            string cmd = txBox.Text + delimiter;
            byte[] bytes = Encoding.ASCII.GetBytes(cmd);

            if (bytes.Length > 0)
            {
                if(localEcho())
                    LogInfo("You: " + cmd);

                try
                {
                    ThePort.Write(bytes, 0, bytes.Length);
                    saveSentCmd(txBox.Text);
                }
                catch
                {
                    LogError( (delimiter == "" ? '\n' : "") + "Could not send command\n");
                }

                txBox.Text = "";
                readCtr = 0;
            }
        }

        public void OnDebugEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnDebugTX(sender, e);
            }
            else if (e.Key == Key.Up)
            {
                txBox.Text = GetOldCmd(++readCtr);
            }
            else if (e.Key == Key.Down)
            {
                txBox.Text = GetOldCmd(--readCtr);
            }
        }

        private void saveSentCmd(string text)
        {

            if (!File.Exists(cmdLogPath))
            {
                string dir = System.AppDomain.CurrentDomain.BaseDirectory + "/log";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using (StreamWriter sw = File.CreateText(cmdLogPath))
                {
                    sw.WriteLine(text);
                }
            }
            else
            {
                List<string> newLines = new List<string>();
                //check if already exists
                using (StreamReader sr = File.OpenText(cmdLogPath))
                {
                    string[] lines = File.ReadAllLines(cmdLogPath);
                    for (int x = 0; x < lines.Length; x++)
                    {
                        if (lines[x] != text)
                        {
                            newLines.Add(lines[x]);
                        }
                    }
                }

                newLines.Add(text);
                File.WriteAllLines(cmdLogPath, newLines);
            }
        }

        private void saveReceived(string text)
        {
            string logFileName = "/logfile.txt";

            string filePath = LogPath + logFileName;

            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);

                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine(text);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine(text);
                }
            }
        }

        private string GetOldCmd(int v)
        {
            if (File.Exists(cmdLogPath))
            {
                using (StreamReader sr = File.OpenText(cmdLogPath))
                {
                    string[] readText = File.ReadAllLines(cmdLogPath);
                    if (v > readText.Length) readCtr = readText.Length;
                    else if (v < 1) readCtr = 1;
                    return readText[readText.Length - readCtr];
                }
            }

            return "";
        }

        public void SendFile(object sender, RoutedEventArgs e)
        {
            byte[] toSend;
            byte[] fileData;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                fileData = File.ReadAllBytes(openFileDialog.FileName);
            else
                return;

            if (encodeB64())
            {
                string encoded = Convert.ToBase64String(fileData);
                toSend = Encoding.UTF8.GetBytes(encoded);
            } 
            else
            {
                toSend = fileData;
            }

            try
            {
                for (int i = 0; i < toSend.Length; i++)
                    ThePort.Write(toSend, i, 1);
            } catch 
            {
                MessageBox.Show("Could not send file.");

            }
        }

        private void RichTextBoxDelegate(string str, string color)
        {
            Dispatcher.Invoke(() =>
            {
                if (str[str.Length - 1] != '\n' || str[str.Length - 1] != '\r') 
                    str += "\n";

                rxBox.AppendText("[" + DateTime.Now.ToString("h:mm:ss") + "] " + str, color);

                if(enableScroll)
                    rxBox.ScrollToEnd();
                //rxBox.Document.Blocks.Add(new Paragraph(new Run("[" + DateTime.Now.ToString("h:mm:ss") + "] " + str + "\n")));
            });
        }

        private void LogInfo(string str)
        {
            RichTextBoxDelegate(str, "darkgreen");
        }
        private void LogWarn(string str)
        {
            RichTextBoxDelegate(str, "Goldenrod");
        }
        private void LogSystem(string str)
        {
            RichTextBoxDelegate(str, "olive");
        }
        private void LogError(string str)
        {
            RichTextBoxDelegate(str, "red");
        }
        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
                hex.AppendFormat("{0:X2} ", b);
            return hex.ToString();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            this.Topmost = (settingWindow?.ontopcheck.IsChecked == true);
        }
    }
}

public static class RichTextBoxExtensions
{
    public static void AppendText(this RichTextBox box, string text, string color)
    {
        BrushConverter bc = new BrushConverter();
        TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
        tr.Text = text;
        try
        {
            tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                bc.ConvertFromString(color));
        }
        catch (FormatException) { }
    }
}

