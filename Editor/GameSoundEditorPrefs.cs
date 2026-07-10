using UnityEditor;
using System;

namespace GameSound.Unity.Editor
{
    internal static class GameSoundEditorPrefs
    {
        private const string BaseUrlKey = "GameSound.Unity.ApiBaseUrl";
        private const string AccessTokenKey = "GameSound.Unity.AccessToken";
        private const string AccessTokenExpiresAtKey = "GameSound.Unity.AccessTokenExpiresAt";
        private const string RefreshTokenKey = "GameSound.Unity.RefreshToken";
        private const string ImportRootKey = "GameSound.Unity.ImportRoot";
        private const string AutoRefreshKey = "GameSound.Unity.AutoRefresh";
        private const string ProductionApiBaseUrl = "https://gamesound.ai";

        public static string ApiBaseUrl
        {
            get
            {
                // Production packages intentionally use one fixed API origin.
                // Older package versions exposed an editable/dev base URL; overwrite it
                // so public installs cannot accidentally authenticate against dev.
                if (NormalizeBaseUrl(EditorPrefs.GetString(BaseUrlKey, string.Empty)) != ProductionApiBaseUrl)
                {
                    EditorPrefs.SetString(BaseUrlKey, ProductionApiBaseUrl);
                }
                return ProductionApiBaseUrl;
            }
            set
            {
                EditorPrefs.SetString(BaseUrlKey, ProductionApiBaseUrl);
            }
        }

        public static string AccessToken
        {
            get
            {
                if (IsAccessTokenExpired())
                {
                    ClearTokens();
                    return string.Empty;
                }
                return EditorPrefs.GetString(AccessTokenKey, string.Empty);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    EditorPrefs.DeleteKey(AccessTokenKey);
                    EditorPrefs.DeleteKey(AccessTokenExpiresAtKey);
                    return;
                }

                EditorPrefs.SetString(AccessTokenKey, value);
            }
        }

        public static void SaveAccessToken(string token, int expiresInSeconds)
        {
            AccessToken = token;
            var ttl = Math.Max(60, expiresInSeconds);
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(ttl).ToUnixTimeSeconds();
            EditorPrefs.SetString(AccessTokenExpiresAtKey, expiresAt.ToString());
        }

        public static string ImportRoot
        {
            get => EditorPrefs.GetString(ImportRootKey, "Assets/GameSound");
            set => EditorPrefs.SetString(ImportRootKey, string.IsNullOrWhiteSpace(value) ? "Assets/GameSound" : value.Trim().TrimEnd('/'));
        }

        public static bool AutoRefreshEnabled
        {
            get => EditorPrefs.GetBool(AutoRefreshKey, true);
            set => EditorPrefs.SetBool(AutoRefreshKey, value);
        }

        public static void ClearTokens()
        {
            AccessToken = string.Empty;
            EditorPrefs.DeleteKey(AccessTokenExpiresAtKey);
            EditorPrefs.DeleteKey(RefreshTokenKey);
        }

        private static string NormalizeBaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return ProductionApiBaseUrl;
            return value.Trim().TrimEnd('/');
        }

        private static bool IsAccessTokenExpired()
        {
            var expiresAtString = EditorPrefs.GetString(AccessTokenExpiresAtKey, string.Empty);
            if (string.IsNullOrWhiteSpace(expiresAtString))
            {
                // Legacy packages did not record token expiry. Force a fresh browser login
                // rather than keeping an unknown-lifetime EditorPrefs token.
                return !string.IsNullOrWhiteSpace(EditorPrefs.GetString(AccessTokenKey, string.Empty));
            }
            if (!long.TryParse(expiresAtString, out var expiresAt)) return true;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiresAt;
        }
    }
}
