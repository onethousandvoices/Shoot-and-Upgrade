using Controllers;
using MainScene;
using Models;
using Save;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace Editor
{
    public sealed class ToolsWindow : EditorWindow
    {
        private static readonly string[] _tabNames = { "Save", "Player" };
        
        private IObjectResolver _resolver;
        private int _activeTab;
        private bool _resolved;

        [MenuItem("Tools/Game Tools %&t")]
        private static void Open() => GetWindow<ToolsWindow>("Game Tools");

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private void OnGUI()
        {
            _activeTab = GUILayout.Toolbar(_activeTab, _tabNames);
            EditorGUILayout.Space(8);
            
            switch (_activeTab)
            {
                case 0:
                    DrawSaveTab();
                    break;
                case 1:
                    DrawPlayerTab();
                    break;
            }
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private static void DrawSaveTab()
        {
            if (!GUILayout.Button("Wipe Save"))
                return;
            SaveSystem.Wipe();
            Debug.Log("[Tools] Save wiped");
        }

        private void DrawPlayerTab()
        {
            if (!RequirePlayMode()) return;
            
            if (!TryResolve<PlayerController>(out var player) || !player.IsAlive)
            {
                EditorGUILayout.HelpBox("Player is not alive", MessageType.Warning);
                return;
            }
            
            if (!TryResolve<PlayerModel>(out var model)) return;
            
            EditorGUILayout.LabelField("HP", $"{model.CurrentHealth:F0} / {model.MaxHealth:F0}");
            
            using (new EditorGUI.DisabledScope(model.CurrentHealth >= model.MaxHealth))
            {
                if (GUILayout.Button("Heal to Full"))
                    model.Heal(model.MaxHealth);
            }
            
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Upgrade Points", model.UpgradePoints.ToString());
            
            if (GUILayout.Button("+10 Points"))
                model.AddUpgradePoints(10);
        }

        private static bool RequirePlayMode()
        {
            if (Application.isPlaying) return true;
            EditorGUILayout.HelpBox("Enter Play Mode first", MessageType.Info);
            return false;
        }

        private bool TryResolve<T>(out T result) where T : class
        {
            result = null;
            if (!EnsureResolver()) return false;
            
            result = _resolver.Resolve<T>();
            return result != null;
        }

        private bool EnsureResolver()
        {
            if (_resolved && _resolver != null) return true;
            
            var scope = FindAnyObjectByType<MainSceneLifetimeScope>();
            if (!scope) return false;
            
            _resolver = scope.Container;
            _resolved = _resolver != null;
            return _resolved;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            ResetResolver();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ResetResolver();
        }

        private void ResetResolver()
        {
            _resolver = null;
            _resolved = false;
        }
    }
}
