using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameSound.Unity.Editor
{
    internal sealed class GameSoundApiClient
    {
        public const string PackageVersion = "0.3.3";
        private const string ProductionApiBaseUrl = "https://gamesound.ai";

        private readonly string baseUrl;
        private readonly string accessToken;

        public GameSoundApiClient(string baseUrl, string accessToken)
        {
            this.baseUrl = (string.IsNullOrWhiteSpace(baseUrl) ? ProductionApiBaseUrl : baseUrl.Trim()).TrimEnd('/');
            this.accessToken = accessToken ?? string.Empty;
        }

        public Task<DeviceStartResponse> StartDeviceLoginAsync()
        {
            var request = new DeviceStartRequest
            {
                clientName = SystemInfo.deviceName,
                unityVersion = Application.unityVersion,
                packageVersion = PackageVersion
            };
            return PostJsonAsync<DeviceStartRequest, DeviceStartResponse>("/ssr-api/unity/auth/device/start", request, false);
        }

        public Task<DevicePollResponse> PollDeviceLoginAsync(string deviceCode)
        {
            var request = new DevicePollRequest { deviceCode = deviceCode };
            return PostJsonAsync<DevicePollRequest, DevicePollResponse>("/ssr-api/unity/auth/device/poll", request, false);
        }

        public Task<ProjectsResponse> GetProjectsAsync()
        {
            return GetJsonAsync<ProjectsResponse>("/ssr-api/unity/projects");
        }

        public Task<ManifestResponse> GetManifestAsync(string projectId)
        {
            return GetJsonAsync<ManifestResponse>($"/ssr-api/unity/projects/{UnityWebRequest.EscapeURL(projectId)}/manifest");
        }

        public Task<DownloadResponse> CreateDownloadAsync(string projectId, string soundId)
        {
            return PostJsonAsync<object, DownloadResponse>(
                $"/ssr-api/unity/projects/{UnityWebRequest.EscapeURL(projectId)}/assets/{UnityWebRequest.EscapeURL(soundId)}/download",
                new EmptyJson(),
                true);
        }

        public Task<UnityCommandsResponse> GetUnityCommandsAsync(string projectId, int limit = 20)
        {
            var escapedProjectId = UnityWebRequest.EscapeURL(projectId ?? string.Empty);
            var clampedLimit = Mathf.Clamp(limit, 1, 50);
            return GetJsonAsync<UnityCommandsResponse>($"/ssr-api/unity/commands?projectId={escapedProjectId}&limit={clampedLimit}");
        }

        public Task<UnityCommandAckResponse> AckUnityCommandAsync(string commandId, bool success, string errorMessage = null)
        {
            var request = new UnityCommandAckRequest
            {
                status = success ? "acked" : "failed",
                errorMessage = success ? null : errorMessage
            };
            return PostJsonAsync<UnityCommandAckRequest, UnityCommandAckResponse>(
                $"/ssr-api/unity/commands/{UnityWebRequest.EscapeURL(commandId)}/ack",
                request,
                true);
        }

        public Task<UnityConnectionHeartbeatResponse> SendHeartbeatAsync(string projectId, string manifestVersion)
        {
            var request = new UnityConnectionHeartbeatRequest
            {
                unityProjectGuid = GameSoundEditorPrefs.UnityProjectGuid,
                clientName = SystemInfo.deviceName,
                unityVersion = Application.unityVersion,
                packageVersion = PackageVersion,
                manifestVersion = manifestVersion
            };
            return PostJsonAsync<UnityConnectionHeartbeatRequest, UnityConnectionHeartbeatResponse>(
                $"/ssr-api/unity/projects/{UnityWebRequest.EscapeURL(projectId)}/unity/connections/heartbeat",
                request,
                true);
        }

        public Task LogoutAsync()
        {
            return PostJsonAsync<EmptyJson, EmptyJson>("/ssr-api/unity/logout", new EmptyJson(), true);
        }

        public async Task DownloadFileAsync(string url, string destinationPath)
        {
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerFile(destinationPath) { removeFileOnAbort = true };
                await SendAsync(request);
            }
        }

        private async Task<TResponse> GetJsonAsync<TResponse>(string path)
        {
            using (var request = UnityWebRequest.Get(baseUrl + path))
            {
                AddAuthHeader(request);
                await SendAsync(request);
                return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
            }
        }

        private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest body, bool requiresAuth)
        {
            using (var request = new UnityWebRequest(baseUrl + path, UnityWebRequest.kHttpVerbPOST))
            {
                var json = JsonUtility.ToJson(body);
                var bytes = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                if (requiresAuth) AddAuthHeader(request);
                await SendAsync(request);
                return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
            }
        }

        private void AddAuthHeader(UnityWebRequest request)
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            }
        }

        private static Task SendAsync(UnityWebRequest request)
        {
            var completion = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();
            operation.completed += _ => completion.TrySetResult(true);
            return AwaitAndThrowAsync(request, completion.Task);
        }

        private static async Task AwaitAndThrowAsync(UnityWebRequest request, Task waitTask)
        {
            await waitTask;
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                var body = request.downloadHandler != null ? SanitizeResponseBody(request.downloadHandler.text) : string.Empty;
                var safeUrl = RedactUrlQuery(request.url);
                var safeError = RedactUrlQuery(request.error);
                throw new InvalidOperationException($"GameSound API error ({request.responseCode}) at {safeUrl}: {safeError}\n{body}");
            }
        }

        private static string RedactUrlQuery(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var queryIndex = value.IndexOf('?');
            return queryIndex < 0 ? value : value.Substring(0, queryIndex) + "?<redacted>";
        }

        private static string SanitizeResponseBody(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Length <= 2000 ? value : value.Substring(0, 2000) + "…";
        }

        [Serializable]
        private sealed class EmptyJson { }
    }
}
