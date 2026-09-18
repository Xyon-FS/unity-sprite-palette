using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WhatIDoToday.SpritePalette.Editor
{
    public sealed class SpritePaletteWindow : EditorWindow
    {
        internal const string AllPaletteId = "__all";
        internal const string FavoritesPaletteId = "__favorites";
        internal const string RecentPaletteId = "__recent";

        private readonly List<Sprite> sprites = new();
        private readonly Dictionary<string, int> paletteCounts = new();

        private SpritePaletteUserPreferences Preferences => SpritePaletteUserPreferences.instance;
        private SpritePaletteProjectSettings ProjectSettings => SpritePaletteProjectSettings.instance;

        internal static SpritePaletteWindow ActivePlacementWindow { get; private set; }

        private Vector2 contentScroll;
        private Vector2 sidebarScroll;
        private Transform parent;
        private Sprite activeSprite;
        private Vector3 currentPlacementPosition;
        private GameObject previewObject;
        private SpriteRenderer previewRenderer;
        private Material defaultPreviewMaterial;

        private string selectedSortingLayer = "Default";
        private int sortingOrder;
        private OverlapOrderMode overlapOrderMode = OverlapOrderMode.BringForward;
        private Color spriteColor = Color.white;
        private Material spriteMaterial;
        private bool flipX;
        private bool flipY;
        private Vector2 objectScale = Vector2.one;
        private float rotationZ;
        private float positionZ;
        private string objectNameOverride = string.Empty;

        private GUIStyle spriteButtonStyle;
        private GUIStyle activeSpriteButtonStyle;
        private GUIStyle sidebarButtonStyle;
        private GUIStyle activeSidebarButtonStyle;

        [MenuItem("Tools/Sprite Palette")]
        private static void OpenWindow()
        {
            SpritePaletteWindow window = GetWindow<SpritePaletteWindow>("Sprite Palette");
            window.minSize = new Vector2(380f, 420f);
            window.Show();
            window.ScheduleInitialLoad();
        }

        private void OnEnable()
        {
            ProjectSettings.EnsureInitialized();
            EnsureValidPaletteSelection();
            SpritePaletteProjectSettings.Changed += OnProjectSettingsChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            ScheduleInitialLoad();
        }

        private void OnDisable()
        {
            EditorApplication.update -= CompleteInitialLoad;
            EditorApplication.update -= FocusSceneViewForPlacement;
            SavePreferences();
            SpritePaletteProjectSettings.Changed -= OnProjectSettingsChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            StopPlacement();
        }

        private void ScheduleInitialLoad()
        {
            EditorApplication.update -= CompleteInitialLoad;
            EditorApplication.update += CompleteInitialLoad;
        }

        private void CompleteInitialLoad()
        {
            EditorApplication.update -= CompleteInitialLoad;
            if (this == null)
                return;

            ProjectSettings.EnsureInitialized();
            EnsureValidPaletteSelection();
            LoadSprites();
        }

        private void OnDestroy()
        {
            StopPlacement();
        }

        private void EnsureStyles()
        {
            spriteButtonStyle ??= new GUIStyle(GUI.skin.button);
            activeSpriteButtonStyle ??= new GUIStyle("SelectionRect");
            sidebarButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 24f
            };
            activeSidebarButtonStyle ??= new GUIStyle(sidebarButtonStyle)
            {
                fontStyle = FontStyle.Bold
            };
        }

        private void OnProjectSettingsChanged()
        {
            EnsureValidPaletteSelection();
            LoadSprites();
            Repaint();
        }

        private void EnsureValidPaletteSelection()
        {
            string id = Preferences.activePaletteId;
            bool isBuiltIn = id == AllPaletteId || id == FavoritesPaletteId || id == RecentPaletteId;
            if (!isBuiltIn && ProjectSettings.FindPalette(id) == null)
                Preferences.activePaletteId = ProjectSettings.Palettes.Count > 0 ? ProjectSettings.Palettes[0].id : AllPaletteId;
        }

        private void OnGUI()
        {
            EnsureStyles();
            EditorGUI.BeginChangeCheck();
            DrawToolbar();

            bool compact = position.width < 620f;
            if (compact)
            {
                DrawCompactPaletteSelector();
                contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
                DrawMainContent();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(190f)))
                        DrawPaletteSidebar();

                    contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
                    using (new EditorGUILayout.VerticalScope())
                        DrawMainContent();
                    EditorGUILayout.EndScrollView();
                }
            }

            if (EditorGUI.EndChangeCheck())
                SavePreferences();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                    LoadSprites();

                if (GUILayout.Button("New Palette", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                    CreatePalette();

                SpritePaletteDefinition activePalette = GetActiveNamedPalette();
                using (new EditorGUI.DisabledScope(activePalette == null))
                {
                    if (GUILayout.Button("Edit", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                        SpritePaletteDefinitionEditorWindow.Open(activePalette);

                    if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                        DeleteActivePalette();
                }
            }
        }

        private void DrawCompactPaletteSelector()
        {
            List<string> ids = BuildPaletteIdList();
            string[] labels = ids.Select(GetPaletteDisplayName).ToArray();
            int current = Mathf.Max(0, ids.IndexOf(Preferences.activePaletteId));
            int selected = EditorGUILayout.Popup("Palette", current, labels);
            if (selected >= 0 && selected < ids.Count && ids[selected] != Preferences.activePaletteId)
                SelectPalette(ids[selected]);
        }

        private void DrawPaletteSidebar()
        {
            EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);
            sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll);

            DrawPaletteButton(AllPaletteId, "All Sprites");
            DrawPaletteButton(FavoritesPaletteId, "Favorites");
            DrawPaletteButton(RecentPaletteId, "Recent");

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("My Palettes", EditorStyles.boldLabel);
            foreach (SpritePaletteDefinition palette in ProjectSettings.Palettes)
            {
                if (palette != null)
                    DrawPaletteButton(palette.id, palette.name);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPaletteButton(string id, string label)
        {
            bool active = Preferences.activePaletteId == id;
            string count = paletteCounts.TryGetValue(id, out int value) ? value.ToString() : "0";
            if (GUILayout.Button($"{label}    {count}", active ? activeSidebarButtonStyle : sidebarButtonStyle))
                SelectPalette(id);
        }

        private void DrawMainContent()
        {
            SpritePaletteDefinition activePalette = GetActiveNamedPalette();
            EditorGUILayout.LabelField(GetPaletteDisplayName(Preferences.activePaletteId), EditorStyles.boldLabel);

            DrawSourceSection(activePalette);
            DrawRenderingSection();
            DrawTransformSection();
            DrawPlacementSection();
            DrawPresetSection();
            DrawPlacementStatus();
            EditorGUILayout.Space(6f);
            DrawSpriteBrowserControls();
            DrawSpriteGrid();
        }

        private void DrawSourceSection(SpritePaletteDefinition palette)
        {
            Preferences.showSource = EditorGUILayout.Foldout(Preferences.showSource, "Source and Target", true);
            if (!Preferences.showSource)
                return;

            EditorGUI.indentLevel++;
            if (palette != null)
            {
                string sourceText = palette.folderGuids.Count == 0 && palette.spriteKeys.Count == 0
                    ? "No sources configured"
                    : $"{palette.folderGuids.Count} folder(s), {palette.spriteKeys.Count} individual sprite(s)";
                EditorGUILayout.LabelField("Palette Sources", sourceText);
            }
            else
            {
                EditorGUILayout.LabelField("Palette Sources", "Combined or automatic collection");
            }

            parent = (Transform)EditorGUILayout.ObjectField("Parent Object", parent, typeof(Transform), true);
            EditorGUILayout.LabelField("Placement Target", GetPlacementTargetDescription(), EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }

        private void DrawRenderingSection()
        {
            Preferences.showRendering = EditorGUILayout.Foldout(Preferences.showRendering, "Rendering", true);
            if (!Preferences.showRendering)
                return;

            EditorGUI.indentLevel++;
            selectedSortingLayer = DrawSortingLayerPopup("Sorting Layer", selectedSortingLayer);
            sortingOrder = EditorGUILayout.IntField("Order in Layer", sortingOrder);
            overlapOrderMode = (OverlapOrderMode)EditorGUILayout.EnumPopup("Overlapping Auto Order", overlapOrderMode);
            spriteColor = EditorGUILayout.ColorField("Color", spriteColor);
            spriteMaterial = (Material)EditorGUILayout.ObjectField("Material", spriteMaterial, typeof(Material), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                flipX = EditorGUILayout.Toggle("Flip X", flipX);
                flipY = EditorGUILayout.Toggle("Flip Y", flipY);
            }
            EditorGUI.indentLevel--;
            UpdatePreviewObjectProperties();
        }

        private void DrawTransformSection()
        {
            Preferences.showTransform = EditorGUILayout.Foldout(Preferences.showTransform, "Transform", true);
            if (!Preferences.showTransform)
                return;

            EditorGUI.indentLevel++;
            objectScale = EditorGUILayout.Vector2Field("Scale", objectScale);
            rotationZ = EditorGUILayout.FloatField("Z Rotation", rotationZ);
            positionZ = EditorGUILayout.FloatField("Z Position", positionZ);
            objectNameOverride = EditorGUILayout.TextField("Object Name", objectNameOverride);
            EditorGUI.indentLevel--;
            UpdatePreviewTransform();
        }

        private void DrawPlacementSection()
        {
            Preferences.showPlacement = EditorGUILayout.Foldout(Preferences.showPlacement, "Placement", true);
            if (!Preferences.showPlacement)
                return;

            EditorGUI.indentLevel++;
            Preferences.snapToGrid = EditorGUILayout.Toggle("Grid Snap", Preferences.snapToGrid);
            using (new EditorGUI.DisabledScope(!Preferences.snapToGrid))
                Preferences.gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", Preferences.gridSize));

            Preferences.rotationHotkeyStep = Mathf.Max(0.1f, EditorGUILayout.FloatField("Rotation Step", Preferences.rotationHotkeyStep));
            Preferences.scaleHotkeyPercent = Mathf.Clamp(EditorGUILayout.FloatField("Resize Step (%)", Preferences.scaleHotkeyPercent), 0.1f, 100f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Properties"))
                    ResetPlacementProperties();
                if (GUILayout.Button("Copy From Selection"))
                    CopyPropertiesFromSelectedObject();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawPresetSection()
        {
            Preferences.showPreset = EditorGUILayout.Foldout(Preferences.showPreset, "Per-Sprite Preset", true);
            if (!Preferences.showPreset)
                return;

            EditorGUI.indentLevel++;
            Preferences.rememberPresetPerSprite = EditorGUILayout.Toggle("Load Presets", Preferences.rememberPresetPerSprite);
            Preferences.presetRememberScale = EditorGUILayout.Toggle("Remember Scale", Preferences.presetRememberScale);
            Preferences.presetRememberRotation = EditorGUILayout.Toggle("Remember Rotation", Preferences.presetRememberRotation);
            Preferences.presetRememberRendering = EditorGUILayout.Toggle("Remember Rendering", Preferences.presetRememberRendering);
            Preferences.presetRememberSorting = EditorGUILayout.Toggle("Remember Sorting", Preferences.presetRememberSorting);

            using (new EditorGUI.DisabledScope(activeSprite == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Preset"))
                    SavePresetForActiveSprite();
                if (GUILayout.Button("Forget Preset"))
                {
                    ProjectSettings.RemovePreset(SpritePaletteAssetUtility.GetSpriteKey(activeSprite));
                    Repaint();
                }
            }

            if (activeSprite != null)
            {
                string key = SpritePaletteAssetUtility.GetSpriteKey(activeSprite);
                EditorGUILayout.LabelField(
                    ProjectSettings.TryGetPreset(key, out _) ? "Saved preset available" : "No saved preset",
                    EditorStyles.miniLabel
                );
            }
            EditorGUI.indentLevel--;
        }

        private void DrawPlacementStatus()
        {
            if (activeSprite == null)
            {
                EditorGUILayout.HelpBox("Select a sprite to begin placement.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Active sprite: {activeSprite.name}\n" +
                $"Target: {GetPlacementTargetDescription()}\n" +
                "Left-click places a copy. Esc or right-click stops placement.",
                MessageType.None
            );

            if (GUILayout.Button("Stop Placement"))
                StopPlacement();
        }

        private void DrawSpriteBrowserControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Sprite Browser", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    Preferences.searchText = EditorGUILayout.TextField("Search", Preferences.searchText ?? string.Empty);
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(Preferences.searchText)))
                    {
                        if (GUILayout.Button("Clear", GUILayout.Width(48f)))
                        {
                            Preferences.searchText = string.Empty;
                            GUI.FocusControl(null);
                        }
                    }
                }

                Preferences.previewSize = EditorGUILayout.IntSlider("Preview Size", Preferences.previewSize, 50, 150);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawSpriteGrid()
        {
            List<Sprite> filtered = sprites
                .Where(sprite => sprite != null && SpriteMatchesSearch(sprite))
                .ToList();

            EditorGUILayout.LabelField($"Sprites ({filtered.Count} / {sprites.Count})", EditorStyles.boldLabel);
            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    ProjectSettings.Palettes.Count == 0
                        ? "Create a named palette and add one or more sprite folders."
                        : "No sprites match the current palette and search.",
                    MessageType.Info
                );
                return;
            }

            float usableWidth = Mathf.Max(120f, position.width - (position.width < 620f ? 30f : 220f));
            int columns = Mathf.Max(1, Mathf.FloorToInt(usableWidth / (Preferences.previewSize + 12f)));

            for (int index = 0; index < filtered.Count; index += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        int spriteIndex = index + column;
                        if (spriteIndex < filtered.Count)
                            DrawSpriteButton(filtered[spriteIndex]);
                        else
                            GUILayout.Space(Preferences.previewSize + 4f);
                    }
                }
            }
        }

        private void DrawSpriteButton(Sprite sprite)
        {
            int size = Preferences.previewSize;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(size)))
            {
                Rect previewRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
                bool isActive = sprite == activeSprite;

                if (GUI.Button(previewRect, GUIContent.none, isActive ? activeSpriteButtonStyle : spriteButtonStyle))
                    StartPlacement(sprite);

                DrawSpritePreview(sprite, previewRect);

                string key = SpritePaletteAssetUtility.GetSpriteKey(sprite);
                using (new EditorGUILayout.HorizontalScope(GUILayout.Width(size)))
                {
                    GUILayout.Label(sprite.name, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(Mathf.Max(20f, size - 48f)));
                    if (GUILayout.Button(Preferences.IsFavorite(key) ? "★" : "☆", EditorStyles.miniButton, GUILayout.Width(22f)))
                    {
                        Preferences.ToggleFavorite(key);
                        if (Preferences.activePaletteId == FavoritesPaletteId)
                            LoadSprites();
                    }

                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(22f)))
                        ShowAddToPaletteMenu(sprite);
                }
            }
        }

        private static void DrawSpritePreview(Sprite sprite, Rect previewRect)
        {
            if (sprite == null || sprite.texture == null)
            {
                GUI.Label(previewRect, "Preview unavailable", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            Rect textureRect;
            try
            {
                textureRect = sprite.textureRect;
            }
            catch (Exception)
            {
                GUI.Label(previewRect, sprite.name, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (sprite.texture.width <= 0 || sprite.texture.height <= 0 || textureRect.width <= 0f || textureRect.height <= 0f)
                return;

            Rect inner = new(previewRect.x + 5f, previewRect.y + 5f, previewRect.width - 10f, previewRect.height - 10f);
            Rect uv = new(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height
            );
            Rect fitted = CalculateAspectFitRect(inner, textureRect.width, textureRect.height);
            GUI.DrawTextureWithTexCoords(fitted, sprite.texture, uv, true);
        }

        private static Rect CalculateAspectFitRect(Rect container, float width, float height)
        {
            float aspect = width / height;
            float targetWidth = container.width;
            float targetHeight = targetWidth / aspect;
            if (targetHeight > container.height)
            {
                targetHeight = container.height;
                targetWidth = targetHeight * aspect;
            }

            return new Rect(
                container.x + (container.width - targetWidth) * 0.5f,
                container.y + (container.height - targetHeight) * 0.5f,
                targetWidth,
                targetHeight
            );
        }

        private bool SpriteMatchesSearch(Sprite sprite)
        {
            return string.IsNullOrWhiteSpace(Preferences.searchText) ||
                   sprite.name.IndexOf(Preferences.searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ShowAddToPaletteMenu(Sprite sprite)
        {
            if (sprite == null)
                return;

            GenericMenu menu = new();
            menu.AddItem(
                new GUIContent("Create New Palette From This Sprite"),
                false,
                () => CreatePaletteFromSprite(sprite)
            );
            menu.AddSeparator(string.Empty);

            if (ProjectSettings.Palettes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("Add to Existing Palette/No palettes available"));
            }
            else
            {
                foreach (SpritePaletteDefinition palette in ProjectSettings.Palettes)
                {
                    if (palette == null)
                        continue;

                    SpritePaletteDefinition capturedPalette = palette;
                    string safeName = palette.name.Replace("/", "⁄");
                    string path = "Add to Existing Palette/" + safeName;

                    if (PaletteContainsSprite(palette, sprite))
                        menu.AddDisabledItem(new GUIContent(path + " (Already Included)"));
                    else
                        menu.AddItem(new GUIContent(path), false, () => AddSpriteToPalette(sprite, capturedPalette));
                }
            }

            menu.ShowAsContext();
        }

        private void AddSpriteToPalette(Sprite sprite, SpritePaletteDefinition palette)
        {
            if (sprite == null || palette == null || PaletteContainsSprite(palette, sprite))
                return;

            string key = SpritePaletteAssetUtility.GetSpriteKey(sprite);
            if (string.IsNullOrEmpty(key))
                return;

            palette.spriteKeys.Add(key);
            ProjectSettings.Commit();
        }

        private void CreatePaletteFromSprite(Sprite sprite)
        {
            if (sprite == null)
                return;

            SpritePaletteDefinition palette = ProjectSettings.CreatePalette(sprite.name + " Palette");
            string key = SpritePaletteAssetUtility.GetSpriteKey(sprite);
            if (!string.IsNullOrEmpty(key))
                palette.spriteKeys.Add(key);

            ProjectSettings.Commit();
            SelectPalette(palette.id);
            SpritePaletteDefinitionEditorWindow.Open(palette);
        }

        private static bool PaletteContainsSprite(SpritePaletteDefinition palette, Sprite sprite)
        {
            if (palette == null || sprite == null)
                return false;

            string key = SpritePaletteAssetUtility.GetSpriteKey(sprite);
            if (palette.spriteKeys.Contains(key))
                return true;

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            string assetDirectory = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            foreach (string folderGuid in palette.folderGuids)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);
                if (string.IsNullOrEmpty(folderPath))
                    continue;

                if (palette.includeSubfolders)
                {
                    if (assetPath.StartsWith(folderPath + "/", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (string.Equals(assetDirectory, folderPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void DeleteActivePalette()
        {
            SpritePaletteDefinition palette = GetActiveNamedPalette();
            if (palette == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "Delete Palette",
                    $"Delete '{palette.name}'? Sprite assets will not be deleted.",
                    "Delete",
                    "Cancel"))
                return;

            ProjectSettings.RemovePalette(palette.id);
            EnsureValidPaletteSelection();
            LoadSprites();
        }

        private void CreatePalette()
        {
            SpritePaletteDefinition palette = ProjectSettings.CreatePalette("New Palette");
            SelectPalette(palette.id);
            SpritePaletteDefinitionEditorWindow.Open(palette);
        }

        private void ShowPaletteMenu()
        {
            SpritePaletteDefinition palette = GetActiveNamedPalette();
            if (palette == null)
                return;

            GenericMenu menu = new();
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                SpritePaletteDefinition copy = ProjectSettings.DuplicatePalette(palette);
                if (copy != null)
                {
                    SelectPalette(copy.id);
                    SpritePaletteDefinitionEditorWindow.Open(copy);
                }
            });
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete Palette", $"Delete '{palette.name}'? Sprite assets will not be deleted.", "Delete", "Cancel"))
                {
                    ProjectSettings.RemovePalette(palette.id);
                    EnsureValidPaletteSelection();
                    LoadSprites();
                }
            });
            menu.ShowAsContext();
        }

        private List<string> BuildPaletteIdList()
        {
            List<string> ids = new() { AllPaletteId, FavoritesPaletteId, RecentPaletteId };
            ids.AddRange(ProjectSettings.Palettes.Where(item => item != null).Select(item => item.id));
            return ids;
        }

        private string GetPaletteDisplayName(string id)
        {
            if (id == AllPaletteId) return "All Sprites";
            if (id == FavoritesPaletteId) return "Favorites";
            if (id == RecentPaletteId) return "Recent";
            return ProjectSettings.FindPalette(id)?.name ?? "Palette";
        }

        private SpritePaletteDefinition GetActiveNamedPalette()
        {
            return ProjectSettings.FindPalette(Preferences.activePaletteId);
        }

        private void SelectPalette(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            StopPlacement();
            Preferences.activePaletteId = id;
            Preferences.Commit();
            ApplyPaletteDefaults(ProjectSettings.FindPalette(id));
            LoadSprites();
        }

        private void ApplyPaletteDefaults(SpritePaletteDefinition palette)
        {
            if (palette == null || !palette.useDefaults || palette.defaults == null)
                return;

            SpritePlacementPreset value = palette.defaults;
            objectScale = value.scale;
            rotationZ = value.rotationZ;
            positionZ = value.positionZ;
            spriteColor = value.color;
            spriteMaterial = value.material;
            flipX = value.flipX;
            flipY = value.flipY;
            selectedSortingLayer = value.sortingLayer;
            sortingOrder = value.sortingOrder;
            overlapOrderMode = palette.defaultOverlapOrderMode;
            Preferences.snapToGrid = palette.defaultSnapToGrid;
            Preferences.gridSize = Mathf.Max(0.01f, palette.defaultGridSize);
        }

        private void LoadSprites()
        {
            ProjectSettings.EnsureInitialized();
            HashSet<Sprite> result = new();
            string id = Preferences.activePaletteId;

            if (id == FavoritesPaletteId)
            {
                foreach (string key in Preferences.favorites ?? new List<string>())
                    AddResolvedSprite(result, key);
            }
            else if (id == RecentPaletteId)
            {
                foreach (string key in Preferences.recent ?? new List<string>())
                    AddResolvedSprite(result, key);
            }
            else if (id == AllPaletteId)
            {
                foreach (SpritePaletteDefinition palette in ProjectSettings.Palettes)
                    AddPaletteSprites(result, palette);
            }
            else
            {
                AddPaletteSprites(result, ProjectSettings.FindPalette(id));
            }

            sprites.Clear();
            sprites.AddRange(result);
            sprites.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            RefreshPaletteCounts();
            Repaint();
        }

        private void RefreshPaletteCounts()
        {
            paletteCounts.Clear();
            HashSet<Sprite> all = new();
            foreach (SpritePaletteDefinition palette in ProjectSettings.Palettes)
            {
                HashSet<Sprite> paletteSprites = new();
                AddPaletteSprites(paletteSprites, palette);
                paletteCounts[palette.id] = paletteSprites.Count;
                all.UnionWith(paletteSprites);
            }

            paletteCounts[AllPaletteId] = all.Count;
            paletteCounts[FavoritesPaletteId] = Preferences.favorites?.Count(key => SpritePaletteAssetUtility.ResolveSprite(key) != null) ?? 0;
            paletteCounts[RecentPaletteId] = Preferences.recent?.Count(key => SpritePaletteAssetUtility.ResolveSprite(key) != null) ?? 0;
        }

        private static void AddResolvedSprite(HashSet<Sprite> target, string key)
        {
            Sprite sprite = SpritePaletteAssetUtility.ResolveSprite(key);
            if (sprite != null)
                target.Add(sprite);
        }

        private static void AddPaletteSprites(HashSet<Sprite> target, SpritePaletteDefinition palette)
        {
            if (palette == null)
                return;

            foreach (string folderGuid in palette.folderGuids)
            {
                string folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);
                if (!AssetDatabase.IsValidFolder(folderPath))
                    continue;

                foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folderPath }))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!palette.includeSubfolders)
                    {
                        string directory = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
                        if (!string.Equals(directory, folderPath, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                    {
                        if (asset is Sprite sprite)
                            target.Add(sprite);
                    }
                }
            }

            foreach (string key in palette.spriteKeys)
                AddResolvedSprite(target, key);
        }

        private void StartPlacement(Sprite sprite)
        {
            activeSprite = sprite;
            ActivePlacementWindow = this;
            Preferences.AddRecent(SpritePaletteAssetUtility.GetSpriteKey(sprite));

            if (Preferences.rememberPresetPerSprite)
                LoadPresetForSprite(sprite);

            CreateOrUpdatePreviewObject();
            SceneView.RepaintAll();
            Repaint();

            EditorApplication.update -= FocusSceneViewForPlacement;
            EditorApplication.update += FocusSceneViewForPlacement;
        }

        private void FocusSceneViewForPlacement()
        {
            EditorApplication.update -= FocusSceneViewForPlacement;
            if (activeSprite == null)
                return;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            sceneView.Focus();
            sceneView.Repaint();
        }

        internal void StopPlacement()
        {
            EditorApplication.update -= FocusSceneViewForPlacement;
            if (ActivePlacementWindow == this)
                ActivePlacementWindow = null;
            activeSprite = null;
            DestroyPreviewObject();
            SceneView.RepaintAll();
            Repaint();
        }

        internal void RotatePlacement(float direction)
        {
            if (activeSprite == null)
                return;
            rotationZ = Mathf.Repeat(rotationZ + direction * Preferences.rotationHotkeyStep, 360f);
            RefreshPreviewAndWindow();
        }

        internal void ResizePlacement(float direction)
        {
            if (activeSprite == null)
                return;
            float factor = 1f + Preferences.scaleHotkeyPercent / 100f;
            objectScale = direction > 0f ? objectScale * factor : objectScale / factor;
            RefreshPreviewAndWindow();
        }

        internal void AdjustOrder(int direction)
        {
            if (activeSprite == null)
                return;
            sortingOrder = direction > 0 ? SafeIncrementOrder(sortingOrder) : SafeDecrementOrder(sortingOrder);
            RefreshPreviewAndWindow();
        }

        private void RefreshPreviewAndWindow()
        {
            UpdatePreviewObjectProperties();
            UpdatePreviewTransform();
            SceneView.RepaintAll();
            Repaint();
        }

        private void CreateOrUpdatePreviewObject()
        {
            if (previewObject == null)
            {
                previewObject = new GameObject("Sprite Palette Preview")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            }

            if (defaultPreviewMaterial == null)
                defaultPreviewMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            UpdatePreviewObjectProperties();
            UpdatePreviewTransform();
        }

        private void UpdatePreviewObjectProperties()
        {
            if (previewRenderer == null)
                return;

            previewRenderer.sprite = activeSprite;
            previewRenderer.sortingLayerName = selectedSortingLayer;
            previewRenderer.sortingOrder = sortingOrder;
            previewRenderer.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, Mathf.Min(spriteColor.a, 0.65f));
            previewRenderer.flipX = flipX;
            previewRenderer.flipY = flipY;
            previewRenderer.sharedMaterial = spriteMaterial != null ? spriteMaterial : defaultPreviewMaterial;
        }

        private void UpdatePreviewTransform()
        {
            if (previewObject == null)
                return;

            previewObject.transform.SetPositionAndRotation(
                currentPlacementPosition,
                Quaternion.Euler(0f, 0f, rotationZ)
            );
            previewObject.transform.localScale = new Vector3(objectScale.x, objectScale.y, 1f);
        }

        private void DestroyPreviewObject()
        {
            if (previewObject != null)
                DestroyImmediate(previewObject);
            previewObject = null;
            previewRenderer = null;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (activeSprite == null)
                return;

            Event currentEvent = Event.current;
            EventType currentEventType = currentEvent.type;
            int controlId = GUIUtility.GetControlID(0x5350504C, FocusType.Passive);
            EventType eventType = currentEvent.GetTypeForControl(controlId);

            if (eventType == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            if (eventType == EventType.KeyDown && SpritePaletteShortcuts.HandlePlacementInput(currentEvent))
                return;

            if (currentEventType == EventType.MouseMove ||
                currentEventType == EventType.MouseDrag ||
                currentEventType == EventType.Repaint)
            {
                UpdatePlacementPosition(currentEvent.mousePosition);
                UpdatePreviewTransform();
            }

            if (currentEventType == EventType.Repaint)
                DrawSceneInstructions();

            if (eventType == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                CreateSpriteObject(activeSprite, currentPlacementPosition);
                currentEvent.Use();
            }
            else if (eventType == EventType.MouseDown && currentEvent.button == 1)
            {
                StopPlacement();
                currentEvent.Use();
            }

            if (eventType == EventType.MouseMove || eventType == EventType.MouseDrag)
                sceneView.Repaint();
        }

        private void UpdatePlacementPosition(Vector2 mousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Plane placementPlane = new(Vector3.forward, new Vector3(0f, 0f, positionZ));
            if (!placementPlane.Raycast(ray, out float distance))
                return;

            Vector3 value = ray.GetPoint(distance);
            if (Preferences.snapToGrid)
            {
                value.x = Mathf.Round(value.x / Preferences.gridSize) * Preferences.gridSize;
                value.y = Mathf.Round(value.y / Preferences.gridSize) * Preferences.gridSize;
            }

            value.z = positionZ;
            currentPlacementPosition = value;
        }

        private void DrawSceneInstructions()
        {
            Handles.BeginGUI();

            Rect panelRect = new(12f, 12f, 390f, 144f);
            GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

            const float left = 22f;
            const float width = 370f;
            const float lineHeight = 19f;
            float top = 20f;

            GUI.Label(new Rect(left, top, width, lineHeight), $"Sprite: {activeSprite.name}", EditorStyles.boldLabel);
            top += lineHeight;
            GUI.Label(new Rect(left, top, width, lineHeight), $"Layer: {selectedSortingLayer} | Order: {sortingOrder}");
            top += lineHeight;
            GUI.Label(new Rect(left, top, width, lineHeight), $"{SpritePaletteShortcuts.RotateLeftDisplay}/{SpritePaletteShortcuts.RotateRightDisplay}: rotate by {Preferences.rotationHotkeyStep:0.#}°");
            top += lineHeight;
            GUI.Label(new Rect(left, top, width, lineHeight), $"{SpritePaletteShortcuts.ResizeUpDisplay}/{SpritePaletteShortcuts.ResizeDownDisplay}: resize by {Preferences.scaleHotkeyPercent:0.#}%");
            top += lineHeight;
            GUI.Label(new Rect(left, top, width, lineHeight), $"{SpritePaletteShortcuts.OrderUpDisplay}/{SpritePaletteShortcuts.OrderDownDisplay}: change Order in Layer");
            top += lineHeight;
            GUI.Label(new Rect(left, top, width, lineHeight), "Left-click: place | Esc/right-click: stop");

            Handles.EndGUI();
        }

        private void CreateSpriteObject(Sprite sprite, Vector3 worldPosition)
        {
            string finalName = string.IsNullOrWhiteSpace(objectNameOverride) ? sprite.name : objectNameOverride;
            GameObject spriteObject = new(finalName);
            StageUtility.PlaceGameObjectInCurrentStage(spriteObject);
            Undo.RegisterCreatedObjectUndo(spriteObject, "Place sprite");

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = selectedSortingLayer;
            renderer.sortingOrder = sortingOrder;
            renderer.color = spriteColor;
            renderer.flipX = flipX;
            renderer.flipY = flipY;
            if (spriteMaterial != null)
                renderer.sharedMaterial = spriteMaterial;

            spriteObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0f, 0f, rotationZ));
            spriteObject.transform.localScale = new Vector3(objectScale.x, objectScale.y, 1f);

            if (parent != null)
            {
                try
                {
                    Undo.SetTransformParent(spriteObject.transform, parent, "Set sprite parent");
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Sprite Palette could not use the selected parent: {exception.Message}");
                }
            }

            Physics2D.SyncTransforms();
            if (overlapOrderMode != OverlapOrderMode.Disabled)
                renderer.sortingOrder = CalculateOrderFromOverlappingSprites(renderer, sortingOrder, overlapOrderMode);

            if (Preferences.rememberPresetPerSprite)
                SavePresetForActiveSprite();

            Selection.activeGameObject = spriteObject;
        }

        private int CalculateOrderFromOverlappingSprites(SpriteRenderer newRenderer, int defaultOrder, OverlapOrderMode mode)
        {
            Bounds newBounds = newRenderer.bounds;
            bool found = false;
            int lowest = int.MaxValue;
            int highest = int.MinValue;

            foreach (SpriteRenderer other in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (other == null || other == newRenderer || other == previewRenderer || !other.enabled || other.sprite == null)
                    continue;
                if (other.gameObject.scene != newRenderer.gameObject.scene || other.sortingLayerID != newRenderer.sortingLayerID)
                    continue;
                if (!BoundsOverlap2D(newBounds, other.bounds))
                    continue;

                found = true;
                lowest = Mathf.Min(lowest, other.sortingOrder);
                highest = Mathf.Max(highest, other.sortingOrder);
            }

            if (!found)
                return defaultOrder;
            return mode == OverlapOrderMode.BringForward ? SafeIncrementOrder(highest) : SafeDecrementOrder(lowest);
        }

        private static bool BoundsOverlap2D(Bounds first, Bounds second)
        {
            return first.min.x <= second.max.x && first.max.x >= second.min.x &&
                   first.min.y <= second.max.y && first.max.y >= second.min.y;
        }

        private static int SafeIncrementOrder(int value) => value == int.MaxValue ? int.MaxValue : value + 1;
        private static int SafeDecrementOrder(int value) => value == int.MinValue ? int.MinValue : value - 1;

        private void LoadPresetForSprite(Sprite sprite)
        {
            string key = SpritePaletteAssetUtility.GetSpriteKey(sprite);
            if (!ProjectSettings.TryGetPreset(key, out SpritePlacementPreset preset))
                return;

            if (preset.rememberScale) objectScale = preset.scale;
            if (preset.rememberRotation)
            {
                rotationZ = preset.rotationZ;
                positionZ = preset.positionZ;
            }
            if (preset.rememberRendering)
            {
                spriteColor = preset.color;
                spriteMaterial = preset.material;
                flipX = preset.flipX;
                flipY = preset.flipY;
            }
            if (preset.rememberSorting)
            {
                selectedSortingLayer = preset.sortingLayer;
                sortingOrder = preset.sortingOrder;
            }
        }

        private void SavePresetForActiveSprite()
        {
            string key = SpritePaletteAssetUtility.GetSpriteKey(activeSprite);
            if (string.IsNullOrEmpty(key))
                return;

            ProjectSettings.SetPreset(new SpritePlacementPreset
            {
                spriteKey = key,
                rememberScale = Preferences.presetRememberScale,
                scale = objectScale,
                rememberRotation = Preferences.presetRememberRotation,
                rotationZ = rotationZ,
                positionZ = positionZ,
                rememberRendering = Preferences.presetRememberRendering,
                color = spriteColor,
                material = spriteMaterial,
                flipX = flipX,
                flipY = flipY,
                rememberSorting = Preferences.presetRememberSorting,
                sortingLayer = selectedSortingLayer,
                sortingOrder = sortingOrder
            });
        }

        private void ResetPlacementProperties()
        {
            selectedSortingLayer = "Default";
            sortingOrder = 0;
            overlapOrderMode = OverlapOrderMode.BringForward;
            spriteColor = Color.white;
            spriteMaterial = null;
            flipX = false;
            flipY = false;
            objectScale = Vector2.one;
            rotationZ = 0f;
            positionZ = 0f;
            objectNameOverride = string.Empty;
            RefreshPreviewAndWindow();
        }

        private void CopyPropertiesFromSelectedObject()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Sprite Palette", "Select a GameObject with a SpriteRenderer.", "OK");
                return;
            }

            SpriteRenderer renderer = selected.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Sprite Palette", "The selected GameObject does not contain a SpriteRenderer.", "OK");
                return;
            }

            selectedSortingLayer = renderer.sortingLayerName;
            sortingOrder = renderer.sortingOrder;
            spriteColor = renderer.color;
            spriteMaterial = renderer.sharedMaterial;
            flipX = renderer.flipX;
            flipY = renderer.flipY;
            Vector3 scale = selected.transform.localScale;
            objectScale = new Vector2(scale.x, scale.y);
            rotationZ = selected.transform.eulerAngles.z;
            positionZ = selected.transform.position.z;
            objectNameOverride = selected.name;
            RefreshPreviewAndWindow();
        }

        private static string DrawSortingLayerPopup(string label, string selected)
        {
            SortingLayer[] layers = SortingLayer.layers;
            if (layers == null || layers.Length == 0)
                return "Default";

            string[] names = layers.Select(item => item.name).ToArray();
            int index = Mathf.Max(0, Array.IndexOf(names, selected));
            return names[EditorGUILayout.Popup(label, index, names)];
        }

        private string GetPlacementTargetDescription()
        {
            if (parent != null)
                return $"{parent.gameObject.scene.name}/{parent.name}";

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                return $"Prefab Stage/{Path.GetFileName(prefabStage.assetPath)}";

            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        private void SavePreferences()
        {
            Preferences.Commit();
        }
    }
}