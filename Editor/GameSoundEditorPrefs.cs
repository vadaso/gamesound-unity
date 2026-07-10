using System;
using System.IO;
using UnityEditor;

namespace GameSound.Unity.Editor
{
    internal static class GameSoundEditorPrefs
    {
        public const string DefaultImportRoot = "Assets/GameSound";

        private const string LegacyBaseUrlKey = "GameSound.Unity.ApiBaseUrl";
        private const string AccessTokenKey = "GameSound.Unity.AccessToken";
        private const string AccessTokenExpiresAtKey = "GameSound.Unity.AccessTokenExpiresAt";
        private const string LegacyRefreshTokenKey = "GameSound.Unity.RefreshToken";
        private const string ImportRootKey = "GameSound.Unity.ImportRoot";
        private const string AutoRefreshKey = "GameSound.Unity.AutoRefresh";

        static GameSoundEditorPrefs()
        {
            // Versions before 0.3.8 persisted credentials and an editable API origin in
            // global EditorPrefs. Production packages now use a fixed origin and keep the
            // access token only for the lifetime of the current Unity editor session.
            var legacyToken = EditorPrefs.GetString(AccessTokenKey, string.Empty);
            var legacyExpiry = EditorPrefs.GetString(AccessTokenExpiresAtKey, string.Empty);
            if (string.IsNullOrWhiteSpace(SessionState.GetString(AccessTokenKey, string.Empty)) &&
                !string.IsNullOrWhiteSpace(legacyToken) &&
                long.TryParse(legacyExpiry, out var expiresAt) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expiresAt)
            {
                SessionState.SetString(AccessTokenKey, legacyToken);
                SessionState.SetString(AccessTokenExpiresAtKey, legacyExpiry);
            }

            EditorPrefs.DeleteKey(LegacyBaseUrlKey);
            EditorPrefs.DeleteKey(AccessTokenKey);
            EditorPrefs.DeleteKey(AccessTokenExpiresAtKey);
            EditorPrefs.DeleteKey(LegacyRefreshTokenKey);
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
                return SessionState.GetString(AccessTokenKey, string.Empty);
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    SessionState.EraseString(AccessTokenKey);
                    SessionState.EraseString(AccessTokenExpiresAtKey);
                    return;
                }

                SessionState.SetString(AccessTokenKey, value);
            }
        }

        public static void SaveAccessToken(string token, int expiresInSeconds)
        {
            AccessToken = token;
            var ttl = Math.Max(60, expiresInSeconds);
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(ttl).ToUnixTimeSeconds();
            SessionState.SetString(AccessTokenExpiresAtKey, expiresAt.ToString());
        }

        public static string ImportRoot
        {
            get
            {
                var saved = EditorPrefs.GetString(ImportRootKey, DefaultImportRoot);
                var normalized = NormalizeImportRoot(saved);
                if (!string.Equals(saved, normalized, StringComparison.Ordinal))
                {
                    EditorPrefs.SetString(ImportRootKey, normalized);
                }
                return normalized;
            }
            set => EditorPrefs.SetString(ImportRootKey, NormalizeImportRoot(value));
        }

        public static bool AutoRefreshEnabled
        {
            get => EditorPrefs.GetBool(AutoRefreshKey, false);
            set => EditorPrefs.SetBool(AutoRefreshKey, value);
        }

        public static void ClearTokens()
        {
            AccessToken = string.Empty;
            SessionState.EraseString(AccessTokenExpiresAtKey);
            EditorPrefs.DeleteKey(AccessTokenKey);
            EditorPrefs.DeleteKey(AccessTokenExpiresAtKey);
            EditorPrefs.DeleteKey(LegacyRefreshTokenKey);
        }

        public static string NormalizeImportRoot(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DefaultImportRoot;

            var candidate = value.Trim().Replace('\\', '/').TrimEnd('/');
            while (candidate.Contains("//")) candidate = candidate.Replace("//", "/");

            if (Path.IsPathRooted(candidate) ||
                (candidate.Length >= 2 && char.IsLetter(candidate[0]) && candidate[1] == ':') ||
                (!string.Equals(candidate, "Assets", StringComparison.Ordinal) &&
                 !candidate.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                return DefaultImportRoot;
            }

            var segments = candidate.Split('/');
            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment) ||
                    segment == "." ||
                    segment == ".." ||
                    ContainsInvalidPathCharacter(segment) ||
                    HasInvalidWindowsPathEnding(segment) ||
                    IsWindowsReservedPathSegment(segment))
                {
                    return DefaultImportRoot;
                }
            }

            return candidate;
        }

        private static bool ContainsInvalidPathCharacter(string value)
        {
            foreach (var character in value)
            {
                if (character < 32 || "<>:\"|?*".IndexOf(character) >= 0) return true;
            }
            return false;
        }

        internal static bool IsWindowsReservedPathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            var name = value.Trim();
            var extensionIndex = name.IndexOf('.');
            if (extensionIndex >= 0) name = name.Substring(0, extensionIndex);
            name = name.TrimEnd(' ', '.').ToUpperInvariant();

            if (name == "CON" || name == "PRN" || name == "AUX" || name == "NUL") return true;
            if (name.Length == 4 && (name.StartsWith("COM", StringComparison.Ordinal) || name.StartsWith("LPT", StringComparison.Ordinal)))
            {
                return name[3] >= '1' && name[3] <= '9';
            }

            return false;
        }

        private static bool HasInvalidWindowsPathEnding(string value)
        {
            return value.EndsWith(" ", StringComparison.Ordinal) || value.EndsWith(".", StringComparison.Ordinal);
        }

        private static bool IsAccessTokenExpired()
        {
            var expiresAtString = SessionState.GetString(AccessTokenExpiresAtKey, string.Empty);
            if (string.IsNullOrWhiteSpace(expiresAtString))
            {
                return !string.IsNullOrWhiteSpace(SessionState.GetString(AccessTokenKey, string.Empty));
            }
            if (!long.TryParse(expiresAtString, out var expiresAt)) return true;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiresAt;
        }
    }
}
