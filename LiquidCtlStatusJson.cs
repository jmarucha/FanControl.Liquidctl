#nullable enable
using System.Collections.Generic;

namespace FanControl.Liquidctl;

public class LiquidCtlStatusJson
{
    public LiquidCtlStatusJson()
    {
        this.bus = "";
        this.status = new List<StatusRecord>();
    }

    public string bus { get; set; }
    public required string address { get; set; }

    public required string description { get; set; }

    public List<StatusRecord> status { get; set; }

    public class StatusRecord
    {
        public required string key { get; set; }
        public string? value { get; set; }
        public string? unit { get; set; }
    }
}