using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    internal class LiquidctlDevice
    {
        public readonly bool HasPumpSpeed, HasPumpDuty, HasLiquidTemperature, HasFanSpeed;
        private readonly string _address;
        public readonly List<FanControl> FanControlSensors = new();
        public readonly List<FanSpeed> FanSpeedSensors = new();
        public readonly LiquidTemperature liquidTemperature;

        private readonly IPluginLogger _logger;
        public readonly PumpDuty pumpDuty;
        public readonly PumpSpeed pumpSpeed;

        public LiquidctlDevice(LiquidctlStatusJSON output, IPluginLogger pluginLogger)
        {
            _logger = pluginLogger;
            _address = output.address;

            HasPumpSpeed = output.status.Exists(entry => entry.key == PumpSpeed.Key && entry.value is not null);
            if (HasPumpSpeed)
                pumpSpeed = new PumpSpeed(output);


            HasPumpDuty = output.status.Exists(entry => entry.key == PumpDuty.Key && entry.value is not null);
            if (HasPumpDuty)
                pumpDuty = new PumpDuty(output);

            HasFanSpeed = output.status.Exists(entry =>
                entry.key.Contains("Fan") && entry.key.Contains("speed", StringComparison.CurrentCultureIgnoreCase) &&
                entry.value is not null);

            if (HasFanSpeed)
            {
                var fanSpeed = output.status.Where(entry =>
                    entry.key.Contains("Fan", StringComparison.CurrentCultureIgnoreCase) &&
                    entry.key.Contains("speed", StringComparison.CurrentCultureIgnoreCase) &&
                    entry.value is not null);
                foreach (var speed in fanSpeed)
                {
                    var channel = ParseChannel(speed.key, "Fan");
                    var fan = new FanSpeed(output, channel);
                    this.FanSpeedSensors.Add(fan);
                    this.FanControlSensors.Add(new FanControl(output, channel));
                }
            }

            HasLiquidTemperature =
                output.status.Exists(entry => entry.key == LiquidTemperature.Key && entry.value is not null);
            if (HasLiquidTemperature)
                liquidTemperature = new LiquidTemperature(output);
        }

        private int ParseChannel(string key, string type)
        {
            var result = -1;
            var val = key.Split(" ")
                .Single(s => int.TryParse(s, NumberStyles.Integer, new NumberFormatInfo(), out result));
            if (val.Length != 0 && result != -1)
                _logger.Log($"{key} does not contain any index identifier. Setting for only one {type} channel");
            return result;
        }

        private void UpdateFromStatusDescriptor(LiquidctlStatusJSON output)
        {
            if (HasLiquidTemperature) liquidTemperature.LoadFromStatusDescriptor(output);
            if (HasPumpSpeed) pumpSpeed.LoadFromStatusDescriptor(output);
            if (HasPumpDuty) pumpDuty.LoadFromStatusDescriptor(output);
            if (!HasFanSpeed) return;
            foreach (var speed in FanSpeedSensors) speed.LoadFromStatusDescriptor(output);
            foreach (var control in FanControlSensors) control.LoadFromStatusDescriptor(output);
        }


        public void LoadJson()
        {
            try
            {
                LiquidctlStatusJSON output = LiquidctlCLIWrapper.ReadStatus(_address).First();
                UpdateFromStatusDescriptor(output);
            }
            catch (InvalidOperationException)
            {
                throw new Exception($"Device {_address} not showing up");
            }
        }

        private string GetFanStatuses()
        {
            return FanSpeedSensors.Select((speed, idx) =>
                    $"{{ {speed.Name} : {speed.Value}{speed.Unit}, Duty: {FanControlSensors[idx].Value}{FanControlSensors[idx].Unit} }}")
                .Aggregate((i, j) => $"{i},\n{j}");
        }

        public string GetDeviceInfo()
        {
            var ret = $"Device @ {_address}";
            if (HasLiquidTemperature) ret += $", Liquid @ {liquidTemperature.Value}";
            if (HasPumpSpeed) ret += $", Pump @ {pumpSpeed.Value}";
            if (HasPumpDuty) ret += $"({pumpDuty.Value})";
            if (HasFanSpeed) ret += $", Fans @ {GetFanStatuses()}";
            return ret;
        }

        public class LiquidTemperature : IPluginSensor
        {
            internal const string Key = "Liquid temperature";
            private float _value;

            public LiquidTemperature(LiquidctlStatusJSON output)
            {
                Id = $"{output.address}-liqtmp";
                Name = $"Liquid Temp. - {output.description}";
                LoadFromStatusDescriptor(output);
            }

            public string Id { get; }

            public string Name { get; }

            public float? Value => _value;

            internal void LoadFromStatusDescriptor(LiquidctlStatusJSON output)
            {
                var value = output.status.Single(entry => entry.key == Key).value;
                if (value != null)
                    _value = (float)value;
            }

            public void Update()
            {
            } // plugin updates sensors
        }

        public class PumpSpeed : IPluginSensor
        {
            internal const string Key = "Pump speed";
            private float _value;

            public PumpSpeed(LiquidctlStatusJSON output)
            {
                Id = $"{output.address}-pumprpm";
                Name = $"Pump - {output.description}";
                LoadFromStatusDescriptor(output);
            }

            public string Id { get; }

            public string Name { get; }

            public float? Value => _value;

            internal void LoadFromStatusDescriptor(LiquidctlStatusJSON output)
            {
                var value = output.status.Single(entry => entry.key == Key).value;
                if (value != null)
                    _value = (float)value;
            }

            public void Update()
            {
            } // plugin updates sensors
        }

        public class PumpDuty : IPluginControlSensor
        {
            internal const string Key = "Pump speed";

            private static readonly Dictionary<int, int> RpmLookup = new()
            {
                // We can only estimate, as it is not provided in any output. Hence I applied this ugly hack
                { 1200, 40 }, { 1206, 41 }, { 1212, 42 }, { 1218, 43 }, { 1224, 44 }, { 1230, 45 }, { 1236, 46 },
                { 1242, 47 },
                { 1248, 48 }, { 1254, 49 },
                { 1260, 50 }, { 1313, 51 }, { 1366, 52 }, { 1419, 53 }, { 1472, 54 }, { 1525, 55 }, { 1578, 56 },
                { 1631, 57 },
                { 1684, 58 }, { 1737, 59 },
                { 1790, 60 }, { 1841, 61 }, { 1892, 62 }, { 1943, 63 }, { 1994, 64 }, { 2045, 65 }, { 2096, 66 },
                { 2147, 67 },
                { 2198, 68 }, { 2249, 69 },
                { 2300, 70 }, { 2330, 71 }, { 2360, 72 }, { 2390, 73 }, { 2420, 74 }, { 2450, 75 }, { 2480, 76 },
                { 2510, 77 },
                { 2540, 78 }, { 2570, 79 },
                { 2600, 80 }, { 2618, 81 }, { 2636, 82 }, { 2654, 83 }, { 2672, 84 }, { 2690, 85 }, { 2708, 86 },
                { 2726, 87 },
                { 2744, 88 }, { 2762, 89 },
                { 2780, 90 }, { 2789, 91 }, { 2798, 92 }, { 2807, 93 }, { 2816, 94 }, { 2825, 95 }, { 2834, 96 },
                { 2843, 97 },
                { 2852, 98 }, { 2861, 99 },
                { MaxRpm, 100 }
            };

            private const int MaxRpm = 2870;
            private readonly string _address;
            private float _value;

            public PumpDuty(LiquidctlStatusJSON output)
            {
                _address = output.address;
                Id = $"{_address}-pumpduty";
                Name = $"Pump Control - {output.description}";
                LoadFromStatusDescriptor(output);
            }

            public string Id { get; }

            public string Name { get; }

            public float? Value => _value;

            internal void LoadFromStatusDescriptor(LiquidctlStatusJSON output)
            {
                var value = output.status.Single(entry => entry.key == Key).value;
                if (value == null) return;
                var reading = (float)value;
                //_value = reading > MAX_RPM ? 100.0f : (float)Math.Ceiling(100.0f * reading / MAX_RPM);
                _value = RpmLookup.OrderBy(e => Math.Abs(e.Key - reading)).FirstOrDefault().Value;
            }

            public void Reset()
            {
                Set(60.0f);
            }

            public void Set(float val)
            {
                LiquidctlCLIWrapper.SetPump(_address, (int)val);
            }

            public void Update()
            {
            } // plugin updates sensors
        }

        public class FanSpeed : IPluginSensor
        {
            private readonly string _key;

            private float _value;

            public FanSpeed(LiquidctlStatusJSON output, int channel)
            {
                _key = channel != -1 ? $"Fan {channel} speed" : "Fan speed";
                Id = $"{output.address}-fanRPM{channel}";
                Name = $"Fan {channel}- {output.description}";
                LoadFromStatusDescriptor(output);
            }

            public string Id { get; }

            public string Name { get; }

            public string Unit { get; private set; }

            public float? Value => _value;

            internal void LoadFromStatusDescriptor(LiquidctlStatusJSON output)
            {
                var statusRecord = output.status.Single(entry => entry.key == _key);
                var value = statusRecord.value;
                var unit = statusRecord.unit;
                if (unit.Length != 0)
                    Unit = unit;
                if (value != null)
                    _value = (float)value;
            }

            public void Update()
            {
            } // plugin updates sensors
        }

        public class FanControl : IPluginControlSensor
        {
            private readonly string _key;

            // private static readonly Dictionary<int, int> RpmLookup = new()
            // {
            //     // We can only estimate, as it is not provided in any output. Hence I applied this ugly hack
            //     { 520, 20 }, { 521, 21 }, { 522, 22 }, { 523, 23 }, { 524, 24 }, { 525, 25 }, { 526, 26 }, { 527, 27 },
            //     { 528, 28 }, { 529, 29 },
            //     { 530, 30 }, { 532, 31 }, { 534, 32 }, { 536, 33 }, { 538, 34 }, { 540, 35 }, { 542, 36 }, { 544, 37 },
            //     { 546, 38 }, { 548, 39 },
            //     { 550, 40 }, { 571, 41 }, { 592, 42 }, { 613, 43 }, { 634, 44 }, { 655, 45 }, { 676, 46 }, { 697, 47 },
            //     { 718, 48 }, { 739, 49 },
            //     { 760, 50 }, { 781, 51 }, { 802, 52 }, { 823, 53 }, { 844, 54 }, { 865, 55 }, { 886, 56 }, { 907, 57 },
            //     { 928, 58 }, { 949, 59 },
            //     { 970, 60 }, { 989, 61 }, { 1008, 62 }, { 1027, 63 }, { 1046, 64 }, { 1065, 65 }, { 1084, 66 },
            //     { 1103, 67 },
            //     { 1122, 68 }, { 1141, 69 },
            //     { 1160, 70 }, { 1180, 71 }, { 1200, 72 }, { 1220, 73 }, { 1240, 74 }, { 1260, 75 }, { 1280, 76 },
            //     { 1300, 77 },
            //     { 1320, 78 }, { 1340, 79 },
            //     { 1360, 80 }, { 1377, 81 }, { 1394, 82 }, { 1411, 83 }, { 1428, 84 }, { 1445, 85 }, { 1462, 86 },
            //     { 1479, 87 },
            //     { 1496, 88 }, { 1513, 89 },
            //     { 1530, 90 }, { 1550, 91 }, { 1570, 92 }, { 1590, 93 }, { 1610, 94 }, { 1630, 95 }, { 1650, 96 },
            //     { 1670, 97 },
            //     { 1690, 98 }, { 1720, 99 },
            //     { MaxRpm, 100 }
            // };

            // private const int MaxRpm = 2000;
            private readonly string _address;
            private float _value;
            private readonly int _channel;

            public FanControl(LiquidctlStatusJSON output, int channel)
            {
                _channel = channel;
                _value = 50.0f; // default value for fan control is 50% duty cycle. This is the same as the default value for the fan speed. 
                Unit = "%"; // default unit for fan control is % duty cycle. This is the same as the default unit for the fan speed. 
                _key = _channel != -1 ? $"Fan {channel} duty" : "Fan duty";
                _address = output.address;

                Id = $"{output.address}-fanCtrl{channel}";
                Name = $"Fan {channel} Control - {output.description}";
                LoadFromStatusDescriptor(output);
            }

            public string Id { get; }

            public string Name { get; }

            public string Unit { get; private set; }

            public float? Value => _value;

            internal void LoadFromStatusDescriptor(LiquidctlStatusJSON output)
            {
                var statusRecord = output.status.Single(entry => entry.key == _key);

                var value = statusRecord.value;
                var unit = statusRecord.unit;
                if (unit.Length != 0)
                    Unit = unit;
                if (value == null) return;
                _value = (float)value;
                //_value = reading > MAX_RPM ? 100.0f : (float)Math.Ceiling(100.0f * reading / MAX_RPM);
                // _value = RpmLookup.OrderBy(e => Math.Abs(e.Key - reading)).FirstOrDefault().Value;
            }

            public void Reset()
            {
                Set(50.0f);
            }

            public void Set(float val)
            {
                if (_channel != -1)
                {
                    LiquidctlCLIWrapper.SetFan(_address, _channel, (int)val);
                }
                else
                {
                    LiquidctlCLIWrapper.SetFan(_address, (int)val);
                }
            }


            public void Update()
            {
            } // plugin updates sensors
        }
    }
}