using Microsoft.HockeyApp;
using System;
using System.Collections.Generic;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Microsoft.Devices.Tpm;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Blink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        enum LedColors
        {
            Green =2,
            Orange = 3, 
            Red = 4
        }

        // change color here
        private const int LED_PIN = (int)LedColors.Orange;

        private int LEDStatus = 0;
        private GpioPin pin;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
        DispatcherTimer mainTimer = null;


        static string deviceConnectionString = String.Empty;
        static DeviceClient Client = null;

        public MainPage()
        {
            this.InitializeComponent();
            InitGPIO();
            mainTimer = new DispatcherTimer();
            mainTimer.Interval = new TimeSpan(0, 0, 1);
            mainTimer.Tick += MainTimer_Tick;
            mainTimer.Start();

            // read connection string from tpm
            ReadTpm();

            // report version data to device twin
            try
            {
                InitClient();
                ReportVersionToDeviceTwin();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }                                  
        }

        private void ReadTpm()
        {
            TpmDevice tpm = new TpmDevice(0);            
            deviceConnectionString = tpm.GetConnectionString();
        }

        public static async void ReportVersionToDeviceTwin()
        {
            try
            {
                Console.WriteLine("Sending version data as reported property");

                TwinCollection reportedProperties, appinfo;
                reportedProperties = new TwinCollection();
                appinfo = new TwinCollection();
                appinfo["appinfo"] = "1.0.#{Build.BuildId}#.0";
                reportedProperties["appinfo"] = appinfo;
                await Client.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }


        public static async void InitClient()
        {
            try
            {
                Console.WriteLine("Connecting to hub");
                Client = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
                Console.WriteLine("Retrieving twin");
                await Client.GetTwinAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }


        private void MainTimer_Tick(object sender, object e)
        {
            FlipLED();

        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }
            pin = gpio.OpenPin(LED_PIN);
            pin.Write(GpioPinValue.High);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            TurnOnLED();

            GpioStatus.Text = "GPIO pin initialized correctly.";
        }

        private string EnumToString(int icolor)
        {
            switch (icolor)
            {
                case 2:
                    return "green";
                case 3:
                    return "orange";
                default:
                    return "red";
            }
        }

        private void FlipLED()
        {
            if (LEDStatus == 0)
            {
                LEDStatus = 1;
                if (pin != null)
                {
                    // to turn on the LED, we need to push the pin 'low'
                    pin.Write(GpioPinValue.Low);
                }
                LED.Fill = redBrush;
                StateText.Text = "On";

                // Hockeytracking
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("color", EnumToString(LED_PIN));
                dic.Add("status", "on");
                HockeyClient.Current.TrackEvent("FlipLed", dic);
            }
            else
            {
                LEDStatus = 0;
                if (pin != null)
                {
                    pin.Write(GpioPinValue.High);
                }
                LED.Fill = grayBrush;
                StateText.Text = "Off";

                // Hockeytracking
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("color", EnumToString(LED_PIN));
                dic.Add("status", "off");
                HockeyClient.Current.TrackEvent("FlipLed", dic);
            }
        }

 
        private void TurnOnLED()
        {
            if (LEDStatus == 0)
            {
                FlipLED();
            }
        }


    }
}
