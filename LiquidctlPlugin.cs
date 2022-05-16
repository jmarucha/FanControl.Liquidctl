using System.Collections.Generic;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl
{
    public class LiquidctlPlugin : IPlugin2
    {
        internal List<LiquidctlDevice> devices = new List<LiquidctlDevice>();

        public string Name => "LiquidctlPlugin";

        public void Initialize()
        {

            LiquidctlCLIWrapper.Initialize();
        }

        public void Load(IPluginSensorsContainer _container)
        {
            List<LiquidctlStatusJSON> input = LiquidctlCLIWrapper.ReadStatus();
            foreach (LiquidctlStatusJSON liquidctl in input)
            {
                LiquidctlDevice device = new LiquidctlDevice(liquidctl);
                if (device.hasPumpSpeed)
                    _container.FanSensors.Add(device.pumpSpeed);
                if (device.hasPumpDuty)
                    _container.ControlSensors.Add(device.pumpDuty);
                if (device.hasLiquidTemperature)
                    _container.TempSensors.Add(device.liquidTemperature);
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
