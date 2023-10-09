using System.Collections.Generic;
using System.IO;
using FanControl.Plugins;
using Newtonsoft.Json;

namespace FanControl.Liquidctl
{
    public class LiquidctlPlugin : IPlugin2
    {
        internal List<LiquidctlDevice> devices = new List<LiquidctlDevice>();
        internal IPluginLogger logger;
        internal Config config;

        public string Name => "LiquidctlPlugin";

        public LiquidctlPlugin(IPluginLogger pluginLogger)
        {
            logger = pluginLogger;
        }

        public void Initialize()
        {
            using (StreamReader tr = new StreamReader(@"Plugins\FanControl.Liquidctl.json"))
            {
                config = JsonConvert.DeserializeObject<Config>(tr.ReadToEnd());
            }

            LiquidctlCLIWrapper.Initialize(config, logger);
        }

        public void Load(IPluginSensorsContainer _container)
        {
            logger.Log($"Loading: {Name}");

            List<LiquidctlStatusJSON> statusList = LiquidctlCLIWrapper.ReadStatus();
            foreach (LiquidctlStatusJSON status in statusList)
            {
                LiquidctlDevice device = new LiquidctlDevice(status, logger);
                logger.Log(device.GetDeviceInfo());
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
