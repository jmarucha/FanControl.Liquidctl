using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FanControl.Plugins;

namespace FanControl.Liquidctl;

public class LiquidCtlPlugin : IPlugin2
{
    private readonly List<LiquidctlDevice> _devices = new();

    private readonly List<LiquidctlDeviceJSON>
        _initializedDevices = new(); //Added to filter devices that are not initialized correctly

    private readonly IPluginLogger _logger;

    private readonly IPluginDialog _dialog;

    public LiquidCtlPlugin(IPluginLogger pluginLogger, IPluginDialog dialog)
    {
        _dialog = dialog;
        _logger = pluginLogger;
        _logger.Log("Liquid Control Plugin active!");
    }

    public string Name => "LiquidCtlPlugin";

    public void Initialize()
    {
        try
        {
            ConfigManager.Init();
            LiquidctlCLIWrapper.LiquidCtlExe = ConfigManager.GetConfigValue("app.liquidCtl.execPath");

            _initializedDevices.AddRange(LiquidctlCLIWrapper.Initialize());

            var deviceNames = _initializedDevices
                .Select(json => json.description)
                .Order(StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.StringSort))
                .Select(name => $"=> {name}")
                .Aggregate((i, j) => i + "\n" + j);
            _logger.Log($"Initialized devices:\n{deviceNames}");
        }
        catch (Exception e)
        {
           HandleErrors(e);
        }
    }

    public bool IsInited()
    {
        return _initializedDevices.Count != 0;
    }

    public void Load(IPluginSensorsContainer container)
    {
        try
        {
            var statusDescriptors = LiquidctlCLIWrapper.ReadStatus();
            foreach (var device in from statusDescriptor in statusDescriptors
                     where _initializedDevices.Exists(deviceDesc =>
                         deviceDesc.address.Equals(statusDescriptor.address) &&
                         deviceDesc.description.Equals(statusDescriptor.description))
                     select new LiquidctlDevice(statusDescriptor, _logger))
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
        catch (Exception e)
        {
            HandleErrors(e);
        }
    }

    public void Close()
    {
        _initializedDevices.Clear();
        _devices.Clear();
    }

    public void Update()
    {
        foreach (var device in _devices)
        {
            try
            {
                device.LoadJson();
            }
            catch (Exception e)
            {
                HandleErrors(e);
            }
            
        }
    }

    private void HandleErrors(Exception e)
    {
        var debug = ConfigManager.GetConfigBool("app.debug");
        if (e is ApplicationException || e.InnerException is ApplicationException)
        {
            var message = debug ? $"{e.Message}\n{(e.InnerException?.Message ?? "")}":
                $"{e.Message}\n{e.StackTrace}\n{e.InnerException?.Message ?? ""}";
            _logger.Log($"Error occured at Liquid Control Plugin: {message}");
        }
        else
        {
            _logger.Log(debug
                ? $"Error occured at Liquid Control Plugin: {e.Message}\n{e.StackTrace}"
                : $"Error occured at Liquid Control Plugin: {e.Message}");    
        }
            
        if (!debug) return;
        var prompt = _dialog.ShowMessageDialog($"The Liquid Control plugin may not function correctly!\n{e.Message}");
        try
        {
            prompt.Wait();
        }
        catch (ObjectDisposedException exception)
        {
            _logger.Log($"Error occured at Liquid Control Plugin: {exception.Message}");
        }
        catch (AggregateException exception)
        {
            var messages = exception.InnerExceptions.Select((ex, pos) => $"{pos}. {ex.Message}")
                .Aggregate((i, j) => $"{i}\n{j}");
            _logger.Log($"Errors occured at Liquid Control Plugin:\n {messages}");
        }

    }
}