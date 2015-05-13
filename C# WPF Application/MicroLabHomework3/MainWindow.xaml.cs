using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Gauges;
using System.IO.Ports;
using Newtonsoft.Json;


namespace MicroLabHomework3
{
    public partial class MainWindow : DXWindow
    {
        SerialPort port;
        public MainWindow()
        {
            port = new SerialPort("COM11", 9600, Parity.None, 8, StopBits.One);
            InitializeComponent();
            TryOpenPort();
            port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        private void TryOpenPort()
        {
            try
            {
                port.Open();
                btnConnect.Background = Brushes.Green;
                btnConnect.IsEnabled = false;
            }
            catch (Exception)
            {
                btnConnect.Background = Brushes.Gray;
                btnConnect.IsEnabled = true;
            }
        }
        private static void DataReceivedHandler(
                      object sender,
                      SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;            
            string indata = sp.ReadLine();
            Data d = JsonConvert.DeserializeObject<Data>(indata);
        }
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            TryOpenPort();
        }

        private void btnZero_Click(object sender, RoutedEventArgs e)
        {
            sliderPWM.Value = 5.0000;
        }

        private void sliderPWM_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (btnConnect.IsEnabled == false)
            {
                byte[] data = new byte[4]{
                0xFF,
                0x00,
                0x00,
                0x00
                };
                if (sliderPWM.Value<5.00)
                {
                    data[1] = 0x10;
                    data[2] = (byte)Map(sliderPWM.Value, 5, 0, 0, 255);
                }
                else
                {
                    data[1] = 0x01;
                    data[2] = (byte)Map(sliderPWM.Value, 5, 10, 0, 255);
                }
                data[3] =(byte) (data[1] ^ data[2]);
                TryWritePort(data);
                
            }
        }

        private void TryWritePort(byte[] data)
        {
            try
            {
                port.Write(data, 0, data.Length);
            }
            catch (Exception)
            {
                btnConnect.Background = Brushes.Gray;
                btnConnect.IsEnabled = true;
                MessageBox.Show("Connection Lost!", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public double Map(double Value, double FromLow, double FromHigh, double ToLow, double ToHigh)
        {
            double Result;
            Result = (((Value - FromLow) * (ToHigh - ToLow)) / (FromHigh - FromLow)) + ToLow;

            return Result;
        }
    }


}
