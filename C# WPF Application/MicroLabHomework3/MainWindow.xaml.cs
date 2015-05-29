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
using System.Windows.Threading;


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
        DispatcherTimer dispatcherTimer;
        byte[] data;
        public MainWindow()
        {
            // COM Port can be change in this row
            port = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);
            InitializeComponent();
            TryOpenPort();
            port.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            //  DispatcherTimer setup
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            // Set dispatcherTimer_Tick method will be called in every one second
            dispatcherTimer.Interval = new TimeSpan(0, 0,0,1,0);
            dispatcherTimer.Start();
        }
        // this method checks that the value of PWM duty from microcontroller is equal to user interface settings
        // if not, the method send the value again. 
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtPWM.Text))
            {
                if (data != null)
                {
                    byte checkdata = (byte)(data[0] & 0x7F);
                    checkdata = (byte)(checkdata << 1);
                    if (Convert.ToInt32(txtPWM.Text) != (int)checkdata)
                    {
                        TryWritePort(data);
                    }
                }           
            }
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
            try
            {
                Data d = JsonConvert.DeserializeObject<Data>(indata);
                this.Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        txtTemp.Text = d.Temprature.Trim();
                        txtPWM.Text = d.PWMDuty.Trim();
                    }
                    catch (Exception)
                    {
                        txtError.Text += "Json string could not parse to text!\n";
                    }

                }));

            }
            catch (Exception)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    txtError.Text += "Json string could not parse to text!\n";
                }));
               
            }

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
                data = new byte[1];

                if (sliderPWM.Value < 5.00)
                {
                    data[0] = (byte)((byte)Map(sliderPWM.Value, 5, 0, 0, 255) >> 1);
                    //7. bit to False 
                    data[0] = (byte)(data[0] & 0x7F);
                }
                else
                {
                    data[0] = (byte)((byte)Map(sliderPWM.Value, 5, 10, 0, 255) >> 1);
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
