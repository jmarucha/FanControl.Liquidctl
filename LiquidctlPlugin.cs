using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl;

public class LiquidctlPlugin : IPlugin2
{
    private readonly List<LiquidctlDevice> _devices = new();

    private readonly List<LiquidctlDeviceJSON>
        _initializedDevices = new(); //Added to filter devices that are not initialized correctly

    private readonly IPluginLogger _logger;

    public LiquidctlPlugin(IPluginLogger pluginLogger)
    {
        _logger = pluginLogger;
        _logger.Log("Liquid Ctl Plugin active!");
    }

    public string Name => "LiquidctlPlugin";

    public void Initialize()
    {
        _initializedDevices.AddRange(LiquidctlCLIWrapper.Initialize());

        var deviceNames = _initializedDevices
            .Select(json => json.description)
            .Order(StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.StringSort))
            .Select(name => $"=> {name}")
            .Aggregate((i, j) => i + "\n" + j);
        _logger.Log($"Initialized devices:\n{deviceNames}");
    }

    public bool IsInited()
    {
        return _initializedDevices.Count != 0;
    }

    public void Load(IPluginSensorsContainer container)
    {
        var statusDescriptors = LiquidctlCLIWrapper.ReadStatus();
        foreach (var device in from statusDescriptor in statusDescriptors where _initializedDevices.Exists(deviceDesc =>
                     deviceDesc.address.Equals(statusDescriptor.address) &&
                     deviceDesc.description.Equals(statusDescriptor.description)) select new LiquidctlDevice(statusDescriptor, _logger))
        {
            _logger.Log(device.GetDeviceInfo());
            if (device.HasPumpSpeed)
                container.FanSensors.Add(device.pumpSpeed);
            if (device.HasPumpDuty)
                container.ControlSensors.Add(device.pumpDuty);
            if (device.HasLiquidTemperature)
                container.TempSensors.Add(device.liquidTemperature);
            if (device.HasFanSpeed)
            {
                
                container.FanSensors.AddRange(device.FanSpeedSensors);
                container.ControlSensors.AddRange(device.FanControlSensors);
            }

            _devices.Add(device);
        }
    }

    public void Close()
    {
        _devices.Clear();
    }

    public void Update()
    {
        foreach (var device in _devices) device.LoadJson();
    }
}