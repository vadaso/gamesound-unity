using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameSound.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSound.Unity.Editor
{
    internal static class GameSoundImporter
    {
        public static async Task<GameSoundAsset> ImportAsync(
            GameSoundApiClient api,
            GameSoundProjectDto project,
            GameSoundManifestItemDto item,
            string importRoot,
            bool forceDownload = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (item == null) throw new ArgumentNullException(nameof(item));

            var existingBeforeDownload = FindExistingGameSoundAsset(item.itemId, item.soundId);
            if (!forceDownload && IsAssetCurrent(existingBeforeDownload, item))
            {
                return existingBeforeDownload;
            }

            var download = await api.CreateDownloadAsync(project.id, item.soundId);
            var extension = NormalizeExtension(download.format, item.format);
            var folder = BuildAssetFolder(importRoot, project.name, item.folderPath);
            EnsureAssetFolder(folder);

            var baseName = SanitizeFileName(string.IsNullOrWhiteSpace(item.title) ? "GameSound Audio" : item.title);
            var audioAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}{extension}");
            var existingAsset = existingBeforeDownload ?? FindExistingGameSoundAsset(item.itemId, item.soundId);
            if (existingAsset != null && existingAsset.Clip != null)
            {
                var existingClipPath = AssetDatabase.GetAssetPath(existingAsset.Clip);
                if (!string.IsNullOrWhiteSpace(existingClipPath))
                {
                    audioAssetPath = existingClipPath;
                }
            }

            await api.DownloadFileAsync(download.url, audioAssetPath);

            var settingsAppliedBeforeImport = ApplyAudioImportSettings(audioAssetPath, item);
            AssetDatabase.ImportAsset(audioAssetPath, ImportAssetOptions.ForceUpdate);
            if (!settingsAppliedBeforeImport && ApplyAudioImportSettings(audioAssetPath, item))
            {
                AssetDatabase.WriteImportSettingsIfDirty(audioAssetPath);
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioAssetPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"Downloaded file was not imported as AudioClip: {audioAssetPath}");
            }

            var metadataPath = existingAsset != null
                ? AssetDatabase.GetAssetPath(existingAsset)
                : AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.gamesound.asset");

            var asset = existingAsset != null
                ? existingAsset
                : ScriptableObject.CreateInstance<GameSoundAsset>();

            asset.ApplyRemoteMetadata(
                project.id,
                project.name,
                item.itemId,
                item.soundId,
                item.source,
                string.IsNullOrWhiteSpace(download.versionHash) ? item.versionHash : download.versionHash,
                item.title,
                item.folderPath,
                item.type,
                item.duration,
                string.IsNullOrWhiteSpace(download.format) ? item.format : download.format,
                clip);

            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(asset, metadataPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        public static bool IsImportedAndCurrent(GameSoundManifestItemDto item)
        {
            if (item == null) return false;
            return IsAssetCurrent(FindExistingGameSoundAsset(item.itemId, item.soundId), item);
        }

        public static bool IsImported(GameSoundManifestItemDto item, Dictionary<string, string> importedVersionIndex)
        {
            if (item == null || importedVersionIndex == null) return false;
            return HasImportedKey(importedVersionIndex, ItemKey(item.itemId)) ||
                   HasImportedKey(importedVersionIndex, SoundKey(item.soundId));
        }

        public static bool IsImportedAndCurrent(GameSoundManifestItemDto item, Dictionary<string, string> importedVersionIndex)
        {
            if (item == null || importedVersionIndex == null || string.IsNullOrWhiteSpace(item.versionHash)) return false;
            return TryVersionMatches(importedVersionIndex, ItemKey(item.itemId), item.versionHash) ||
                   TryVersionMatches(importedVersionIndex, SoundKey(item.soundId), item.versionHash);
        }

        public static bool RefreshMetadataIfImported(GameSoundProjectDto project, GameSoundManifestItemDto item)
        {
            if (project == null || item == null) return false;

            var asset = FindExistingGameSoundAsset(item.itemId, item.soundId);
            if (asset == null || asset.Clip == null || !NeedsMetadataUpdate(asset, project, item))
            {
                return false;
            }

            asset.ApplyRemoteMetadata(
                project.id,
                project.name,
                item.itemId,
                item.soundId,
                item.source,
                string.IsNullOrWhiteSpace(item.versionHash) ? asset.VersionHash : item.versionHash,
                item.title,
                item.folderPath,
                item.type,
                item.duration,
                item.format,
                asset.Clip);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static Dictionary<string, string> BuildImportedVersionIndex()
        {
            var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("t:GameSoundAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameSoundAsset>(path);
                if (asset == null || asset.Clip == null) continue;
                var version = asset.VersionHash ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(asset.ItemId)) index[ItemKey(asset.ItemId)] = version;
                if (!string.IsNullOrWhiteSpace(asset.SoundId)) index[SoundKey(asset.SoundId)] = version;
            }
            return index;
        }

        private static bool HasImportedKey(Dictionary<string, string> index, string key)
        {
            return !string.IsNullOrWhiteSpace(key) && index.ContainsKey(key);
        }

        private static bool TryVersionMatches(Dictionary<string, string> index, string key, string versionHash)
        {
            return !string.IsNullOrWhiteSpace(key) &&
                   index.TryGetValue(key, out var importedVersion) &&
                   string.Equals(importedVersion, versionHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ItemKey(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId) ? null : "item:" + itemId;
        }

        private static string SoundKey(string soundId)
        {
            return string.IsNullOrWhiteSpace(soundId) ? null : "sound:" + soundId;
        }

        private static bool IsAssetCurrent(GameSoundAsset asset, GameSoundManifestItemDto item)
        {
            if (asset == null || item == null || asset.Clip == null) return false;
            if (string.IsNullOrWhiteSpace(item.versionHash)) return false;
            return string.Equals(asset.VersionHash, item.versionHash, StringComparison.OrdinalIgnoreCase);
        }

        private static bool NeedsMetadataUpdate(GameSoundAsset asset, GameSoundProjectDto project, GameSoundManifestItemDto item)
        {
            return !Same(asset.ProjectId, project.id) ||
                   !Same(asset.ProjectName, project.name) ||
                   !Same(asset.ItemId, item.itemId) ||
                   !Same(asset.SoundId, item.soundId) ||
                   !Same(asset.Source, item.source) ||
                   (!string.IsNullOrWhiteSpace(item.versionHash) && !Same(asset.VersionHash, item.versionHash)) ||
                   !Same(asset.Title, item.title) ||
                   !Same(asset.FolderPath, item.folderPath) ||
                   !Same(asset.SoundType, item.type) ||
                   !Same(asset.Format, item.format) ||
                   Math.Abs(asset.Duration - item.duration) > 0.001;
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        private static GameSoundAsset FindExistingGameSoundAsset(string itemId, string soundId)
        {
            if (string.IsNullOrWhiteSpace(itemId) && string.IsNullOrWhiteSpace(soundId)) return null;
            var guids = AssetDatabase.FindAssets("t:GameSoundAsset");
            GameSoundAsset soundMatch = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameSoundAsset>(path);
                if (asset == null) continue;

                if (!string.IsNullOrWhiteSpace(itemId) && asset.ItemId == itemId)
                {
                    return asset;
                }

                if (soundMatch == null && !string.IsNullOrWhiteSpace(soundId) && asset.SoundId == soundId)
                {
                    soundMatch = asset;
                }
            }
            return soundMatch;
        }

        private static bool ApplyAudioImportSettings(string path, GameSoundManifestItemDto item)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return false;

            importer.forceToMono = false;
            importer.loadInBackground = item.duration >= 10.0;

            var settings = importer.defaultSampleSettings;
            if (item.duration >= 30.0 || string.Equals(item.type, "bgm", StringComparison.OrdinalIgnoreCase))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.75f;
            }
            else if (item.duration <= 2.0)
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.quality = 1.0f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.quality = 0.8f;
            }

            importer.defaultSampleSettings = settings;
            EditorUtility.SetDirty(importer);
            return true;
        }

        private static string BuildAssetFolder(string importRoot, string projectName, string folderPath)
        {
            var root = string.IsNullOrWhiteSpace(importRoot) ? "Assets/GameSound" : importRoot.Trim().TrimEnd('/');
            var project = SanitizePathSegment(projectName);
            var remote = string.IsNullOrWhiteSpace(folderPath) ? string.Empty : SanitizeRelativePath(folderPath);
            return string.IsNullOrWhiteSpace(remote) ? $"{root}/{project}" : $"{root}/{project}/{remote}";
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
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

        private static string NormalizeExtension(string primaryFormat, string fallbackFormat)
        {
            var format = string.IsNullOrWhiteSpace(primaryFormat) ? fallbackFormat : primaryFormat;
            format = (format ?? "mp3").Trim().TrimStart('.').ToLowerInvariant();
            if (format == "mpeg") format = "mp3";
            if (format == "wave") format = "wav";
            return "." + format;
        }

        private static string SanitizeRelativePath(string path)
        {
            var pieces = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < pieces.Length; i++)
            {
                pieces[i] = SanitizePathSegment(pieces[i]);
            }
            return string.Join("/", pieces);
        }

        private static string SanitizePathSegment(string value)
        {
            var sanitized = SanitizeFileName(value);
            return string.IsNullOrWhiteSpace(sanitized) ? "Untitled" : sanitized;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Untitled";
            var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var sanitized = Regex.Replace(value, $"[{invalid}]", "_").Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "Untitled" : sanitized;
        }
    }
}
