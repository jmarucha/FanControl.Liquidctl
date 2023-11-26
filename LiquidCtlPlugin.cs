using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FanControl.Plugins;

namespace FanControl.Liquidctl;

public class LiquidCtlPlugin : IPlugin2
{
    private readonly List<LiquidctlDevice> _devices = new();

    private readonly IPluginDialog _dialog;

    private readonly List<LiquidctlDeviceJSON>
        _initializedDeviceDescriptors = new(); //Contains only initialized devices correctly

    private readonly IPluginLogger _logger;

    public LiquidCtlPlugin(IPluginLogger pluginLogger, IPluginDialog dialog)
    {
        _dialog = dialog;
        _logger = pluginLogger;
        _logger.Log("Liquid Control Plugin active!");
        ConfigManager.FileLoadExceptionHandler = context =>
        {
            _logger.Log($"Unrecoverable Error while loading config file: {context.Exception.Message}");
            var task = _dialog.ShowMessageDialog(
                $"Unable loading config file $ {context.Exception.Message}. Please ensure that the file is in the location!");
            if (!task.IsCompleted) task.Wait();
        };
    }

    public string Name => "LiquidCtlPlugin";

    public void Initialize()
    {
        try
        {
            ConfigManager.Init();
            LiquidctlCLIWrapper.LiquidCtlExe = ConfigManager.GetConfigValue("app.liquidCtl.execPath");

            _initializedDeviceDescriptors.AddRange(LiquidctlCLIWrapper.Initialize());

            if (LiquidctlCLIWrapper.FailedToInitDevices.Count != 0)
                _logger.Log(
                    $"Failed to initialize devices: \n{string.Join('\n', LiquidctlCLIWrapper.FailedToInitDevices.Select((info, i) => $"{i + 1}. {info.Device.description} \nCause: {info.FailureMessage}"))}");

            if (_initializedDeviceDescriptors.Count == 0)
            {
                _logger.Log("No initialized device discovered!");
                return;
            }

            var deviceNames = _initializedDeviceDescriptors
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


    public void Load(IPluginSensorsContainer container)
    {
        try
        {
            foreach (var device in _initializedDeviceDescriptors
                         .Select(desc =>
                             desc.serial_number is not null
                                 ? LiquidctlCLIWrapper.ReadStatus(Option.SerialId, desc.serial_number)
                                 : desc.address is not null
                                     ? LiquidctlCLIWrapper.ReadStatus(Option.Address, desc.address)
                                     : LiquidctlCLIWrapper.ReadStatus(Option.Description, desc.description))
                         .Select(json => new LiquidctlDevice(json, _logger)))
            {
                _logger.Log(device.GetDeviceInfo());
                if (device.HasPumpSpeed) container.FanSensors.Add(device.PumpSpeedSensor);
                if (device.HasPumpDuty) container.ControlSensors.Add(device.PumpDutyController);
                if (device.HasLiquidTemperature) container.TempSensors.Add(device.LiquidTemperatureSensor);
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
        _initializedDeviceDescriptors.Clear();
        _devices.Clear();
    }

    public void Update()
    {
        foreach (var device in _devices)
            try
            {
                device.LoadJson();
            }
            catch (Exception e)
            {
                HandleErrors(e);
            }
    }

    /// <summary>
    ///     Checks if there are correctly initialized devices that the plugin can get data from.
    /// </summary>
    /// <returns>True  if there are any</returns>
    public bool HasInitialized()
    {
        return _initializedDeviceDescriptors.Count != 0;
    }

    /// <summary>
    ///     Count the number of devices enumerated and found properly intialized
    /// </summary>
    /// <returns>The count</returns>
    public int EnumeratedDeviceCount()
    {
        return _initializedDeviceDescriptors.Count;
    }

    private void HandleErrors(Exception e)
    {
        var debug = ConfigManager.GetConfigBool("app.debug");
        if (e is ApplicationException || e.InnerException is ApplicationException)
        {
            var message = debug
                ? $"{e.Message}\n{e.InnerException?.Message ?? ""}"
                : $"{e.Message}\n{e.StackTrace}\n{e.InnerException?.Message ?? ""}";
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
            if (!prompt.IsCompleted) prompt.Wait();
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