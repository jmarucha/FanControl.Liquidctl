using System.Collections.Generic;
namespace FanControl.Liquidctl
{
    public class LiquidctlStatusJSON
    {
        public class StatusRecord
        {
            public string key { get; set; }
            public float? value { get; set; }
            public string unit { get; set; }
        }
        public string bus { get; set; }
        public string address { get; set; }

        public string description { get; set; }

        public List<StatusRecord> status { get; set; }
    }
}
