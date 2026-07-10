using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

namespace GameSound.Unity.Editor
{
    internal sealed class GameSoundApiClient
    {
        private const string ProductionApiBaseUrl = "https://gamesound.ai";

        private readonly string accessToken;
        private static readonly string ResolvedPackageVersion = ResolvePackageVersion();

        public GameSoundApiClient(string accessToken)
        {
            this.accessToken = accessToken ?? string.Empty;
        }

        public static string PackageVersion => ResolvedPackageVersion;

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


        public Task LogoutAsync()
        {
            return PostJsonAsync<EmptyJson, EmptyJson>("/ssr-api/unity/logout", new EmptyJson(), true);
        }

        public async Task DownloadFileAsync(string url, string destinationPath)
        {
            var safeUrl = RequireHttpsUrl(url, "download");
            var fullDestinationPath = Path.GetFullPath(destinationPath);
            var destinationDirectory = Path.GetDirectoryName(fullDestinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new InvalidOperationException("GameSound download destination is invalid.");
            }

            Directory.CreateDirectory(destinationDirectory);
            var temporaryDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Library", "GameSound", "Downloads");
            Directory.CreateDirectory(temporaryDirectory);
            var temporaryPath = Path.Combine(temporaryDirectory, Guid.NewGuid().ToString("N") + ".download");

            try
            {
                using (var request = new UnityWebRequest(safeUrl, UnityWebRequest.kHttpVerbGET))
                {
                    request.downloadHandler = new DownloadHandlerFile(temporaryPath) { removeFileOnAbort = true };
                    await SendAsync(request);
                    ValidateDownloadedFile(temporaryPath, request.GetResponseHeader("Content-Length"));
                }

                ReplaceDownloadedFile(temporaryPath, fullDestinationPath);
            }
            finally
            {
                DeleteIfExists(temporaryPath);
            }
        }

        private async Task<TResponse> GetJsonAsync<TResponse>(string path)
        {
            using (var request = UnityWebRequest.Get(ProductionApiBaseUrl + path))
            {
                AddAuthHeader(request);
                await SendAsync(request);
                return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
            }
        }

        private async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest body, bool requiresAuth)
        {
            using (var request = new UnityWebRequest(ProductionApiBaseUrl + path, UnityWebRequest.kHttpVerbPOST))
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

        private static void ValidateDownloadedFile(string destinationPath, string contentLengthHeader)
        {
            if (!File.Exists(destinationPath))
            {
                throw new InvalidOperationException("GameSound download did not create a file.");
            }

            var actualBytes = new FileInfo(destinationPath).Length;
            if (actualBytes <= 0)
            {
                throw new InvalidOperationException("GameSound download returned an empty file.");
            }

            if (string.IsNullOrWhiteSpace(contentLengthHeader)) return;
            if (!long.TryParse(contentLengthHeader, out var expectedBytes)) return;
            if (expectedBytes < 0) return;

            if (actualBytes != expectedBytes)
            {
                throw new InvalidOperationException($"GameSound download incomplete. Expected {expectedBytes} bytes but wrote {actualBytes} bytes.");
            }
        }

        internal static string RequireHttpsUrl(string value, string purpose)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(uri.Host) ||
                !string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                throw new InvalidOperationException($"GameSound {purpose} URL must use HTTPS.");
            }

            // Keep the server-provided spelling intact because re-serializing an AWS/R2
            // presigned URL can change escaping and invalidate its signature.
            return value.Trim();
        }

        private static void ReplaceDownloadedFile(string temporaryPath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                File.Move(temporaryPath, destinationPath);
                return;
            }

            try
            {
                File.Replace(temporaryPath, destinationPath, null);
            }
            catch (PlatformNotSupportedException exception)
            {
                throw new InvalidOperationException(
                    "GameSound could not safely replace the existing AudioClip on this platform. The previous clip was preserved.",
                    exception);
            }
            catch (IOException exception)
            {
                throw new InvalidOperationException(
                    "GameSound could not atomically replace the existing AudioClip. The previous clip was preserved.",
                    exception);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path);
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
                var bufferedHandler = request.downloadHandler as DownloadHandlerBuffer;
                var body = bufferedHandler != null ? SanitizeResponseBody(bufferedHandler.text) : string.Empty;
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

        private static string ResolvePackageVersion()
        {
            try
            {
                return PackageInfo.FindForAssembly(typeof(GameSoundApiClient).Assembly)?.version ?? "development";
            }
            catch
            {
                return "development";
            }
        }

        [Serializable]
        private sealed class EmptyJson { }
    }
}
