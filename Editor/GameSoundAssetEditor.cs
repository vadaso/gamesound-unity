using GameSound.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSound.Unity.Editor
{
    [CustomEditor(typeof(GameSoundAsset))]
    internal sealed class GameSoundAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (GameSoundAsset)target;

            EditorGUILayout.LabelField("GameSound Asset", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawText("Title", asset.Title);
                DrawText("Project", asset.ProjectName);
                DrawText("Folder", string.IsNullOrWhiteSpace(asset.FolderPath) ? "/" : asset.FolderPath);
                DrawText("Source", asset.Source);
                DrawText("Type", asset.SoundType);
                DrawText("Format", asset.Format);
                if (asset.Duration > 0)
                {
                    EditorGUILayout.LabelField("Duration", FormatDuration(asset.Duration));
                }
                DrawText("Last Synced", asset.LastSyncedAtUtc);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Audio Clip", asset.Clip, typeof(AudioClip), false);
            }

            using (new EditorGUI.DisabledScope(asset.Clip == null))
            {
                if (GUILayout.Button("Ping Audio Clip"))
                {
                    EditorGUIUtility.PingObject(asset.Clip);
                }
            }
        }

        private static void DrawText(string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                EditorGUILayout.LabelField(label, value);
            }
        }

        private static string FormatDuration(double seconds)
        {
            var total = Mathf.Max(0, Mathf.RoundToInt((float)seconds));
            return $"{total / 60}:{(total % 60):00}";
        }
    }
}
