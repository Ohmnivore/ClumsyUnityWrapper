using UnityEditor;
using UnityEngine;

namespace Clumsy.Editor
{
    /// <summary>
    /// The automatic lifecycle of the Clumsy executable.
    /// </summary>
    public class EntryPoint
    {
        [InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            var config = ProjectSettings.instance.Configuration;

            // Prepare the config that was set in the project settings.
            // The user might specify a different one later, but this one will be ready for Runner.Instance.SetParameter.
            if (ProjectSettings.instance.IsEnabled && config != null)
                Runner.Instance.SetConfiguration(config);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                var config = ProjectSettings.instance.Configuration;

                if (ProjectSettings.instance.IsEnabled && config is { AutoStart: true })
                    Runner.Instance.Start();
            }
            else if (stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                Runner.Instance.Stop();
            }
        }
    }
}
