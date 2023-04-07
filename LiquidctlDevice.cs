using System;
using System.Collections.Generic;
using System.IO;
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
                _rpmLookup = BuildRpmLookup($"Plugins\\lut\\{_name}.lut", 3000); // The most widespread Asatek Pumps have a ~3000 RPM limit
                UpdateFromJSON(output);
            }

            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                float reading = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
                _value = reading < _rpmLookup.Count ? _rpmLookup[(int)reading] : 100;
            }

            public static readonly string KEY = "Pump speed";
            readonly List<int> _rpmLookup = new List<int>();

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
                _rpmLookup = BuildRpmLookup($"Plugins\\lut\\{_name}.lut");
                UpdateFromJSON(output);
            }

            public void UpdateFromJSON(LiquidctlStatusJSON output)
            {
                float reading = output.status.Single(entry => entry.key == KEY).GetValueAsFloat() ?? 0;
                _value = reading < _rpmLookup.Count ? _rpmLookup[(int)reading] : 100;
            }

            public static readonly string KEY = "Fan speed";
            readonly List<int> _rpmLookup = new List<int>();

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

        private static IPluginLogger logger;
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
            catch (InvalidOperationException e)
            {
                logger.Log($"Device {address} not showing up: {e.Message}");
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

        private static List<int> BuildRpmLookup(string lutFile, int maxRpm = 1400)
        {
            List<int> rpmLookup = new List<int>();
            if (File.Exists(lutFile)) // Read lookup table to estimate fan percentage based on it
            {
                logger.Log($"Using LUT file {lutFile}");
                using (StreamReader sr = File.OpenText(lutFile))
                {
                    string line;
                    int lastStoredRpm = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] values = line.Split(';');
                        int percentage = Convert.ToInt32(values[0]);
                        int specifiedRpm = Convert.ToInt32(values[1]);
                        for (int rpm = lastStoredRpm; rpm < specifiedRpm; rpm++)
                        {
                            rpmLookup.Add(percentage);
                            lastStoredRpm = rpm;
                        }
                    }
                }
            }
            else // Use linear interpolation as a rough estimate
            {
                for (int rpm = 0; rpm <= maxRpm; rpm++)
                {
                    rpmLookup.Add(100 * rpm / maxRpm);
                }
            }
            return rpmLookup;
        }
    }
}
