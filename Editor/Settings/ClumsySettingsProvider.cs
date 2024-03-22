using UnityEditor;
using UnityEngine.UIElements;

namespace Clumsy.Editor
{
    /// <summary>
    /// Project-independent settings.
    /// </summary>
    class ClumsySettingsProvider : SettingsProvider
    {
        public const string MenuPath = "Preferences/Clumsy";

        SerializedObject m_ChangeTracker;

        private ClumsySettingsProvider() :
            base(MenuPath, SettingsScope.User)
        {

        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            m_ChangeTracker = new SerializedObject(UserSettings.instance);

            var editor = UnityEditor.Editor.CreateEditor(UserSettings.instance);
            var gui = editor.CreateInspectorGUI();

            rootElement.Add(gui);
        }

        public override void OnInspectorUpdate()
        {
            base.OnInspectorUpdate();

            // We need to manually save the changes after modifications made in the UI
            if (m_ChangeTracker.UpdateIfRequiredOrScript())
                ProjectSettings.instance.SaveChanges();
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            var provider = new ClumsySettingsProvider();

            var serializedObject = new SerializedObject(UserSettings.instance);

            // Make searchable
            provider.keywords = GetSearchKeywordsFromSerializedObject(serializedObject);

            return provider;
        }
    }
}
