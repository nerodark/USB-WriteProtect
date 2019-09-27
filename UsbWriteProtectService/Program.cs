using System;
using System.Collections.Generic;
using System.ServiceProcess;

namespace UsbWriteProtectService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var whitelistedUsbDevices = new List<KeyValuePair<string, string>>();

            // 4 different types of whitelisting: by caption, vendor id, product id or serial number.

            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("Caption", "Samsung Flash Drive FIT USB Device"));
            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("Caption", "Ut165 USB2FlashStorage USB Device"));
            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("VendorId", "0B27"));
            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("ProductId", "0165"));
            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("SerialNumber", "00000000001A33"));

            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("Caption", "General USB Flash Disk USB Device"));
            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("VendorId", "8644"));
            //whitelistedUsbDevices.Add(new KeyValuePair<string, string>("ProductId", "800E"));
            whitelistedUsbDevices.Add(new KeyValuePair<string, string>("SerialNumber", "13730000000385D0"));

            if (Environment.UserInteractive)
            {
                var service = new UsbWriteProtectService(whitelistedUsbDevices);
                service.StartConsole(args);
                Console.Read();
                service.StopConsole();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new UsbWriteProtectService(whitelistedUsbDevices)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
