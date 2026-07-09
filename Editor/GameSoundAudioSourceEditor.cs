using GameSound.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSound.Unity.Editor
{
    [CustomEditor(typeof(GameSoundAudioSource), true)]
    internal sealed class GameSoundAudioSourceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var component = (GameSoundAudioSource)target;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("GameSound Emitter", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var reference = component.SoundReference;
                EditorGUILayout.LabelField("Reference", reference.DisplayName);
                if (!string.IsNullOrWhiteSpace(reference.FolderPath)) EditorGUILayout.LabelField("Folder", reference.FolderPath);
                if (!string.IsNullOrWhiteSpace(reference.ItemId)) EditorGUILayout.LabelField("Item ID", reference.ItemId);
                if (!string.IsNullOrWhiteSpace(reference.SoundId)) EditorGUILayout.LabelField("Sound ID", reference.SoundId);
                if (!string.IsNullOrWhiteSpace(reference.VersionHash)) EditorGUILayout.LabelField("Version", reference.VersionHash);
            }

            using (new EditorGUI.DisabledScope(component.Sound == null || component.Sound.Clip == null))
            {
                if (GUILayout.Button("Apply GameSound Clip to AudioSource"))
                {
                    component.ApplyToAudioSource();
                    EditorUtility.SetDirty(component);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(component.Sound == null || component.Sound.Clip == null))
                {
                    if (GUILayout.Button("Play"))
                    {
                        component.Play();
                    }
                }

                if (GUILayout.Button("Stop"))
                {
                    component.StopImmediate();
                }
            }

            if (component.Sound != null)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Imported Asset", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Project", component.Sound.ProjectName);
                    EditorGUILayout.LabelField("Format", component.Sound.Format);
                    EditorGUILayout.LabelField("Last Synced", component.Sound.LastSyncedAtUtc);

                    if (GUILayout.Button("Ping GameSound Asset"))
                    {
                        EditorGUIUtility.PingObject(component.Sound);
                        Selection.activeObject = component.Sound;
                    }
                }
            }
        }
    }
}
