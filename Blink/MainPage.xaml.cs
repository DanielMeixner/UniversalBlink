using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Devices.Management;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Blink
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {        DispatcherTimer mainTimer = null; 
        public MainPage()
        {
            this.InitializeComponent();
            InitGPIO();
            mainTimer = new DispatcherTimer();
            mainTimer.Interval = new TimeSpan(0, 0, 1);
            mainTimer.Tick += MainTimer_Tick;
            mainTimer.Start();
            SendVersionNumberAsync().Wait();
        }

        public async Task SendVersionNumberAsync()
        {
            TpmDevice myDevice = new TpmDevice(0); // Use logical device 0 on the TPM
            string hubUri = await  myDevice.GetConnectionStringAsync();
            string deviceId = await myDevice.GetDeviceIdAsync();
            string sasToken = await myDevice.GetSASTokenAsync();

            var deviceClient = DeviceClient.Create(
                hubUri,
                AuthenticationMethodFactory.
                    CreateAuthenticationWithToken(deviceId, sasToken), TransportType.Amqp);


            TwinCollection reportedProperties, appinfo;
            reportedProperties = new TwinCollection();
            appinfo = new TwinCollection();
            appinfo["versionnumber"] = "1.0.#{Build.BuildId}#.0";
            reportedProperties["appinfo"] = appinfo;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

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
            }
        }

        private void TurnOffLED()
        {
            if (LEDStatus == 1)
            {
                FlipLED();
            }
        }
        private void TurnOnLED()
        {
            if (LEDStatus == 0)
            {
                FlipLED();
            }
        }

        private int LEDStatus = 0;
        private const int LED_PIN = 2;
        private GpioPin pin;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);
    }
}
