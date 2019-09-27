using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;

namespace UsbWriteProtectService
{
    public partial class UsbWriteProtectService : ServiceBase
    {
        private readonly Guid GUID_DEVCLASS_USB = Guid.Parse("4d36e967-e325-11ce-bfc1-08002be10318");
        private readonly List<KeyValuePair<string, string>> whitelistedUsbDevices;

        private ServiceController serviceController;
        private ManualResetEvent serviceEvent;
        private ManagementEventWatcher usbDeviceWatcher;
        private WqlEventQuery usbDeviceQuery;
        private ManagementEventWatcher usbDriveWatcher;
        private WqlEventQuery usbDriveQuery;
        private Thread serviceChecker;
        private bool serviceIsRunning;

        private object usbDeviceLock = new object();

        public UsbWriteProtectService(List<KeyValuePair<string, string>> whitelistedUsbDevices)
        {
            InitializeComponent();

            this.whitelistedUsbDevices = whitelistedUsbDevices;
        }

        private void UsbDeviceWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var device = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            if (device["PNPClass"].ToString() == "DiskDrive")
            {
                switch (e.NewEvent.ClassPath.ClassName)
                {
                    case "__InstanceCreationEvent":
                        try
                        {
                            if (!CheckDeviceStatus(device.GetPropertyValue("PNPDeviceID").ToString()))
                            {
                                DeviceHelper.SetDeviceEnabled(GUID_DEVCLASS_USB, device.GetPropertyValue("PNPDeviceID").ToString(), true);
                                CheckDeviceStatus(device.GetPropertyValue("PNPDeviceID").ToString(), true, true);
                            }
                        }
                        catch
                        {
                        }
                        break;

                    case "__InstanceDeletionEvent":
                        break;
                }
            }
        }

        private void UsbDeviceWatcher_Stopped(object sender, StoppedEventArgs e)
        {
            usbDeviceWatcher.Dispose();
        }

