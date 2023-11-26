using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;

namespace FanControl.Liquidctl;

internal record FailedDeviceInfo(LiquidctlDeviceJSON Device, string FailureMessage);

internal static class LiquidctlCLIWrapper
{
    public static string LiquidCtlExe { get; internal set; }

    public static List<FailedDeviceInfo> FailedToInitDevices { get; private set; }

    internal static IEnumerable<LiquidctlDeviceJSON> Initialize()
    {
        var deviceDescriptors = ScanDevices();
        var nonInitializedDevices = new List<LiquidctlDeviceJSON>();
        FailedToInitDevices = new List<FailedDeviceInfo>();

        foreach (var deviceDesc in deviceDescriptors)
            try
            {
                CallLiquidControl(deviceDesc.serial_number is not ""
                    ? $"--json --serial {deviceDesc.serial_number} initialize"
                    : $"--json -m \"{deviceDesc.description}\" initialize");
            }
            catch (Exception e)
            {
                // Ignore exceptions during initialize and add to  non initialized devices.
                nonInitializedDevices.Add(deviceDesc);
                var failedDeviceInfo = new FailedDeviceInfo(deviceDesc, e.Message);
                FailedToInitDevices.Add(failedDeviceInfo);
            }

        // Filter out non initialized devices
        deviceDescriptors.RemoveAll(deviceDesc => nonInitializedDevices.Contains(deviceDesc));
        return deviceDescriptors;
    }

    private static List<LiquidctlDeviceJSON> ScanDevices()
    {
        var process = CallLiquidControl("--json list");
        return JsonConvert.DeserializeObject<List<LiquidctlDeviceJSON>>(process.StandardOutput.ReadToEnd());
    }

    internal static LiquidctlStatusJSON ReadStatus(Option option, string value)
    {
        var valueStr = option.IsNumeric() ? $"{value}" : $"\"{value}\"";
        var process = CallLiquidControl($"--json {option.GetSwitch()} {valueStr} status");
        var status = JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
        return status?.Count > 0 ? status[0] : null;
    }

    internal static IEnumerable<LiquidctlStatusJSON> ReadStatus()
    {
        var process = CallLiquidControl("--json status");
        return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
    }

    internal static IEnumerable<LiquidctlStatusJSON> ReadStatus(string address)
    {
        var process = CallLiquidControl($"--json --address {address} status");
        return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
    }

    internal static void SetPump(string address, int value)
    {
        CallLiquidControl($"--address {address} set pump speed {value}");
    }

    internal static void SetFan(string address, int channel, int value)
    {
        CallLiquidControl($"--address {address} set fan{channel} speed {value}");
    }

    internal static void SetFan(string address, int value)
    {
        CallLiquidControl($"--address {address} set fan speed {value}");
    }

    private static Process CallLiquidControl(string arguments)
    {
        var process = new Process();

        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.StartInfo.FileName = LiquidCtlExe;
        process.StartInfo.Arguments = arguments;

        try
        {
            process.Start();
        }
        catch (Win32Exception e)
        {
            throw new ApplicationException($"Failed to locate LiquidCtl executable: @ {LiquidCtlExe}", e);
        }
        catch (InvalidOperationException e)
        {
            throw new ApplicationException("LiquidCtl executable path not set, Please check your configuration!", e);
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(
                $"LiquidCtl cli returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");

        return process;
    }
}