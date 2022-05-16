using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace FanControl.Liquidctl
{
    internal static class LiquidctlCLIWrapper
    {
        public static string liquidctlexe = "Plugins\\liquidctl.exe"; //TODO extract path to executable to config
        internal static void Initialize()
        {
            LiquidctlCall($"--json initialize all");
        }
        internal static List<LiquidctlStatusJSON> ReadStatus()
        {
            Process process = LiquidctlCall($"--json status");
            return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
        }
        internal static List<LiquidctlStatusJSON> ReadStatus(string address)
        {
            Process process = LiquidctlCall($"--json --address {address} status");
            return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
        }
        internal static void SetPump(string address, int value)
        {
            LiquidctlCall($"--address {address} set pump speed {(value)}");
        }

        private static Process LiquidctlCall(string arguments)
        {
            Process process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.FileName = liquidctlexe;
            process.StartInfo.Arguments = arguments;

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"liquidctl returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");
            }

            return process;
        }
    }
}
