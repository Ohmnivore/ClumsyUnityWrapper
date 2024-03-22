using System.IO;
using UnityEditor;
using UnityEngine;

namespace Clumsy.Editor
{
    public enum WindowStyle
    {
        [Tooltip("The window is minimized")]
        Minimized,

        [Tooltip("The window is completely hidden")]
        Hidden
    }

    /// <summary>
    /// Project-independent settings.
    /// </summary>
    [FilePath(FilePath, FilePathAttribute.Location.PreferencesFolder)]
    class UserSettings : ScriptableSingleton<UserSettings>
    {
        const string FilePath = "Clumsy/Clumsy.settings";
        const string ExecutableFileName = "clumsy.exe";
        const string BundledClumsyDirectoryPath = "Packages/com.ohmnivore.clumsyunitywrapper/dist~/clumsy-0.3-win64-c";
        
        public string GetExecutablePath()
        {
            var directory = string.IsNullOrEmpty(CustomInstallationDirectory) ?
                Path.GetFullPath(BundledClumsyDirectoryPath) :
                CustomInstallationDirectory;

            return Path.Join(directory, ExecutableFileName);
        }

        public string CustomInstallationDirectory
        {
            get => m_CustomInstallationDirectory;
            internal set => m_CustomInstallationDirectory = value;
        }

        public WindowStyle WindowStyle
        {
            get => m_WindowStyle;
            internal set => m_WindowStyle = value;
        }

        [Tooltip("Optional path to a directory containing Clumsy binaries. If empty then the default bundled Clumsy version is used.")]
        [SerializeField]
        string m_CustomInstallationDirectory;

        [Tooltip("The style to apply onto the external Clumsy window.")]
        [SerializeField]
        WindowStyle m_WindowStyle = WindowStyle.Minimized;

        public void SaveChanges()
        {
            Save(true);
        }

        public void Reset()
        {
            m_CustomInstallationDirectory = null;
            m_WindowStyle = WindowStyle.Minimized;

            SaveChanges();
        }
    }
}
