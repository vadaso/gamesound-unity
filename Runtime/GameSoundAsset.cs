using System;
using UnityEngine;

namespace GameSound.Unity
{
    [CreateAssetMenu(menuName = "GameSound/GameSound Asset", fileName = "GameSoundAsset")]
    public sealed class GameSoundAsset : ScriptableObject
    {
        [SerializeField, HideInInspector] private string projectId;
        [SerializeField, HideInInspector] private string itemId;
        [SerializeField, HideInInspector] private string soundId;
        [SerializeField, HideInInspector] private string versionHash;

        [Header("Display")]
        [SerializeField] private string projectName;
        [SerializeField] private string source;
        [SerializeField] private string title;
        [SerializeField] private string folderPath;
        [SerializeField] private string soundType;
        [SerializeField] private double duration;
        [SerializeField] private string format;
        [SerializeField] private string lastSyncedAtUtc;

        [Header("Unity Asset")]
        [SerializeField] private AudioClip clip;

        public string ProjectId => projectId;
        public string ProjectName => projectName;
        public string ItemId => itemId;
        public string SoundId => soundId;
        public string Source => source;
        public string VersionHash => versionHash;
        public string Title => title;
        public string FolderPath => folderPath;
        public string SoundType => soundType;
        public double Duration => duration;
        public string Format => format;
        public string LastSyncedAtUtc => lastSyncedAtUtc;
        public AudioClip Clip => clip;

        public void ApplyRemoteMetadata(
            string newProjectId,
            string newProjectName,
            string newItemId,
            string newSoundId,
            string newSource,
            string newVersionHash,
            string newTitle,
            string newFolderPath,
            string newSoundType,
            double newDuration,
            string newFormat,
            AudioClip newClip)
        {
            projectId = newProjectId;
            projectName = newProjectName;
            itemId = newItemId;
            soundId = newSoundId;
            source = newSource;
            versionHash = newVersionHash;
            title = newTitle;
            folderPath = newFolderPath;
            soundType = newSoundType;
            duration = newDuration;
            format = newFormat;
            clip = newClip;
            lastSyncedAtUtc = DateTimeOffset.UtcNow.ToString("O");
        }
    }
}
