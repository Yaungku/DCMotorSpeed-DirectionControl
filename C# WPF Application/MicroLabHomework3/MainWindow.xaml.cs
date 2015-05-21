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
    /*
     * Communication will include only one byte that has 1 bit for direction and 7 bit for PWM duty.
     *           _ | _ _ _ _ _ _ _
     *          dir     PWM / 2 (PWM is 8 bit [0 - 255] so we will slide one bit and lose sensitivity)
     *          
     *          Microcontroller will select this 7 bit and multiply with two again and it will know the value of PWM Duty.  
     * * */
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
        private void DataReceivedHandler(
                      object sender,
                      SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;            
            string indata = sp.ReadLine();
            /*
            Data d = JsonConvert.DeserializeObject<Data>(indata);
            txtTemp.Content = d.Temprature;
            txtPWM.Content = d.PWMDuty;
             * */
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
            //Is serial port connected? 
            if (btnConnect.IsEnabled == false)
            {
                byte[] data = new byte[1];
                data[0] = (byte)((byte)Map(sliderPWM.Value, 5, 0, 0, 255) >> 1);
                if (sliderPWM.Value<5.00)
                {
                    //7. bit to False 
                    data[0] = (byte)(data[0] & 0x7F);                    
                }
                else
                {
                    //7. bit to True 
                    data[0] = (byte)(data[0] | 0x80);
                }
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
