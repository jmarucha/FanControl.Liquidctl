using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Newtonsoft.Json;

namespace FanControl.Liquidctl;

internal static class LiquidctlCLIWrapper
{
    private const string LiquidCtlExe = "Plugins\\liquidctl.exe"; //TODO extract path to executable to config

    internal static IEnumerable<LiquidctlDeviceJSON> Initialize()
    {
        var deviceDescriptors = ScanDevices();
        var nonInitializedDevices = new List<LiquidctlDeviceJSON>();
        
        foreach (var deviceDesc in deviceDescriptors)
        {
            try
            {
                if (deviceDesc.serial_number is not "")
                {
                    CallLiquidControl($"--serial {deviceDesc.serial_number} initialize" );    
                }
                else
                {
                    CallLiquidControl($"-m {deviceDesc.description} initialize" );
                }
                
            }
            catch (Exception e)
            {
                // Ignore exceptions during initialize and add  non initialized devices.
                nonInitializedDevices.Add(deviceDesc);
            }
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
        catch (Exception e)
        {
          if(e is Win32Exception )
          {
              throw new ApplicationException($"Failed to locate LiquidCtl executable: @ {LiquidCtlExe}", e);
          }   
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(
                $"LiquidCtl cli returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");

        return process;
    }
}