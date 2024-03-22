using System;
using System.Diagnostics;

namespace Clumsy.Editor
{
    /// <summary>
    /// Launches or stops the Clumsy executable.
    /// </summary>
    public class Runner
    {
        private static Runner s_Instance;

        public static Runner Instance => s_Instance ??= new Runner();

        /// <summary>
        /// True when the external Clumsy process/window is running.
        /// </summary>
        public bool IsRunning => m_Process is { HasExited: false };

        /// <summary>
        /// True when the user or the system has closed the external Clumsy process/window.
        /// Also true if Clumsy closed itself (ex because of invalid arguments).
        /// </summary>
        public bool WasClosedExternally => m_Process != null && !IsRunning;

        private Process m_Process;

        private ParametersProcessor m_Parameters = new ParametersProcessor();

        /// <summary>
        /// The configuration that was last set by <see cref="SetConfiguration"/>.
        /// </summary>
        public ClumsyConfiguration Configuration => m_Configuration;

        private ClumsyConfiguration m_Configuration;

        /// <summary>
        /// Sets the configuration that Clumsy should use when it starts.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when Clumsy is already running</exception>
        public void SetConfiguration(ClumsyConfiguration configuration)
        {
            if (IsRunning)
                throw new InvalidOperationException("Clumsy: The configuration cannot be changed while the external Clumsy process/window is running");

            m_Configuration = configuration;

#if CLUMSY_LOG
            UnityEngine.Debug.Log($"Clumsy: Using configuration {configuration.name}");
#endif

            var prevParameters = m_Parameters.GetParameters();
            m_Parameters.Initialize(m_Configuration.Filter, m_Configuration.ParametersDefinition);
            m_Parameters.TransferParameters(prevParameters);
        }

        /// <summary>
        /// Starts the external Clumsy process/window with the last set configuration and parameters.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;

            if (!ProjectSettings.instance.IsEnabled)
                return;

            m_Process = new Process();
            
            var windowStyle = UserSettings.instance.WindowStyle == WindowStyle.Minimized ?
                ProcessWindowStyle.Minimized :
                ProcessWindowStyle.Hidden;

            m_Process.StartInfo.FileName = UserSettings.instance.GetExecutablePath();
            m_Process.StartInfo.WindowStyle = windowStyle;
            m_Process.StartInfo.Arguments = m_Configuration.ConvertToConsoleArguments(m_Parameters);

            // This is the same admin elevation strategy that Clumsy uses: https://github.com/jagt/clumsy/blob/master/src/elevate.c#L125.
            // We need to elevate it ourselves, otherwise Clumsy will self-elevate and try to spawn a new process which would complicate matters for us.
            m_Process.StartInfo.Verb = "runas";
            m_Process.StartInfo.UseShellExecute = true; // Required for "runas"
            
#if CLUMSY_LOG
            UnityEngine.Debug.Log($"Clumsy: Started external process with arguments: {m_Process.StartInfo.Arguments}");
#endif

            m_Process.Start();
        }

        /// <summary>
        /// Stops the external Clumsy process/window if it's running.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            m_Process.Kill();
            m_Process.WaitForExit();
            m_Process.Dispose();
            m_Process = null;
            
#if CLUMSY_LOG
            UnityEngine.Debug.Log("Clumsy: Stopped external process");
#endif
        }

        /// <summary>
        /// Set a parameter value to replace the marker inside the filter string.
        /// If <paramref name="autoStart"/> is true, then Clumsy will start automatically once all parameters have been provided.
        /// </summary>
        public void SetParameter(string name, string value, bool autoStart = true)
        {
            m_Parameters.SetParameter(name, value);

#if CLUMSY_LOG
            UnityEngine.Debug.Log($"Clumsy: Parameter \"{name}\" set to value \"{value}\"");
#endif

            if (m_Parameters.IsComplete)
                Start();
        }
    }
}
