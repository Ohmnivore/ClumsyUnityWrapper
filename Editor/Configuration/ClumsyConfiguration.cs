using System;
using UnityEngine;

namespace Clumsy.Editor
{
    [Serializable]
    public class ConfigurationModule
    {
        [Tooltip("Enables the module")]
        public bool Enabled;

        [Tooltip("Enables the module for inbound packets")]
        public bool Inbound = true;

        [Tooltip("Enables the module for outbound packets")]
        public bool Outbound = true;
    }

    [Serializable]
    public class LagModule : ConfigurationModule
    {
        [Range(0f, 15000f)]
        public int DelayMS = 50;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "lag");
            return $"{common} --lag-time {DelayMS}";
        }
    }

    [Serializable]
    public class DropModule : ConfigurationModule
    {
        [Range(0f, 100f)]
        public float ChancePercentage = 10f;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "drop");
            return $"{common} --drop-chance {ChancePercentage}";
        }
    }

    [Serializable]
    public class ThrottleModule : ConfigurationModule
    {
        [Range(0f, 100f)]
        public float ChancePercentage = 10f;

        [Tooltip("Time frame in ms, when a throttle start the packets within the time will be queued and sent altogether when time is over")]
        [Range(0f, 1000f)]
        public int TimeframeMS = 30;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "throttle");
            return $"{common} --throttle-chance {ChancePercentage} --throttle-frame {TimeframeMS}";
        }
    }

    [Serializable]
    public class DuplicateModule : ConfigurationModule
    {
        [Range(0f, 100f)]
        public float ChancePercentage = 10f;

        [Tooltip("How many copies to duplicate")]
        [Range(2f, 50f)]
        public int Count = 2;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "duplicate");
            return $"{common} --duplicate-chance {ChancePercentage} --duplicate-count {Count}";
        }
    }

    [Serializable]
    public class OutOfOrderModule : ConfigurationModule
    {
        [Range(0f, 100f)]
        public float ChancePercentage = 10f;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "ood");
            return $"{common} --ood-chance {ChancePercentage}";
        }
    }

    [Serializable]
    public class TamperModule : ConfigurationModule
    {
        [Range(0f, 100f)]
        public float ChancePercentage = 10f;

        [Tooltip("Recompute checksum after after tampering")]
        public bool RedoChecksum = true;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "tamper");
            return $"{common} --tamper-chance {ChancePercentage} --tamper-checksum {ConsoleArgumentsUtils.BoolToArgument(RedoChecksum)}";
        }
    }

    [Serializable]
    public class TCPResetModule : ConfigurationModule
    {
        [Range(0f, 100f)]
        public float ChancePercentage;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "reset");
            return $"{common} --reset-chance {ChancePercentage}";
        }
    }

    [Serializable]
    public class BandwidthModule : ConfigurationModule
    {
        [Tooltip("Bandwidth in Kilobytes per second")]
        [Range(0f, 99999f)]
        public int LimitKBPerSecond = 10;

        public string ConvertToConsoleArguments()
        {
            var common = ConsoleArgumentsUtils.ConvertToConsoleArguments(this, "bandwidth");
            return $"{common} --bandwidth-bandwidth {LimitKBPerSecond}";
        }
    }

    /// <summary>
    /// Represents the configurable state of the Clumsy executable.
    /// </summary>
    [CreateAssetMenu(menuName = "Clumsy/Configuration", fileName = "ClumsyConfiguration")]
    public class ClumsyConfiguration : ScriptableObject
    {
        [Tooltip("When true, Clumsy will be automatically started when entering Play mode. " +
                 "This can only be enabled when the filter contains no parameters, since they would have to be set at runtime.")]
        public bool AutoStart = true;

        [Tooltip("Filter string. Example: tcp and (tcp.DstPort == 443 or tcp.SrcPort == 443). " +
                 "Can contain parameters that will be replaced by a user-provided value at runtime. " +
                 "Parameters are enclosed in curly brackets, ex {port} or {ip}.")]
        public string Filter;

        public LagModule Lag;
        public DropModule Drop;
        public ThrottleModule Throttle;
        public DuplicateModule Duplicate;
        public OutOfOrderModule OutOfOrder;
        public TamperModule Tamper;
        public TCPResetModule TCPReset;
        public BandwidthModule Bandwidth;

        /// <summary>
        /// Contains the parameters defined in <see cref="Filter"/>.
        /// </summary>
        public ParametersDefinition ParametersDefinition => m_ParametersDefinition;

        [NonSerialized]
        private readonly ParametersDefinition m_ParametersDefinition = new ParametersDefinition();

        /// <summary>
        /// Reference:
        /// https://github.com/jagt/clumsy/wiki/Command-Line-Arguments
        /// and
        /// https://github.com/jagt/clumsy/commit/046572c44d388c80a03dddaf6438d2baa04fcb65
        /// </summary>
        public string ConvertToConsoleArguments(ParametersProcessor parametersProcessor)
        {
            if (!parametersProcessor.IsComplete)
            {
                var missing = parametersProcessor.GetMissingParameters();
                throw new ArgumentException($"Clumsy: Not all parameters were provided for the filter \"{parametersProcessor.Filter}\", the missing parameters are: {string.Join(", ", missing)}");
            }

            var processedFilter = parametersProcessor.GetProcessedFilter();
            return $"--filter \"{processedFilter}\" {Lag.ConvertToConsoleArguments()} {Drop.ConvertToConsoleArguments()} {Throttle.ConvertToConsoleArguments()} {Duplicate.ConvertToConsoleArguments()} {OutOfOrder.ConvertToConsoleArguments()} {Tamper.ConvertToConsoleArguments()} {TCPReset.ConvertToConsoleArguments()} {Bandwidth.ConvertToConsoleArguments()}";
        }

        private void OnValidate()
        {
            m_ParametersDefinition.Parse(Filter);

            if (m_ParametersDefinition.NumEntries > 0)
                AutoStart = false;
        }
    }

    /// <summary>
    /// Formatting utilities for console arguments that are passed to the Clumsy executable.
    /// </summary>
    static class ConsoleArgumentsUtils
    {
        public static string ConvertToConsoleArguments(ConfigurationModule module, string name)
        {
            return $"--{name} {BoolToArgument(module.Enabled)} --{name}-inbound {BoolToArgument(module.Inbound)} --{name}-outbound {BoolToArgument(module.Outbound)}";
        }

        public static string BoolToArgument(bool value)
        {
            return value ? "on" : "off";
        }
    }
}
