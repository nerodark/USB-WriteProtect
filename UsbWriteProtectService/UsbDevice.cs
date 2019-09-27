using System;

namespace UsbWriteProtectService
{
    class UsbDevice
    {
        public string Caption { get; set; }
        public string VendorId { get; set; }
        public string ProductId { get; set; }
        public string SerialNumber { get; set; }

        public override string ToString()
        {
            return "Caption: " + Caption + Environment.NewLine
                + "VendorId: " + VendorId + Environment.NewLine
                + "ProductId: " + ProductId + Environment.NewLine
                + "SerialNumber: " + SerialNumber;
        }
    }
}
