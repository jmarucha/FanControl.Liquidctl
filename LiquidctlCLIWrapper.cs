using System;
using System.Collections.Generic;
using System.Diagnostics;
using FanControl.Plugins;
using Newtonsoft.Json.Linq;

namespace FanControl.Liquidctl
{
    internal class LiquidctlCLIWrapper
    {
        internal static IPluginLogger logger;
        internal static Config config;

        internal static void Initialize(Config pluginConfig, IPluginLogger pluginLogger)
        {
            config = pluginConfig;
            logger = pluginLogger;

            LiquidctlCall($"--json initialize all");

            foreach(var match in config.liquidctl.match)
            {
                foreach(var cmd in match.set)
                {
                    LiquidctlCall($"--match {match.device} set {cmd}");
                }
            }
        }
        internal static List<LiquidctlStatusJSON> ReadStatus()
        {
            Process process = LiquidctlCall($"--json status");
            return ParseStatuses(process.StandardOutput.ReadToEnd());
        }
        internal static List<LiquidctlStatusJSON> ReadStatus(string address)
        {
            Process process = LiquidctlCall($"--json --address {address} status");
            return ParseStatuses(process.StandardOutput.ReadToEnd());
        }
        internal static void SetPump(string address, int value)
        {
            LiquidctlCall($"--address {address} set pump speed {(value)}");
        }

        internal static void SetFan(string address, int value)
        {
            LiquidctlCall($"--address {address} set fan speed {(value)}");
        }

        private static Process LiquidctlCall(string arguments)
        {
            Process process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.FileName = config.liquidctl.exePath;
            process.StartInfo.Arguments = arguments;

#if DEBUG
            logger.Log($"Calling: {config.liquidctl.exePath} {arguments}");
#endif
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"liquidctl returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");
            }

            return process;
        }

        private static List<LiquidctlStatusJSON> ParseStatuses(string json)
        {
#if DEBUG
            logger.Log($"Got: {json}");
#endif
            JArray statusArray = JArray.Parse(json);
            List<LiquidctlStatusJSON> statuses = new List<LiquidctlStatusJSON>();

            foreach (JObject statusObject in statusArray)
            {
                try
                {
                    LiquidctlStatusJSON status = statusObject.ToObject<LiquidctlStatusJSON>();
                    statuses.Add(status);
                }
                catch(Exception e)
                {
                    logger.Log($"Unable to parse {statusObject}\ne.Message");
                }
            }

            return statuses;
        }
    }
}
