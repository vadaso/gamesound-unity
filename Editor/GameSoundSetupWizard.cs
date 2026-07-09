using UnityEditor;
using UnityEngine;

namespace GameSound.Unity.Editor
{
    public sealed class GameSoundSetupWizard : EditorWindow
    {
        [MenuItem("Window/GameSound/Setup Wizard")]
        public static void Open()
        {
            GetWindow<GameSoundSetupWizard>("GameSound Setup");
        }

        private void OnEnable()
        {
            minSize = new Vector2(480, 360);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("GameSound Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this once per Unity project to validate the editor connection, import folder, and scene audio basics. GameSound uses Unity AudioSource, so do not disable Unity built-in audio.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            DrawConnectionStep();
            DrawImportStep();
            DrawSceneStep();
            DrawNextStep();
        }

        private static void DrawConnectionStep()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("1. Connection", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("API Host", GameSoundEditorPrefs.ApiBaseUrl);
                EditorGUILayout.HelpBox("Public packages always connect to gamesound.ai. There is no per-project API URL to configure.", MessageType.None);
                EditorGUILayout.LabelField("Login", string.IsNullOrWhiteSpace(GameSoundEditorPrefs.AccessToken) ? "Not connected" : "Token saved");
                if (GUILayout.Button("Open GameSound Window"))
                {
                    GameSoundWindow.Open();
                }
            }
        }

        private static void DrawImportStep()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("2. Import Folder", EditorStyles.boldLabel);
                GameSoundEditorPrefs.ImportRoot = EditorGUILayout.TextField("Import Root", GameSoundEditorPrefs.ImportRoot);
                var valid = AssetDatabase.IsValidFolder(GameSoundEditorPrefs.ImportRoot);
                EditorGUILayout.LabelField("Status", valid ? "Exists" : "Missing");
                if (!valid && GUILayout.Button("Create Import Folder"))
                {
                    EnsureAssetFolder(GameSoundEditorPrefs.ImportRoot);
                    AssetDatabase.Refresh();
                }
            }
        }

        private static void DrawSceneStep()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("3. Scene Audio", EditorStyles.boldLabel);
                var listener = Object.FindObjectOfType<AudioListener>();
                EditorGUILayout.LabelField("Audio Listener", listener == null ? "Missing" : listener.name);
                if (listener == null)
                {
                    EditorGUILayout.HelpBox("Add an AudioListener to the camera or listener object before testing 3D GameSound emitters.", MessageType.Warning);
                }
            }
        }

        private static void DrawNextStep()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("4. Next", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Open Window > GameSound, login, load a manifest, then drag a sound into the Scene view or click Create Emitter.", EditorStyles.wordWrappedLabel);
            }
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder)) assetFolder = "Assets/GameSound";
            assetFolder = assetFolder.Trim().TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
