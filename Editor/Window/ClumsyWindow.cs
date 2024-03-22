using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Clumsy.Editor.Window
{
    public class ClumsyWindow : EditorWindow
    {
        private const string Menu = "Window/Clumsy/";
        private const string Title = "Clumsy";
        private static readonly Vector2 MinSize = new Vector2(320f, 240f);

        [MenuItem(Menu + Title)]
        private static void Open()
        {
            // Dock next to the console window
            var consoleWindowType = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor.CoreModule");

            var window = GetWindow<ClumsyWindow>(Title, true, consoleWindowType);
            window.minSize = MinSize;
        }

        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        private Texture2D m_Icon;

        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        private VisualTreeAsset m_Layout;

        // This field is assigned through a ScriptableObject default reference to avoid dealing with paths
        [SerializeField]
        private StyleSheet m_Style;

        private ToolbarToggle m_EnabledToggle;
        private ToolbarMenuExt m_ConfigurationMenu;
        private ToolbarButton m_SettingsButton;
        private Label m_StatusLabel;
        private VisualElement m_ConfigurationEditorContainer;
        private VisualElement m_ConfigurationEditorGUI;
        private UnityEditor.Editor m_ConfigurationEditor;

        private void OnDisable()
        {
            if (m_ConfigurationEditor != null)
            {
                DestroyImmediate(m_ConfigurationEditor);
                m_ConfigurationEditor = null;
            }
        }

        private void CreateGUI()
        {
            titleContent.image = m_Icon;

            m_Layout.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(m_Style);

            m_EnabledToggle = rootVisualElement.Q<ToolbarToggle>("enabled");
            m_ConfigurationMenu = rootVisualElement.Q<ToolbarMenuExt>("configuration");
            m_SettingsButton = rootVisualElement.Q<ToolbarButton>("settings");
            m_StatusLabel = rootVisualElement.Q<Label>("status");
            m_ConfigurationEditorContainer = rootVisualElement.Q<VisualElement>("configuration-editor-container");

            m_EnabledToggle.value = ProjectSettings.instance.IsEnabled;
            m_EnabledToggle.RegisterValueChangedCallback(OnEnableToggleChanged);

            m_ConfigurationMenu.Dropdown = new ConfigurationDropdown(OnConfigurationChanged, IsConfigurationSelected, OnConfigurationCreate);

            UpdateConfigurationMenuLabel();
            UpdateConfigurationEditor();
            UpdateConfigurationEnabled();

            m_SettingsButton.clicked += OnSettingsButtonClicked;

            UpdateStatusLabel();
            rootVisualElement.schedule.Execute(UpdateStatusLabel).Every(1000);

            EditorApplication.playModeStateChanged += OnplayModeStateChanged;
        }

        private void OnplayModeStateChanged(PlayModeStateChange stateChange)
        {
            UpdateConfigurationEnabled();
            UpdateStatusLabel();
        }

        private void UpdateConfigurationEnabled()
        {
            var disabled = ProjectSettings.instance.IsEnabled && Application.isPlaying;
            m_ConfigurationEditorContainer.SetEnabled(!disabled);
        }

        private void OnEnableToggleChanged(ChangeEvent<bool> evt)
        {
            ProjectSettings.instance.IsEnabled = evt.newValue;
            ProjectSettings.instance.SaveChanges();

            UpdateConfigurationEnabled();

            if (Application.isPlaying)
            {
                if (evt.newValue)
                {
                    Runner.Instance.SetConfiguration(ProjectSettings.instance.Configuration);
                    Runner.Instance.Start();
                }
                else
                {
                    Runner.Instance.Stop();
                }
            }

            UpdateStatusLabel();
        }

        private bool IsConfigurationSelected(string assetPath)
        {
            var config = AssetDatabase.LoadAssetAtPath<ClumsyConfiguration>(assetPath);
            return config == ProjectSettings.instance.Configuration;
        }

        private void OnConfigurationChanged(string newAssetPath)
        {
            var config = AssetDatabase.LoadAssetAtPath<ClumsyConfiguration>(newAssetPath);

            ProjectSettings.instance.Configuration = config;
            ProjectSettings.instance.SaveChanges();

            UpdateConfigurationMenuLabel();
            UpdateConfigurationEditor();
            UpdateStatusLabel();
        }

        private void OnConfigurationCreate()
        {
            var newConfig = ScriptableObject.CreateInstance<ClumsyConfiguration>();
            var newPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Clumsy Configuration.asset");

            AssetDatabase.CreateAsset(newConfig, newPath);

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newConfig;
        }

        private void UpdateConfigurationMenuLabel()
        {
            var config = ProjectSettings.instance.Configuration;

            m_ConfigurationMenu.text = config == null ?
                "Pick Configuration..." :
                config.name;
        }

        private void UpdateConfigurationEditor()
        {
            var config = ProjectSettings.instance.Configuration;

            if (config == null)
            {
                DestroyImmediate(m_ConfigurationEditor);
                m_ConfigurationEditor = null;
                m_ConfigurationEditorGUI.RemoveFromHierarchy();
            }

            if (m_ConfigurationEditor == null)
            {
                m_ConfigurationEditor = UnityEditor.Editor.CreateEditor(config);
                m_ConfigurationEditorGUI = m_ConfigurationEditor.CreateInspectorGUI();
                m_ConfigurationEditorContainer.Add(m_ConfigurationEditorGUI);
            }
            else
            {
                // Avoid re-creating the UI
                UnityEditor.Editor.CreateCachedEditor(config, null, ref m_ConfigurationEditor);
                m_ConfigurationEditorGUI.Bind(m_ConfigurationEditor.serializedObject);
            }
        }

        private void OnSettingsButtonClicked()
        {
            SettingsService.OpenUserPreferences(ClumsySettingsProvider.MenuPath);
        }

        private void UpdateStatusLabel()
        {
            var text = GetStatusLabelText();
            m_StatusLabel.text = $"Status: {text}";
        }

        private string GetStatusLabelText()
        {
            if (!ProjectSettings.instance.IsEnabled)
                return "Disabled";

            if (Application.isPlaying && Runner.Instance.IsRunning)
            {
                return "Running";
            }
            else if (Application.isPlaying && Runner.Instance.WasClosedExternally)
            {
                return "Closed";
            }
            else
            {
                if (ProjectSettings.instance.Configuration == null)
                    return "No configuration selected";
                else
                    return "Ready";
            }
        }
    }

    public class ConfigurationDropdown : ToolbarMenuExt.IDropdown
    {
        public Action<string> OnSelected;
        public Func<string, bool> IsSelected;
        public Action OnCreate;

        public ConfigurationDropdown(Action<string> onSelected, Func<string, bool> isSelected, Action onCreate)
        {
            OnSelected = onSelected;
            IsSelected = isSelected;
            OnCreate = onCreate;
        }

        public void Show(Rect rect)
        {
            var menu = new GenericMenu();

            var assetPaths = FindAllConfigurationAssetPaths();
            foreach (var assetPath in assetPaths)
            {
                // https://discussions.unity.com/t/can-genericmenu-item-content-display/63119/4
                var displayText = assetPath.Replace("/", "\u200A\u2215\u200A");

                menu.AddItem(new GUIContent(displayText), IsSelected(assetPath), () => OnSelected?.Invoke(assetPath));
            }

            menu.AddItem(new GUIContent("Create new configuration..."), false, () => OnCreate?.Invoke());

            menu.DropDown(rect);
        }

        private static IEnumerable<string> FindAllConfigurationAssetPaths()
        {
            // Search everywhere: all Assets and Packages
            var guids = AssetDatabase.FindAssets($"t:{nameof(ClumsyConfiguration)}", new []{ "Assets", "Packages" } );
            return guids.Select(AssetDatabase.GUIDToAssetPath);
        }
    }
}
