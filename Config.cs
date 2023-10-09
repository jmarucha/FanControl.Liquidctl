using System.Collections.Generic;

namespace FanControl.Liquidctl
{
    public class Config
    {
        public class LiquidctlRecord
        {
            public class MatchRecord
            {
                public string device { get; set; }
                public List<string> set { get; set; }
            }

            public string exePath { get; set; }
            public List<MatchRecord> match { get; set; }
        }

        public LiquidctlRecord liquidctl { get; set; }
    }
}
