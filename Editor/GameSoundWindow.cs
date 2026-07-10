using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameSound.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSound.Unity.Editor
{
    public sealed class GameSoundWindow : EditorWindow
    {
        private static readonly string[] SourceFilters = { "All", "Library", "AI", "Other" };
        private const double AutoRefreshIntervalSeconds = 20.0;

        private GameSoundProjectDto[] projects = Array.Empty<GameSoundProjectDto>();
        private GameSoundManifestItemDto[] items = Array.Empty<GameSoundManifestItemDto>();
        private int selectedProjectIndex;
        private int selectedSourceFilter;
        private Vector2 scroll;
        private string status = "Not connected";
        private string currentManifestVersion = string.Empty;
        private bool busy;
        private bool showFallbackLoginCode;
        private string searchQuery = string.Empty;
        private DeviceStartResponse pendingLogin;
        private double nextAutoRefreshAt;
        private bool autoRefreshInFlight;
        private readonly Dictionary<string, bool> folderFoldouts = new Dictionary<string, bool>();

        private GUIStyle heroTitleStyle;
        private GUIStyle heroSubtitleStyle;
        private GUIStyle sectionTitleStyle;
        private GUIStyle cardStyle;
        private GUIStyle nestedCardStyle;
        private GUIStyle mutedLabelStyle;
        private GUIStyle itemTitleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle statusTextStyle;
        private GUIStyle wrapLabelStyle;

        [MenuItem("Window/GameSound")]
        public static void Open()
        {
            GetWindow<GameSoundWindow>("GameSound");
        }

        private void OnEnable()
        {
            minSize = new Vector2(560, 620);
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
            ResetAutoRefreshTimer(3.0);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHero();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawConnectionSection();
            DrawProjectSection();
            DrawSoundSection();
            EditorGUILayout.EndScrollView();

            DrawFooterStatus();
        }

        private void DrawHero()
        {
            var rect = GUILayoutUtility.GetRect(0, 74, GUILayout.ExpandWidth(true));
            var background = new Color(0.12f, 0.13f, 0.16f);
            var accent = new Color(0.16f, 0.68f, 0.86f);

            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3, rect.width, 3), accent);

            GUI.Label(new Rect(rect.x + 18, rect.y + 12, rect.width - 170, 30), "GameSound", heroTitleStyle);
            GUI.Label(
                new Rect(rect.x + 19, rect.y + 43, rect.width - 180, 22),
                "Import project audio and create Unity AudioSource emitters.",
                heroSubtitleStyle);

            DrawBadge(
                new Rect(rect.xMax - 138, rect.y + 20, 118, 24),
                busy ? "WORKING" : IsConnected ? "CONNECTED" : "OFFLINE",
                busy ? new Color(0.95f, 0.63f, 0.18f) : IsConnected ? new Color(0.13f, 0.74f, 0.47f) : new Color(0.58f, 0.62f, 0.70f));
        }

        private void DrawConnectionSection()
        {
            BeginCard("1. Connection", "Log in with your GameSound account and choose where imported audio assets are stored.");

            DrawFixedApiHostRow();
            DrawImportRootRow();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(busy))
                {
                    if (DrawTintedButton("Login in Browser", new Color(0.12f, 0.72f, 0.58f), GUILayout.Height(34)))
                    {
                        _ = StartBrowserLoginAsync();
                    }

                    if (GUILayout.Button("Load Projects", GUILayout.Height(34), GUILayout.Width(120)))
                    {
                        _ = LoadProjectsAsync();
                    }
                }

                if (GUILayout.Button("Logout", GUILayout.Height(34), GUILayout.Width(88)))
                {
                    _ = LogoutAsync();
                }
            }

            if (pendingLogin != null)
            {
                EditorGUILayout.Space(8);
                using (new EditorGUILayout.VerticalScope(nestedCardStyle))
                {
                    EditorGUILayout.LabelField("Browser approval pending", itemTitleStyle);
                    EditorGUILayout.LabelField("No copy/paste is needed. Sign in on the opened GameSound page, click Approve, and this Unity window will connect automatically.", wrapLabelStyle);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Reopen Approval Page"))
                        {
                            Application.OpenURL(pendingLogin.verificationUriComplete);
                        }
                        if (GUILayout.Button(showFallbackLoginCode ? "Hide Fallback Code" : "Show Fallback Code"))
                        {
                            showFallbackLoginCode = !showFallbackLoginCode;
                        }
                    }

                    if (showFallbackLoginCode)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Use this only if the browser page did not receive the code automatically.", mutedLabelStyle);
                        EditorGUILayout.SelectableLabel(pendingLogin.userCode, EditorStyles.textField, GUILayout.Height(20));
                        if (GUILayout.Button("Copy Fallback Code"))
                        {
                            EditorGUIUtility.systemCopyBuffer = pendingLogin.userCode;
                            status = "Copied fallback login code";
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);
            DrawInlineStatus();

            EndCard();
        }

        private void DrawProjectSection()
        {
            BeginCard("2. Projects", "Pick a GameSound project. Unity refreshes imported sounds directly from GameSound.");

            if (!IsConnected)
            {
                DrawEmptyState("Login first to see your GameSound projects.");
                EndCard();
                return;
            }

            if (projects.Length == 0)
            {
                DrawEmptyState("No projects loaded yet.");
                using (new EditorGUI.DisabledScope(busy))
                {
                    if (GUILayout.Button("Load Projects", GUILayout.Height(30)))
                    {
                        _ = LoadProjectsAsync();
                    }
                }
                EndCard();
                return;
            }

            var names = new string[projects.Length];
            for (var i = 0; i < projects.Length; i++)
            {
                var context = string.Equals(projects[i].contextType, "organization", StringComparison.OrdinalIgnoreCase) ? "Org" : "Personal";
                var access = projects[i].isShared ? (string.IsNullOrWhiteSpace(projects[i].role) ? "Shared" : projects[i].role) : "Owner";
                names[i] = $"{projects[i].name}  • {context} • {access}";
            }

            var previousProjectIndex = selectedProjectIndex;
            selectedProjectIndex = Mathf.Clamp(EditorGUILayout.Popup("Project", selectedProjectIndex, names), 0, projects.Length - 1);
            if (selectedProjectIndex != previousProjectIndex)
            {
                items = Array.Empty<GameSoundManifestItemDto>();
                currentManifestVersion = string.Empty;
                ResetAutoRefreshTimer(1.0);
            }
            var project = CurrentProject;
            if (project != null)
            {
                using (new EditorGUILayout.VerticalScope(nestedCardStyle))
                {
                    EditorGUILayout.LabelField(project.name, itemTitleStyle);
                    if (!string.IsNullOrWhiteSpace(project.description))
                    {
                        EditorGUILayout.LabelField(project.description, wrapLabelStyle);
                    }
                    var context = string.Equals(project.contextType, "organization", StringComparison.OrdinalIgnoreCase) ? "Organization" : "Personal";
                    var role = string.IsNullOrWhiteSpace(project.role) ? (project.isShared ? "Shared" : "Owner") : project.role;
                    EditorGUILayout.LabelField("Context", context);
                    EditorGUILayout.LabelField("Access", role);
                    if (!string.IsNullOrWhiteSpace(project.updatedAt))
                    {
                        EditorGUILayout.LabelField("Updated", project.updatedAt);
                    }
                    if (!string.IsNullOrWhiteSpace(currentManifestVersion))
                    {
                        EditorGUILayout.LabelField("Manifest", currentManifestVersion);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(busy || project == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (DrawTintedButton("Refresh from GameSound", new Color(0.22f, 0.55f, 0.95f), GUILayout.Height(32)))
                    {
                        _ = RefreshFromGameSoundAsync();
                    }

                    DrawAutoRefreshToggle();
                }
            }

            EndCard();
        }

        private void DrawSoundSection()
        {
            BeginCard("3. Sound Browser", "Search, preview, import, create emitters, or drag sounds into the Scene view.");

            if (items.Length == 0)
            {
                DrawEmptyState("Select a project and refresh from GameSound.");
                EndCard();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{items.Length} sound(s)", mutedLabelStyle, GUILayout.Width(92));
                searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.MinWidth(120));
                selectedSourceFilter = EditorGUILayout.Popup(selectedSourceFilter, SourceFilters, GUILayout.Width(96));

                using (new EditorGUI.DisabledScope(busy))
                {
                    if (DrawTintedButton("Update Imported", new Color(0.12f, 0.72f, 0.58f), GUILayout.Width(132), GUILayout.Height(22)))
                    {
                        _ = UpdateImportedAsync();
                    }
                }
            }

            EditorGUILayout.Space(8);
            var importedVersionIndex = GameSoundImporter.BuildImportedVersionIndex();
            var folderOrder = new List<string>();
            var grouped = new Dictionary<string, List<GameSoundManifestItemDto>>();
            foreach (var item in items)
            {
                if (!MatchesSoundFilter(item)) continue;
                var folder = NormalizeFolderLabel(item.folderPath);
                if (!grouped.TryGetValue(folder, out var list))
                {
                    list = new List<GameSoundManifestItemDto>();
                    grouped[folder] = list;
                    folderOrder.Add(folder);
                }
                list.Add(item);
            }

            if (folderOrder.Count == 0)
            {
                DrawEmptyState("No sounds match this search/filter.");
                EndCard();
                return;
            }

            foreach (var folder in folderOrder)
            {
                if (!folderFoldouts.ContainsKey(folder)) folderFoldouts[folder] = true;
                folderFoldouts[folder] = EditorGUILayout.Foldout(folderFoldouts[folder], $"{folder}  ({grouped[folder].Count})", true);
                if (!folderFoldouts[folder]) continue;

                EditorGUI.indentLevel++;
                foreach (var item in grouped[folder])
                {
                    DrawSoundItem(item, importedVersionIndex);
                }
                EditorGUI.indentLevel--;
            }

            EndCard();
        }

        private void DrawSoundItem(GameSoundManifestItemDto item, Dictionary<string, string> importedVersionIndex)
        {
            using (new EditorGUILayout.VerticalScope(nestedCardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var dragRect = GUILayoutUtility.GetRect(22, 20, GUILayout.Width(22));
                    GUI.Label(dragRect, "⇱", mutedLabelStyle);
                    HandleSoundItemDrag(dragRect, item);

                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(item.title) ? "Untitled sound" : item.title, itemTitleStyle);
                    GUILayout.FlexibleSpace();
                    if (GameSoundImporter.IsImportedAndCurrent(item, importedVersionIndex))
                    {
                        DrawBadge(GUILayoutUtility.GetRect(70, 20, GUILayout.Width(70)), "CURRENT", new Color(0.13f, 0.74f, 0.47f));
                    }
                    DrawSourcePill(item.source);
                }

                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Folder", mutedLabelStyle, GUILayout.Width(52));
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(item.folderPath) ? "/" : item.folderPath, GUILayout.MinWidth(120));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(FormatDuration(item.duration), mutedLabelStyle, GUILayout.Width(74));
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(item.format) ? "audio" : item.format.ToUpperInvariant(), mutedLabelStyle, GUILayout.Width(52));
                }

                EditorGUILayout.LabelField("Drag the ⇱ handle into the Scene view to create a GameSound Event Emitter.", mutedLabelStyle);

                using (new EditorGUI.DisabledScope(busy))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Preview", GUILayout.Height(28)))
                        {
                            _ = PreviewInBrowserAsync(item);
                        }
                        if (GUILayout.Button("Import / Update", GUILayout.Height(28)))
                        {
                            _ = ImportOneAsync(item);
                        }
                        if (DrawTintedButton("Create Emitter", new Color(0.22f, 0.55f, 0.95f), GUILayout.Height(28)))
                        {
                            _ = CreateAudioSourceAsync(item);
                        }
                    }
                }
            }
        }

        private void HandleSoundItemDrag(Rect dragRect, GameSoundManifestItemDto item)
        {
            var evt = Event.current;
            if (evt == null || item == null) return;
            if (evt.type != EventType.MouseDown || !dragRect.Contains(evt.mousePosition)) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("GameSound.ManifestItem", item);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag(string.IsNullOrWhiteSpace(item.title) ? "GameSound sound" : item.title);
            evt.Use();
        }

        private static string NormalizeFolderLabel(string folderPath)
        {
            return string.IsNullOrWhiteSpace(folderPath) ? "/" : folderPath;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var evt = Event.current;
            if (evt == null) return;

            var item = DragAndDrop.GetGenericData("GameSound.ManifestItem") as GameSoundManifestItemDto;
            if (item == null || CurrentProject == null || busy) return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var position = GetSceneDropPosition(evt.mousePosition, sceneView);
                _ = CreateAudioSourceAsync(item, position);
            }
            evt.Use();
        }

        private static Vector3 GetSceneDropPosition(Vector2 guiPosition, SceneView sceneView)
        {
            var ray = HandleUtility.GUIPointToWorldRay(guiPosition);
            if (Physics.Raycast(ray, out var hit, 5000f))
            {
                return hit.point;
            }

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out var distance))
            {
                return ray.GetPoint(distance);
            }

            return sceneView != null ? sceneView.pivot : Vector3.zero;
        }

        private void DrawFixedApiHostRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("API Host", GUILayout.Width(92));
                EditorGUILayout.SelectableLabel(GameSoundEditorPrefs.ApiBaseUrl, EditorStyles.textField, GUILayout.Height(18));
            }
        }

        private void DrawImportRootRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Import Root", GUILayout.Width(92));
                GameSoundEditorPrefs.ImportRoot = EditorGUILayout.TextField(GameSoundEditorPrefs.ImportRoot);
                if (GUILayout.Button("Reset", GUILayout.Width(76)))
                {
                    GameSoundEditorPrefs.ImportRoot = "Assets/GameSound";
                }
            }
        }

        private void DrawInlineStatus()
        {
            using (new EditorGUILayout.VerticalScope(nestedCardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSmallDot(IsConnected ? new Color(0.13f, 0.74f, 0.47f) : new Color(0.95f, 0.63f, 0.18f));
                    EditorGUILayout.LabelField(IsConnected ? "Connected" : "Login required", itemTitleStyle, GUILayout.Width(120));
                    EditorGUILayout.LabelField(status, statusTextStyle);
                }
            }
        }

        private void DrawAutoRefreshToggle()
        {
            var enabled = GameSoundEditorPrefs.AutoRefreshEnabled;
            var next = GUILayout.Toggle(enabled, "Auto Refresh", GUILayout.Width(112), GUILayout.Height(32));
            if (next == enabled) return;

            GameSoundEditorPrefs.AutoRefreshEnabled = next;
            status = next
                ? "Auto Refresh enabled. Unity will update imported sounds while this window is open."
                : "Auto Refresh disabled.";
            ResetAutoRefreshTimer(1.0);
        }

        private void DrawFooterStatus()
        {
            var rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.13f) : new Color(0.86f, 0.88f, 0.90f));
            var text = busy ? "Working..." : status;
            GUI.Label(new Rect(rect.x + 12, rect.y + 6, rect.width - 24, 18), text, mutedLabelStyle);
        }

        private void BeginCard(string title, string subtitle)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField(title, sectionTitleStyle);
            EditorGUILayout.LabelField(subtitle, wrapLabelStyle);
            EditorGUILayout.Space(8);
        }

        private static void EndCard()
        {
            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyState(string message)
        {
            using (new EditorGUILayout.VerticalScope(nestedCardStyle))
            {
                EditorGUILayout.LabelField(message, wrapLabelStyle);
            }
        }

        private void DrawSourcePill(string source)
        {
            var label = string.IsNullOrWhiteSpace(source) ? "OTHER" : source.Trim().ToUpperInvariant();
            var rect = GUILayoutUtility.GetRect(new GUIContent(label), badgeStyle, GUILayout.Width(74), GUILayout.Height(20));
            var color = IsAiSource(source) ? new Color(0.55f, 0.34f, 0.94f) : IsLibrarySource(source) ? new Color(0.13f, 0.65f, 0.84f) : new Color(0.50f, 0.54f, 0.62f);
            DrawBadge(rect, label, color);
        }

        private void DrawBadge(Rect rect, string label, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, label, badgeStyle);
        }

        private static void DrawSmallDot(Color color)
        {
            var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(16), GUILayout.Height(18));
            EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 4, 10, 10), color);
        }

        private static bool DrawTintedButton(string label, Color color, params GUILayoutOption[] options)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            var clicked = GUILayout.Button(label, options);
            GUI.backgroundColor = previous;
            return clicked;
        }

        private bool MatchesSoundFilter(GameSoundManifestItemDto item)
        {
            if (selectedSourceFilter == 1 && !IsLibrarySource(item.source)) return false;
            if (selectedSourceFilter == 2 && !IsAiSource(item.source)) return false;
            if (selectedSourceFilter == 3 && (IsLibrarySource(item.source) || IsAiSource(item.source))) return false;

            if (string.IsNullOrWhiteSpace(searchQuery)) return true;
            var query = searchQuery.Trim();
            return Contains(item.title, query) || Contains(item.folderPath, query) || Contains(item.source, query) || Contains(item.format, query);
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLibrarySource(string source)
        {
            return string.Equals(source, "library", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAiSource(string source)
        {
            return string.Equals(source, "ai", StringComparison.OrdinalIgnoreCase) || string.Equals(source, "generated", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return "Unknown";
            var time = TimeSpan.FromSeconds(seconds);
            return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }

        private async Task StartBrowserLoginAsync()
        {
            await RunBusyAsync(async () =>
            {
                var api = CreateApi();
                pendingLogin = await api.StartDeviceLoginAsync();
                showFallbackLoginCode = false;
                status = "Opened browser. Approve GameSound connection there to continue.";
                Application.OpenURL(pendingLogin.verificationUriComplete);
                Repaint();

                var interval = Mathf.Clamp(pendingLogin.interval, 2, 10);
                var deadline = DateTimeOffset.UtcNow.AddSeconds(Mathf.Max(60, pendingLogin.expiresIn));
                while (DateTimeOffset.UtcNow < deadline)
                {
                    await Task.Delay(interval * 1000);
                    var poll = await api.PollDeviceLoginAsync(pendingLogin.deviceCode);
                    if (string.Equals(poll.status, "approved", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(poll.accessToken))
                        {
                            throw new InvalidOperationException("GameSound login did not return an access token.");
                        }
                        GameSoundEditorPrefs.SaveAccessToken(poll.accessToken, poll.expiresIn);
                        pendingLogin = null;
                        showFallbackLoginCode = false;
                        status = "Connected to GameSound";
                        await LoadProjectsAsync(false);
                        return;
                    }
                    status = "Waiting for browser approval...";
                    Repaint();
                }

                pendingLogin = null;
                showFallbackLoginCode = false;
                status = "Login timed out. Click Login in Browser to try again.";
            });
        }

        private Task LoadProjectsAsync(bool wrapBusy = true)
        {
            return RunMaybeBusyAsync(wrapBusy, async () =>
            {
                var api = CreateApi();
                var response = await api.GetProjectsAsync();
                projects = response.projects ?? Array.Empty<GameSoundProjectDto>();
                selectedProjectIndex = Mathf.Clamp(selectedProjectIndex, 0, Math.Max(0, projects.Length - 1));
                status = $"Loaded {projects.Length} project(s)";
            });
        }

        private Task LogoutAsync()
        {
            return RunBusyAsync(async () =>
            {
                var hadToken = !string.IsNullOrWhiteSpace(GameSoundEditorPrefs.AccessToken);
                if (hadToken)
                {
                    try
                    {
                        await CreateApi().LogoutAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"GameSound server logout failed; clearing local session anyway: {ex.Message}");
                    }
                }

                ClearLocalSession();
                status = hadToken ? "Logged out and revoked this Unity session" : "Logged out";
            });
        }

        private void ClearLocalSession()
        {
            GameSoundEditorPrefs.ClearTokens();
            projects = Array.Empty<GameSoundProjectDto>();
            items = Array.Empty<GameSoundManifestItemDto>();
            currentManifestVersion = string.Empty;
            pendingLogin = null;
            showFallbackLoginCode = false;
        }

        private Task RefreshFromGameSoundAsync()
        {
            return RunBusyAsync(async () =>
            {
                var project = CurrentProject;
                if (project == null) return;
                var api = CreateApi();
                await LoadManifestInternalAsync(api, project);
                var count = await SyncItemsInternalAsync(api, project, items, onlyImported: true);
                status = count > 0
                    ? $"Updated {count} imported sound(s) from GameSound"
                    : $"Loaded latest manifest from {project.name}";
                ResetAutoRefreshTimer();
            });
        }

        private Task ImportOneAsync(GameSoundManifestItemDto item)
        {
            return RunBusyAsync(async () =>
            {
                var asset = await ImportOneInternalAsync(CreateApi(), CurrentProject, item);
                Selection.activeObject = asset;
                status = $"Imported {item.title}";
            });
        }

        private Task PreviewInBrowserAsync(GameSoundManifestItemDto item)
        {
            return RunBusyAsync(async () =>
            {
                var project = CurrentProject;
                if (project == null) return;
                var download = await CreateApi().CreateDownloadAsync(project.id, item.soundId);
                Application.OpenURL(download.url);
                status = $"Opened preview URL for {item.title}";
            });
        }

        private Task CreateAudioSourceAsync(GameSoundManifestItemDto item, Vector3? scenePosition = null)
        {
            return RunBusyAsync(async () =>
            {
                await CreateAudioSourceInternalAsync(CreateApi(), CurrentProject, item, scenePosition);
                status = $"Created GameSound emitter for {item.title}";
            });
        }

        private Task UpdateImportedAsync()
        {
            return RunBusyAsync(async () =>
            {
                var api = CreateApi();
                var project = CurrentProject;
                if (project == null) return;
                if (items.Length == 0)
                {
                    await LoadManifestInternalAsync(api, project);
                }
                var count = await SyncItemsInternalAsync(api, project, items, onlyImported: true);
                status = count > 0
                    ? $"Updated {count} imported sound(s)"
                    : "Imported sounds are already current";
                ResetAutoRefreshTimer();
            });
        }

        private async Task LoadManifestInternalAsync(GameSoundApiClient api, GameSoundProjectDto project, bool quiet = false)
        {
            if (project == null) return;
            var response = await api.GetManifestAsync(project.id);
            items = response.items ?? Array.Empty<GameSoundManifestItemDto>();
            currentManifestVersion = response.manifestVersion ?? string.Empty;
            if (!quiet)
            {
                status = $"Loaded {items.Length} sound(s) from {project.name}";
            }
        }

        private async Task<GameSoundAsset> ImportOneInternalAsync(GameSoundApiClient api, GameSoundProjectDto project, GameSoundManifestItemDto item)
        {
            if (project == null) throw new InvalidOperationException("Select a project first.");
            if (item == null) throw new ArgumentNullException(nameof(item));
            var asset = await GameSoundImporter.ImportAsync(api, project, item, GameSoundEditorPrefs.ImportRoot);
            Selection.activeObject = asset;
            return asset;
        }

        private async Task<int> SyncItemsInternalAsync(
            GameSoundApiClient api,
            GameSoundProjectDto project,
            GameSoundManifestItemDto[] manifestItems,
            bool onlyImported = false,
            bool quiet = false)
        {
            if (project == null) throw new InvalidOperationException("Select a project first.");
            var count = 0;
            var skipped = 0;
            var importedVersionIndex = GameSoundImporter.BuildImportedVersionIndex();
            var syncItems = manifestItems ?? Array.Empty<GameSoundManifestItemDto>();
            foreach (var item in syncItems)
            {
                if (onlyImported && !GameSoundImporter.IsImported(item, importedVersionIndex))
                {
                    continue;
                }

                if (GameSoundImporter.IsImportedAndCurrent(item, importedVersionIndex))
                {
                    if (GameSoundImporter.RefreshMetadataIfImported(project, item))
                    {
                        count++;
                        if (!quiet)
                        {
                            status = $"Updated metadata {count}: {item.title}";
                            Repaint();
                        }
                    }
                    else
                    {
                        skipped++;
                        if (!quiet)
                        {
                            status = $"Skipped current {skipped}, updated {count}: {item.title}";
                            Repaint();
                        }
                    }
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.versionHash) && GameSoundImporter.IsImported(item, importedVersionIndex))
                {
                    if (GameSoundImporter.RefreshMetadataIfImported(project, item))
                    {
                        count++;
                        if (!quiet)
                        {
                            status = $"Updated metadata {count}: {item.title}";
                            Repaint();
                        }
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                await GameSoundImporter.ImportAsync(api, project, item, GameSoundEditorPrefs.ImportRoot);
                count++;
                if (!quiet)
                {
                    status = $"Updated changed {count}/{syncItems.Length}: {item.title}";
                    Repaint();
                }
            }
            return count;
        }

        private async Task<GameSoundAudioSource> CreateAudioSourceInternalAsync(GameSoundApiClient api, GameSoundProjectDto project, GameSoundManifestItemDto item, Vector3? scenePosition = null)
        {
            var asset = await ImportOneInternalAsync(api, project, item);
            var go = new GameObject(string.IsNullOrWhiteSpace(item.title) ? "GameSound Emitter" : item.title);
            if (scenePosition.HasValue) go.transform.position = scenePosition.Value;

            var component = go.AddComponent<GameSoundEventEmitter>();
            component.ApplyRemoteAsset(asset);
            ApplyManifestUnitySettings(component, item);
            component.ApplyToAudioSource();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create GameSound Event Emitter");
            return component;
        }

        private static void ApplyManifestUnitySettings(GameSoundAudioSource component, GameSoundManifestItemDto item)
        {
            if (component == null || item?.unity == null) return;

            var settings = item.unity;
            component.Loop = settings.loop;
            component.Volume = settings.volume;
            component.SpatialBlend = settings.spatialBlend;
            component.MinDistance = settings.minDistance;
            component.MaxDistance = settings.maxDistance;

            var hasPitchRange = !Mathf.Approximately(settings.randomPitchMin, 0f) || !Mathf.Approximately(settings.randomPitchMax, 0f);
            component.RandomizePitch = hasPitchRange;
            if (hasPitchRange)
            {
                var minPitch = Mathf.Clamp(settings.randomPitchMin, 0.01f, 3f);
                var maxPitch = Mathf.Clamp(settings.randomPitchMax, 0.01f, 3f);
                if (Mathf.Approximately(settings.randomPitchMin, 0f)) minPitch = 1f;
                if (Mathf.Approximately(settings.randomPitchMax, 0f)) maxPitch = minPitch;
                if (maxPitch < minPitch) maxPitch = minPitch;
                component.RandomPitchMin = minPitch;
                component.RandomPitchMax = maxPitch;
            }
        }


        private void OnEditorUpdate()
        {
            if (!GameSoundEditorPrefs.AutoRefreshEnabled) return;
            if (!IsConnected || CurrentProject == null || busy || autoRefreshInFlight || pendingLogin != null) return;

            var now = EditorApplication.timeSinceStartup;
            if (now < nextAutoRefreshAt) return;
            ResetAutoRefreshTimer();
            _ = AutoRefreshFromGameSoundAsync();
        }

        private async Task AutoRefreshFromGameSoundAsync()
        {
            if (autoRefreshInFlight) return;
            autoRefreshInFlight = true;
            try
            {
                var project = CurrentProject;
                if (project == null) return;
                var api = CreateApi();
                await LoadManifestInternalAsync(api, project, quiet: true);
                var count = await SyncItemsInternalAsync(api, project, items, onlyImported: true, quiet: true);
                if (count > 0)
                {
                    status = $"Auto Refresh updated {count} imported sound(s)";
                    Repaint();
                }
            }
            catch (Exception ex)
            {
                status = $"Auto Refresh failed: {ex.Message}";
                Debug.LogWarning($"GameSound Auto Refresh failed: {ex.Message}");
                Repaint();
            }
            finally
            {
                autoRefreshInFlight = false;
            }
        }

        private void ResetAutoRefreshTimer(double delaySeconds = AutoRefreshIntervalSeconds)
        {
            nextAutoRefreshAt = EditorApplication.timeSinceStartup + Math.Max(1.0, delaySeconds);
        }

        private bool IsConnected => !string.IsNullOrWhiteSpace(GameSoundEditorPrefs.AccessToken);

        private GameSoundProjectDto CurrentProject => projects.Length == 0 ? null : projects[Mathf.Clamp(selectedProjectIndex, 0, projects.Length - 1)];

        private GameSoundApiClient CreateApi()
        {
            return new GameSoundApiClient(GameSoundEditorPrefs.ApiBaseUrl, GameSoundEditorPrefs.AccessToken);
        }

        private Task RunBusyAsync(Func<Task> action)
        {
            return RunMaybeBusyAsync(true, action);
        }

        private async Task RunMaybeBusyAsync(bool wrapBusy, Func<Task> action)
        {
            if (busy && wrapBusy) return;
            try
            {
                if (wrapBusy) busy = true;
                status = "Working...";
                Repaint();
                await action();
            }
            catch (Exception ex)
            {
                status = ex.Message;
                Debug.LogException(ex);
            }
            finally
            {
                if (wrapBusy) busy = false;
                Repaint();
            }
        }

        private void EnsureStyles()
        {
            if (cardStyle != null) return;

            cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(10, 10, 8, 4)
            };

            nestedCardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 6)
            };

            heroTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 26,
                normal = { textColor = Color.white }
            };

            heroSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.96f, 0.94f) }
            };

            sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15
            };

            itemTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            mutedLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };

            statusTextStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            wrapLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            badgeStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Color.white }
            };
        }
    }
}
