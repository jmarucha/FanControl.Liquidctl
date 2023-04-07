using System;
using System.Collections.Generic;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    internal class LiquidctlDevice
    {
        public class LiquidTemperature : IPluginSensor
        {
            public LiquidTemperature(LiquidctlStatusJSON output)
            {
                _id = $"{output.address}-liqtmp";
                _name = $"Liquid Temp. - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
            }

            public static readonly string KEY = "Liquid temperature";
            public string Id => _id;
            string _id;

            public string Name => _name;
            string _name;

            public float? Value => _value;
            float _value;

            public void Update()
            { } // plugin updates sensors
        }
        public class PumpSpeed : IPluginSensor
        {
            public PumpSpeed(LiquidctlStatusJSON output)
            {
                _id = $"{output.address}-pumprpm";
                _name = $"Pump - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
            }

            public static readonly string KEY = "Pump speed";
            public string Id => _id;
            readonly string _id;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public void Update()
            { } // plugin updates sensors
        }
        public class PumpDuty : IPluginControlSensor
        {
            public PumpDuty(LiquidctlStatusJSON output)
            {
                _address = output.address;
                _id = $"{_address}-pumpduty";
                _name = $"Pump Control - {output.description}";
                UpdateFromJSON(output);
            }

            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                float reading = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
                //_value = reading > MAX_RPM ? 100.0f : (float)Math.Ceiling(100.0f * reading / MAX_RPM);
                _value = RPM_LOOKUP.OrderBy(e => Math.Abs(e.Key - reading)).FirstOrDefault().Value;
            }

            public static readonly string KEY = "Pump speed";
            static readonly Dictionary<int, int> RPM_LOOKUP = new Dictionary<int, int>
            { // We can only estimate, as it is not provided in any output. Hence I applied this ugly hack
                {1200, 40}, {1206, 41}, {1212, 42}, {1218, 43}, {1224, 44}, {1230, 45}, {1236, 46}, {1242, 47}, {1248, 48}, {1254, 49},
                {1260, 50}, {1313, 51}, {1366, 52}, {1419, 53}, {1472, 54}, {1525, 55}, {1578, 56}, {1631, 57}, {1684, 58}, {1737, 59},
                {1790, 60}, {1841, 61}, {1892, 62}, {1943, 63}, {1994, 64}, {2045, 65}, {2096, 66}, {2147, 67}, {2198, 68}, {2249, 69},
                {2300, 70}, {2330, 71}, {2360, 72}, {2390, 73}, {2420, 74}, {2450, 75}, {2480, 76}, {2510, 77}, {2540, 78}, {2570, 79},
                {2600, 80}, {2618, 81}, {2636, 82}, {2654, 83}, {2672, 84}, {2690, 85}, {2708, 86}, {2726, 87}, {2744, 88}, {2762, 89},
                {2780, 90}, {2789, 91}, {2798, 92}, {2807, 93}, {2816, 94}, {2825, 95}, {2834, 96}, {2843, 97}, {2852, 98}, {2861, 99},
                {MAX_RPM, 100}
            };
            static readonly int MAX_RPM = 2870;

            public string Id => _id;
            string _id;
            string _address;

            public string Name => _name;
            string _name;

            public float? Value => _value;
            float _value;

            public void Reset()
            {
                Set(60.0f);
            }

            public void Set(float val)
            {
                LiquidctlCLIWrapper.SetPump(_address, (int) val);
            }

            public void Update()
            { } // plugin updates sensors

        }

        public class FanSpeed : IPluginSensor
        {
            public FanSpeed(LiquidctlStatusJSON output)
            {
                _id = $"{output.address}-fanrpm";
                _name = $"Fan - {output.description}";
                UpdateFromJSON(output);
            }
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                _value = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
            }

            public static readonly string KEY = "Fan speed";
            public string Id => _id;
            readonly string _id;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public void Update()
            { } // plugin updates sensors
        }
        
        public class FanControl : IPluginControlSensor
        {
            public FanControl(LiquidctlStatusJSON output)
            {
                _address = output.address;
                _id = $"{output.address}-fanctrl";
                _name = $"Fan Control - {output.description}";
                UpdateFromJSON(output);
            }
            // We can only estimate, as it is not provided in any output
            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                float reading = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
                //_value = reading > MAX_RPM ? 100.0f : (float)Math.Ceiling(100.0f * reading / MAX_RPM);
                _value = RPM_LOOKUP.OrderBy(e => Math.Abs(e.Key - reading)).FirstOrDefault().Value;
            }

            public static readonly string KEY = "Fan speed";
            static readonly Dictionary<int, int> RPM_LOOKUP = new Dictionary<int, int>
            { // We can only estimate, as it is not provided in any output. Hence I applied this ugly hack
                {520, 20}, {521, 21}, {522, 22}, {523, 23}, {524, 24}, {525, 25}, {526, 26}, {527, 27}, {528, 28}, {529, 29},
                {530, 30}, {532, 31}, {534, 32}, {536, 33}, {538, 34}, {540, 35}, {542, 36}, {544, 37}, {546, 38}, {548, 39},
                {550, 40}, {571, 41}, {592, 42}, {613, 43}, {634, 44}, {655, 45}, {676, 46}, {697, 47}, {718, 48}, {739, 49},
                {760, 50}, {781, 51}, {802, 52}, {823, 53}, {844, 54}, {865, 55}, {886, 56}, {907, 57}, {928, 58}, {949, 59},
                {970, 60}, {989, 61}, {1008, 62}, {1027, 63}, {1046, 64}, {1065, 65}, {1084, 66}, {1103, 67}, {1122, 68}, {1141, 69},
                {1160, 70}, {1180, 71}, {1200, 72}, {1220, 73}, {1240, 74}, {1260, 75}, {1280, 76}, {1300, 77}, {1320, 78}, {1340, 79},
                {1360, 80}, {1377, 81}, {1394, 82}, {1411, 83}, {1428, 84}, {1445, 85}, {1462, 86}, {1479, 87}, {1496, 88}, {1513, 89},
                {1530, 90}, {1550, 91}, {1570, 92}, {1590, 93}, {1610, 94}, {1630, 95}, {1650, 96}, {1670, 97}, {1690, 98}, {1720, 99},
                {MAX_RPM, 100}
            };
            static readonly int MAX_RPM = 1980;

            public string Id => _id;
            readonly string _id;
            string _address;

            public string Name => _name;
            readonly string _name;

            public float? Value => _value;
            float _value;

            public void Reset()
            {
                Set(50.0f);
            }

            public void Set(float val)
            {
                LiquidctlCLIWrapper.SetFan(_address, (int) val);
            }

            public void Update()
            { } // plugin updates sensors
        }

        public LiquidctlDevice(LiquidctlStatusJSON output, IPluginLogger pluginLogger)
        {
            logger = pluginLogger;
            address = output.address;

            hasPumpSpeed = output.status.Exists(entry => entry.key == PumpSpeed.KEY && !(entry.value is null));
            if (hasPumpSpeed)
                pumpSpeed = new PumpSpeed(output);

            hasPumpDuty = output.status.Exists(entry => entry.key == PumpDuty.KEY && !(entry.value is null));
            if (hasPumpDuty)
                pumpDuty = new PumpDuty(output);

            hasFanSpeed = output.status.Exists(entry => entry.key == FanSpeed.KEY && !(entry.value is null));
            if(hasFanSpeed)
            {
                fanSpeed = new FanSpeed(output);
                fanControl = new FanControl(output);
            }

            hasLiquidTemperature = output.status.Exists(entry => entry.key == LiquidTemperature.KEY && !(entry.value is null));
            if (hasLiquidTemperature)
                liquidTemperature = new LiquidTemperature(output);

            hasSensors = hasPumpSpeed || hasPumpDuty || hasLiquidTemperature || hasFanSpeed;
        }

        public readonly bool hasPumpSpeed, hasPumpDuty, hasLiquidTemperature, hasFanSpeed, hasSensors;

        public void UpdateFromJSON(LiquidctlStatusJSON output)
        {
            if (hasLiquidTemperature)
                liquidTemperature.UpdateFromJSON(output);
            if (hasPumpSpeed)
                pumpSpeed.UpdateFromJSON(output);
            if (hasPumpDuty)
                pumpDuty.UpdateFromJSON(output);
            if (hasFanSpeed)
            {
                fanSpeed.UpdateFromJSON(output);
                fanControl.UpdateFromJSON(output);
            }
        }

        private readonly IPluginLogger logger;
        public string address;
        public LiquidTemperature liquidTemperature;
        public PumpSpeed pumpSpeed;
        public PumpDuty pumpDuty;
        public FanSpeed fanSpeed;
        public FanControl fanControl;

        public void LoadJSON()
        {
            try
            {
                LiquidctlStatusJSON output = LiquidctlCLIWrapper.ReadStatus(address).First();
                UpdateFromJSON(output);
            }
            catch (InvalidOperationException)
            {
                throw new Exception($"Device {address} not showing up");
            }
        }

        public String GetDeviceInfo() {
            String ret = $"Device @ {address}";
            if (hasLiquidTemperature) ret += $", Liquid @ {liquidTemperature.Value}";
            if (hasPumpSpeed) ret += $", Pump @ {pumpSpeed.Value}";
            if (hasPumpDuty) ret += $"({pumpDuty.Value})";
            if (hasFanSpeed) ret += $", Fan @ {fanSpeed.Value} ({fanControl.Value})";
            return ret;
        }
    }
}
