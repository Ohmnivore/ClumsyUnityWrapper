using UnityEditor;
using UnityEngine;

namespace Clumsy.Editor
{
    /// <summary>
    /// Project-specific settings.
    /// </summary>
    [FilePath(FilePath, FilePathAttribute.Location.ProjectFolder)]
    class ProjectSettings : ScriptableSingleton<ProjectSettings>
    {
        const string FilePath = "ProjectSettings/Packages/Clumsy.settings";

        public bool IsEnabled
        {
            get => m_IsEnabled;
            internal set => m_IsEnabled = value;
        }

        public ClumsyConfiguration Configuration
        {
            get => m_Configuration;
            internal set => m_Configuration = value;
        }

        [Tooltip("Whether Clumsy should run when entering Play mode.")]
        [SerializeField]
        bool m_IsEnabled;

        [Tooltip("The configuration Clumsy should use.")]
        [SerializeField]
        ClumsyConfiguration m_Configuration;

        public void SaveChanges()
        {
            Save(true);
        }
    }
}
