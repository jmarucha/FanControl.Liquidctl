using System.Collections.Generic;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    public class LiquidctlPlugin : IPlugin2
    {
        internal List<LiquidctlDevice> devices = new List<LiquidctlDevice>();
        internal IPluginLogger logger;

        public string Name => "LiquidctlPlugin";

        public LiquidctlPlugin(IPluginLogger pluginLogger)
        {
            logger = pluginLogger;
        }

        public void Initialize()
        {

            LiquidctlCLIWrapper.Initialize();
        }

        public void Load(IPluginSensorsContainer _container)
        {
            List<LiquidctlStatusJSON> input = LiquidctlCLIWrapper.ReadStatus();
            foreach (LiquidctlStatusJSON liquidctl in input)
            {
                LiquidctlDevice device = new LiquidctlDevice(liquidctl, logger);
                logger.Log(device.GetDeviceInfo());
                if (device.hasSensors)
                {

                    if (device.hasPumpSpeed)
                        _container.FanSensors.Add(device.pumpSpeed);
                    if (device.hasPumpDuty)
                        _container.ControlSensors.Add(device.pumpDuty);
                    if (device.hasLiquidTemperature)
                        _container.TempSensors.Add(device.liquidTemperature);
                    if (device.hasFanSpeed)
                    {
                        _container.FanSensors.Add(device.fanSpeed);
                        _container.ControlSensors.Add(device.fanControl);
                    }
                    devices.Add(device);
                }
            }
        }

        public void Close()
        {
            devices.Clear();
        }
        public void Update()
        {
            foreach (LiquidctlDevice device in devices)
            {
                device.LoadJSON();
            }
        }
    }
}