        private void UsbDriveWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var device = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            if (device["InterfaceType"].ToString() == "USB")
            {
                switch (e.NewEvent.ClassPath.ClassName)
                {
                    case "__InstanceCreationEvent":
                        EnableWriteProtect();

                        var usbDevice = new UsbDevice();
                        var usbDeviceDetail = GetVidPidFromUsbDevice(device.GetPropertyValue("PNPDeviceID").ToString());

                        usbDevice.Caption = device["Caption"].ToString();
                        usbDevice.VendorId = usbDeviceDetail.VendorId;
                        usbDevice.ProductId = usbDeviceDetail.ProductId;
                        usbDevice.SerialNumber = device["SerialNumber"].ToString();

                        if (IsUsbDeviceWhitelisted(usbDevice))
                        {
                            string driveLetter;
                            var isUsbWriteProtected = CheckWriteProtection(device.GetPropertyValue("DeviceID").ToString(), out driveLetter);

                            if (isUsbWriteProtected)
                            {
                                while (isUsbWriteProtected)
                                {
                                    try
                                    {
                                        DeviceHelper.SetDeviceEnabled(GUID_DEVCLASS_USB, device.GetPropertyValue("PNPDeviceID").ToString(), false);
                                        CheckDeviceStatus(device.GetPropertyValue("PNPDeviceID").ToString(), true, false);
                                    }
                                    catch
                                    {
                                    }

                                    DisableWriteProtect();

                                    try
                                    {
                                        DeviceHelper.SetDeviceEnabled(GUID_DEVCLASS_USB, device.GetPropertyValue("PNPDeviceID").ToString(), true);
                                        CheckDeviceStatus(device.GetPropertyValue("PNPDeviceID").ToString(), true, true);
                                    }
                                    catch
                                    {
                                    }

                                    isUsbWriteProtected = CheckWriteProtection(device.GetPropertyValue("DeviceID").ToString(), out driveLetter);

                                    if (driveLetter == null)
                                    {
                                        isUsbWriteProtected = false;
                                    }
                                }

                                EnableWriteProtect();
                            }
                        }
                        break;

                    case "__InstanceDeletionEvent":
                        break;
                }
            }
        }

        private void UsbDriveWatcher_Stopped(object sender, StoppedEventArgs e)
        {
            usbDriveWatcher.Dispose();

            serviceIsRunning = false;
            serviceEvent.Set();
        }

        public void StartConsole(string[] args)
        {
            OnStart(args);
        }

        public void StopConsole()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            serviceController = new ServiceController("Winmgmt");
            serviceEvent = new ManualResetEvent(true);

            usbDeviceQuery = new WqlEventQuery();
            usbDeviceQuery.EventClassName = "__InstanceOperationEvent";
            usbDeviceQuery.WithinInterval = new TimeSpan(0, 0, 1);
            usbDeviceQuery.Condition = @"TargetInstance ISA 'Win32_PnPEntity'";

            usbDriveQuery = new WqlEventQuery();
            usbDriveQuery.EventClassName = "__InstanceOperationEvent";
            usbDriveQuery.WithinInterval = new TimeSpan(0, 0, 1);
            usbDriveQuery.Condition = @"TargetInstance ISA 'Win32_DiskDrive'";

            serviceChecker = new Thread(CheckService);
            serviceChecker.IsBackground = true;
            serviceChecker.Start();

            EnableWriteProtect();
            DisableAutoRun();
        }

        protected override void OnStop()
        {
            DisableWriteProtect();
            EnableAutoRun();
        }

        private void StartUsbWatcher()
        {
            usbDeviceWatcher = new ManagementEventWatcher();

            usbDeviceWatcher.EventArrived += UsbDeviceWatcher_EventArrived;
            usbDeviceWatcher.Stopped += UsbDeviceWatcher_Stopped;
            usbDeviceWatcher.Query = usbDeviceQuery;

            usbDeviceWatcher.Start();

            usbDriveWatcher = new ManagementEventWatcher();

            usbDriveWatcher.EventArrived += UsbDriveWatcher_EventArrived;
            usbDriveWatcher.Stopped += UsbDriveWatcher_Stopped;
            usbDriveWatcher.Query = usbDriveQuery;

            usbDriveWatcher.Start();
        }

        private void CheckService()
        {
            serviceIsRunning = serviceController.Status == ServiceControllerStatus.Running;

            while (true)
            {
                serviceEvent.WaitOne();

                if (serviceIsRunning)
                {
                    StartUsbWatcher();
                    serviceEvent.Reset();
                }
                else
                {
                    serviceController.WaitForStatus(ServiceControllerStatus.Running);
                    serviceIsRunning = true;
                }
            }
        }

        private dynamic GetVidPidFromUsbDevice(string usbDevice)
        {
            var usbControllerDevices = new ManagementObjectSearcher("SELECT * FROM Win32_USBControllerDevice");
            string vid = null, pid = null, previousAntecedent = null, previousDependent = null;

            foreach (var usbControllerDevice in usbControllerDevices.Get())
            {
                var currentAntecedent = usbControllerDevice["Antecedent"].ToString();
                var currentDependent = usbControllerDevice["Dependent"].ToString();

                if (currentDependent.IndexOf(string.Format("Win32_PnPEntity.DeviceID=\"{0}\"", usbDevice.Replace(@"\", @"\\"))) > -1)
                {
                    var pattern = new Regex(@"VID_(?<VID>\w+)&PID_(?<PID>\w+)");
                    Match match = null;

                    if (pattern.IsMatch(currentDependent))
                    {
                        match = pattern.Match(currentDependent);
                    }
                    else if (currentAntecedent == previousAntecedent && pattern.IsMatch(previousDependent))
                    {
                        match = pattern.Match(previousDependent);
                    }

                    if (match != null)
                    {
                        vid = match.Groups["VID"].Value;
                        pid = match.Groups["PID"].Value;
                        break;
                    }
                }

                previousAntecedent = currentAntecedent;
                previousDependent = currentDependent;
            }

            return new { VendorId = vid, ProductId = pid };
        }

        private bool CheckDeviceStatus(string device, bool wait = false, bool enabled = true)
        {
            bool status = false;

            do
            {
                var pnpDevice = new ManagementObjectSearcher(string.Format("SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{0}'", device.Replace(@"\", @"\\")));

                foreach (var pnpEntity in pnpDevice.Get())
                {
                    status = pnpEntity["Status"].ToString() == "OK";

                    if (wait)
                    {
                        if (enabled && status)
                        {
                            wait = false;
                        }
                        else if (!status)
                        {
                            wait = false;
                        }
                    }
                }
            } while (wait);

            return status;
        }

        private bool CheckWriteProtection(string device, out string driveLetter)
        {
            driveLetter = null;
            var isUsbWriteProtected = true;

            try
            {
                foreach (var partition in new ManagementObjectSearcher(string.Format("ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{0}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition", device)).Get())
                {
                    foreach (var disk in new ManagementObjectSearcher(string.Format("ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{0}'}} WHERE AssocClass = Win32_LogicalDiskToPartition", partition["DeviceID"])).Get())
                    {
                        driveLetter = disk["Name"].ToString();
                        var file = Path.Combine(driveLetter, Path.GetRandomFileName());

                        using (File.Create(file))
                        {
                        }
                        File.Delete(file);

                        isUsbWriteProtected = false;
                    }
                }
            }
            catch
            {
            }

            return isUsbWriteProtected;
        }

        private void EnableWriteProtect()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", true);

            if (key == null)
            {
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", RegistryKeyPermissionCheck.ReadWriteSubTree);
                key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", true);
                key.SetValue("WriteProtect", 1, RegistryValueKind.DWord);
            }
            else if (key.GetValue("WriteProtect") != (object)(1))
            {
                key.SetValue("WriteProtect", 1, RegistryValueKind.DWord);
            }

            key.Close();
        }

        private void DisableWriteProtect()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\StorageDevicePolicies", true);
            
            if (key != null)
            {
                key.SetValue("WriteProtect", 0, RegistryValueKind.DWord);
            }

            key.Close();
        }

        private void EnableAutoRun()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);

            if (key != null)
            {
                key.SetValue("NoDriveTypeAutoRun", 0x00, RegistryValueKind.DWord);
            }

            key.Close();
        }

        private void DisableAutoRun()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);

            if (key != null)
            {
                key.SetValue("NoDriveTypeAutoRun", 0xFF, RegistryValueKind.DWord);
            }

            key.Close();
        }

        private void EnableAllStorageDevices()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\UsbStor", true);

            if (key != null)
            {
                key.SetValue("Start", 3, RegistryValueKind.DWord);
            }

            key.Close();
        }

        private void DisableAllStorageDevices()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\UsbStor", true);

            if (key != null)
            {
                key.SetValue("Start", 4, RegistryValueKind.DWord);
            }

            key.Close();
        }

        private void EnableRegedit()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);

            key.SetValue("DisableRegistryTools", 0, RegistryValueKind.DWord);

            key.Close();
        }

        private void DisableRegedit()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);

            key.SetValue("DisableRegistryTools", 1, RegistryValueKind.DWord);

            key.Close();
        }

        private bool IsUsbDeviceWhitelisted(UsbDevice usbDevice)
        {
            var isWhitelisted = false;

            foreach (var whitelistedUsbDevice in whitelistedUsbDevices)
            {
                switch (whitelistedUsbDevice.Key)
                {
                    case "Caption":
                        if (usbDevice.Caption == whitelistedUsbDevice.Value)
                        {
                            isWhitelisted = true;
                        }
                        break;

                    case "VendorId":
                        if (usbDevice.VendorId == whitelistedUsbDevice.Value)
                        {
                            isWhitelisted = true;
                        }
                        break;

                    case "ProductId":
                        if (usbDevice.ProductId == whitelistedUsbDevice.Value)
                        {
                            isWhitelisted = true;
                        }
                        break;

                    case "SerialNumber":
                        if (usbDevice.SerialNumber == whitelistedUsbDevice.Value)
                        {
                            isWhitelisted = true;
                        }
                        break;
                }
            }

            if (isWhitelisted)
            {
                Console.WriteLine("Whitelisted USB device");
            }
            else
            {
                Console.WriteLine("Blacklisted USB device");
            }

            Console.WriteLine(usbDevice);
            Console.WriteLine();

            return isWhitelisted;
        }
    }
}
