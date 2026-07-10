using System;
using System.Linq;
using System.Reflection;
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

            var reference = component.SoundReference;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Reference", reference.DisplayName);
                if (!string.IsNullOrWhiteSpace(reference.ProjectName)) EditorGUILayout.LabelField("Project", reference.ProjectName);
                if (!string.IsNullOrWhiteSpace(reference.FolderPath)) EditorGUILayout.LabelField("Folder", reference.FolderPath);
                if (!string.IsNullOrWhiteSpace(reference.Source)) EditorGUILayout.LabelField("Source", reference.Source);
                if (!string.IsNullOrWhiteSpace(reference.Format)) EditorGUILayout.LabelField("Format", reference.Format.ToUpperInvariant());
            }

            var clip = ResolveClip(component, reference);
            if (component.StopMode == GameSoundStopMode.FadeOut &&
                (component.StopTrigger == GameSoundEmitterTrigger.ObjectDisable ||
                 component.StopTrigger == GameSoundEmitterTrigger.ObjectDestroy))
            {
                EditorGUILayout.HelpBox(
                    "Fade Out cannot continue after the owning GameObject is disabled or destroyed, so this lifecycle stop is immediate. Use another stop trigger for a timed fade.",
                    MessageType.Info);
            }

            if (component.Loop &&
                (component.PlayTrigger == GameSoundEmitterTrigger.ObjectDisable ||
                 component.PlayTrigger == GameSoundEmitterTrigger.ObjectDestroy))
            {
                EditorGUILayout.HelpBox(
                    "Disable/Destroy playback is emitted as a detached one-shot and does not loop.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(clip == null))
            {
                if (GUILayout.Button("Apply GameSound Clip to AudioSource"))
                {
                    component.ApplyToAudioSource();
                    EditorUtility.SetDirty(component);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(clip == null))
                {
                    if (GUILayout.Button(Application.isPlaying ? "Play" : "Preview"))
                    {
                        PlayOrPreview(component, clip);
                    }
                }

                if (GUILayout.Button("Stop"))
                {
                    StopPlayback(component);
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

        private static AudioClip ResolveClip(GameSoundAudioSource component, GameSoundSoundReference reference)
        {
            if (component.Sound != null && component.Sound.Clip != null) return component.Sound.Clip;
            return reference != null ? reference.Clip : null;
        }

        private static void PlayOrPreview(GameSoundAudioSource component, AudioClip clip)
        {
            component.ApplyToAudioSource();
            if (Application.isPlaying)
            {
                component.Play();
                return;
            }

            if (!EditorAudioPreview.Play(clip))
            {
                Debug.LogWarning("GameSound could not start Unity editor audio preview. Enter Play Mode or preview the AudioClip directly in the Project window.");
            }
        }

        private static void StopPlayback(GameSoundAudioSource component)
        {
            if (Application.isPlaying)
            {
                component.StopImmediate();
                return;
            }

            EditorAudioPreview.StopAll();
        }

        private static class EditorAudioPreview
        {
            private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

            public static bool Play(AudioClip clip)
            {
                if (clip == null) return false;
                StopAll();
                return TryInvoke("PlayPreviewClip", clip, 0, false) ||
                       TryInvoke("PlayClip", clip, 0, false) ||
                       TryInvoke("PlayPreviewClip", clip) ||
                       TryInvoke("PlayClip", clip);
            }

            public static void StopAll()
            {
                if (!TryInvoke("StopAllPreviewClips"))
                {
                    TryInvoke("StopAllClips");
                }
            }

            private static bool TryInvoke(string methodName, params object[] args)
            {
                if (AudioUtilType == null) return false;
                var methods = AudioUtilType
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(method => method.Name == methodName);

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != args.Length) continue;
                    if (!ParametersMatch(parameters, args)) continue;

                    try
                    {
                        method.Invoke(null, args);
                        return true;
                    }
                    catch (TargetInvocationException ex)
                    {
                        Debug.LogWarning($"GameSound editor audio preview failed: {ex.InnerException?.Message ?? ex.Message}");
                        return false;
                    }
                }

                return false;
            }

            private static bool ParametersMatch(ParameterInfo[] parameters, object[] args)
            {
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (args[i] == null)
                    {
                        if (parameters[i].ParameterType.IsValueType) return false;
                        continue;
                    }

                    if (!parameters[i].ParameterType.IsInstanceOfType(args[i])) return false;
                }

                return true;
            }
        }
    }
}
