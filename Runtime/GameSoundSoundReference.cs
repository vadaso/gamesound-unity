using System;
using UnityEngine;

namespace GameSound.Unity
{
    [Serializable]
    public sealed class GameSoundSoundReference
    {
        [SerializeField] private GameSoundAsset asset;
        [SerializeField] private string projectId;
        [SerializeField] private string projectName;
        [SerializeField] private string itemId;
        [SerializeField] private string soundId;
        [SerializeField] private string source;
        [SerializeField] private string versionHash;
        [SerializeField] private string title;
        [SerializeField] private string folderPath;
        [SerializeField] private string soundType;
        [SerializeField] private string format;

        public GameSoundAsset Asset => asset;
        public string ProjectId => projectId;
        public string ProjectName => projectName;
        public string ItemId => itemId;
        public string SoundId => soundId;
        public string Source => source;
        public string VersionHash => versionHash;
        public string Title => title;
        public string FolderPath => folderPath;
        public string SoundType => soundType;
        public string Format => format;
        public AudioClip Clip => asset != null ? asset.Clip : null;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(title)) return title;
                if (!string.IsNullOrWhiteSpace(soundId)) return soundId;
                return "Unassigned GameSound";
            }
        }

        public void ApplyAsset(GameSoundAsset newAsset)
        {
            asset = newAsset;
            if (newAsset == null) return;

            projectId = newAsset.ProjectId;
            projectName = newAsset.ProjectName;
            itemId = newAsset.ItemId;
            soundId = newAsset.SoundId;
            source = newAsset.Source;
            versionHash = newAsset.VersionHash;
            title = newAsset.Title;
            folderPath = newAsset.FolderPath;
            soundType = newAsset.SoundType;
            format = newAsset.Format;
        }

        public bool Matches(string candidateItemId, string candidateSoundId)
        {
            if (!string.IsNullOrWhiteSpace(candidateItemId) &&
                string.Equals(itemId, candidateItemId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(candidateSoundId) &&
                string.Equals(soundId, candidateSoundId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
