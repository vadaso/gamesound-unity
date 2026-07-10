using System;

namespace GameSound.Unity.Editor
{
    [Serializable]
    internal sealed class DeviceStartRequest
    {
        public string clientName;
        public string unityVersion;
        public string packageVersion;
    }

    [Serializable]
    internal sealed class DeviceStartResponse
    {
        public string userCode;
        public string deviceCode;
        public string verificationUri;
        public string verificationUriComplete;
        public int expiresIn;
        public int interval;
        public long timestamp;
    }

    [Serializable]
    internal sealed class DevicePollRequest
    {
        public string deviceCode;
    }

    [Serializable]
    internal sealed class DevicePollResponse
    {
        public string status;
        public string accessToken;
        public int expiresIn;
        public string tokenType;
        public long timestamp;
    }

    [Serializable]
    internal sealed class ProjectsResponse
    {
        public GameSoundProjectDto[] projects;
        public long timestamp;
    }

    [Serializable]
    internal sealed class GameSoundProjectDto
    {
        public string id;
        public string name;
        public string description;
        public string updatedAt;
        public bool isOwner;
        public bool isShared;
        public string orgProfileId;
        public string contextType;
        public string role;
        public bool canWrite;
        public bool canPublishToUnity;
    }

    [Serializable]
    internal sealed class ManifestResponse
    {
        public GameSoundProjectDto project;
        public string manifestVersion;
        public GameSoundFolderDto[] folders;
        public GameSoundManifestItemDto[] items;
        public long timestamp;
    }

    [Serializable]
    internal sealed class GameSoundFolderDto
    {
        public string id;
        public string name;
        public string parentId;
        public string path;
        public int order;
    }

    [Serializable]
    internal sealed class GameSoundManifestItemDto
    {
        public string itemId;
        public string soundId;
        public string source;
        public string title;
        public string type;
        public double duration;
        public string format;
        public string folderPath;
        public int order;
        public string versionHash;
        public string updatedAt;
        public GameSoundUnitySettingsDto unity;
    }

    [Serializable]
    internal sealed class GameSoundUnitySettingsDto
    {
        public bool loop;
        public float volume;
        public float spatialBlend;
        public float minDistance;
        public float maxDistance;
        public float randomPitchMin;
        public float randomPitchMax;
    }

    [Serializable]
    internal sealed class DownloadResponse
    {
        public string url;
        public int expiresIn;
        public string type;
        public string fileName;
        public string format;
        public string versionHash;
        public long timestamp;
    }


}
