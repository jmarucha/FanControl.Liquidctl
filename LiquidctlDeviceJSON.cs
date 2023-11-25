using System;
namespace FanControl.Liquidctl
{
    public class LiquidctlDeviceJSON
    {
        public string description { get; set; }

        public long? vendor_id { get; set; }

        public long? product_id { get; set; }

        public long? release_number { get; set; }

        public string port { get; set; }

        public string serial_number { get; set; }

        public string driver { get; set; }

        public string bus { get; set; }

        public string address { get; set; }

        public bool experimental { get; set; }

    }
}
