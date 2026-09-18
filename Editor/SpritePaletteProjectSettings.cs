using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace WhatIDoToday.SpritePalette.Editor
{
    internal enum OverlapOrderMode
    {
        Disabled,
        BringForward,
        SendBackward
    }

    [Serializable]
    internal sealed class SpritePlacementPreset
    {
        public string spriteKey = string.Empty;
        public bool rememberScale = true;
        public Vector2 scale = Vector2.one;
        public bool rememberRotation;
        public float rotationZ;
        public bool rememberRendering;
        public Color color = Color.white;
        public Material material;
        public bool flipX;
        public bool flipY;
        public bool rememberSorting;
        public string sortingLayer = "Default";
        public int sortingOrder;
        public float positionZ;

        public SpritePlacementPreset Clone()
        {
            return (SpritePlacementPreset)MemberwiseClone();
        }
    }

    [Serializable]
    internal sealed class SpritePaletteDefinition
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "New Palette";
        public bool includeSubfolders = true;
        public List<string> folderGuids = new();
        public List<string> spriteKeys = new();
        public bool useDefaults;
        public bool defaultSnapToGrid = true;
        public float defaultGridSize = 1f;
        public OverlapOrderMode defaultOverlapOrderMode = OverlapOrderMode.BringForward;
        public SpritePlacementPreset defaults = new();

        public SpritePaletteDefinition Clone()
        {
            SpritePaletteDefinition copy = new()
            {
                id = Guid.NewGuid().ToString("N"),
                name = name + " Copy",
                includeSubfolders = includeSubfolders,
                folderGuids = new List<string>(folderGuids),
                spriteKeys = new List<string>(spriteKeys),
                useDefaults = useDefaults,
                defaultSnapToGrid = defaultSnapToGrid,
                defaultGridSize = defaultGridSize,
                defaultOverlapOrderMode = defaultOverlapOrderMode,
                defaults = defaults != null ? defaults.Clone() : new SpritePlacementPreset()
            };
            return copy;
        }
    }

    [FilePath("ProjectSettings/SpritePaletteProjectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class SpritePaletteProjectSettings : ScriptableSingleton<SpritePaletteProjectSettings>
    {
        private const int CurrentDataVersion = 1;
        private const string LegacyDatabasePath = "ProjectSettings/SpritePaletteScaleDatabase.asset";

        [SerializeField] private int dataVersion;
        [SerializeField] private bool legacyMigrationAttempted;
        [SerializeField] private List<SpritePaletteDefinition> palettes = new();
        [SerializeField] private List<SpritePlacementPreset> presets = new();

        internal static event Action Changed;

        internal IReadOnlyList<SpritePaletteDefinition> Palettes
        {
            get
            {
                EnsureInitialized();
                return palettes;
            }
        }

        internal void EnsureInitialized()
        {
            palettes ??= new List<SpritePaletteDefinition>();
            presets ??= new List<SpritePlacementPreset>();

            if (!legacyMigrationAttempted)
            {
                legacyMigrationAttempted = true;
                ImportLegacyScales();
            }

            if (dataVersion < CurrentDataVersion)
            {
                dataVersion = CurrentDataVersion;
                Save(true);
            }
        }

        internal SpritePaletteDefinition CreatePalette(string requestedName)
        {
            EnsureInitialized();
            SpritePaletteDefinition palette = new()
            {
                name = GetUniqueName(string.IsNullOrWhiteSpace(requestedName) ? "New Palette" : requestedName.Trim())
            };
            palettes.Add(palette);
            Commit();
            return palette;
        }

        internal SpritePaletteDefinition DuplicatePalette(SpritePaletteDefinition source)
        {
            if (source == null)
                return null;

            EnsureInitialized();
            SpritePaletteDefinition copy = source.Clone();
            copy.name = GetUniqueName(copy.name);
            palettes.Add(copy);
            Commit();
            return copy;
        }

        internal bool RemovePalette(string id)
        {
            EnsureInitialized();
            int removed = palettes.RemoveAll(item => item != null && item.id == id);
            if (removed > 0)
                Commit();
            return removed > 0;
        }

        internal SpritePaletteDefinition FindPalette(string id)
        {
            EnsureInitialized();
            return palettes.Find(item => item != null && item.id == id);
        }

        internal bool TryGetPreset(string spriteKey, out SpritePlacementPreset preset)
        {
            EnsureInitialized();
            preset = presets.Find(item => item != null && item.spriteKey == spriteKey);
            return preset != null;
        }

        internal void SetPreset(SpritePlacementPreset preset)
        {
            if (preset == null || string.IsNullOrEmpty(preset.spriteKey))
                return;

            EnsureInitialized();
            int index = presets.FindIndex(item => item != null && item.spriteKey == preset.spriteKey);
            if (index >= 0)
                presets[index] = preset.Clone();
            else
                presets.Add(preset.Clone());
            Save(true);
        }

        internal void RemovePreset(string spriteKey)
        {
            EnsureInitialized();
            if (presets.RemoveAll(item => item != null && item.spriteKey == spriteKey) > 0)
                Save(true);
        }

        internal void Commit()
        {
            dataVersion = CurrentDataVersion;
            Save(true);
            Changed?.Invoke();
        }

        private string GetUniqueName(string baseName)
        {
            string candidate = baseName;
            int suffix = 2;
            while (palettes.Exists(item => item != null && string.Equals(item.name, candidate, StringComparison.OrdinalIgnoreCase)))
                candidate = $"{baseName} {suffix++}";
            return candidate;
        }

        private void ImportLegacyScales()
        {
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, LegacyDatabasePath);
            if (!File.Exists(fullPath))
            {
                Save(true);
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(fullPath);
                for (int i = 0; i + 1 < lines.Length; i++)
                {
                    Match keyMatch = Regex.Match(lines[i], @"^\s*-\s*spriteKey:\s*(.+)\s*$");
                    if (!keyMatch.Success)
                        continue;

                    Match scaleMatch = Regex.Match(lines[i + 1], @"^\s*scale:\s*\{x:\s*([^,]+),\s*y:\s*([^}]+)\}");
                    if (!scaleMatch.Success)
                        continue;

                    if (!float.TryParse(scaleMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                        !float.TryParse(scaleMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        continue;

                    string key = keyMatch.Groups[1].Value.Trim();
                    if (presets.Exists(item => item != null && item.spriteKey == key))
                        continue;

                    presets.Add(new SpritePlacementPreset
                    {
                        spriteKey = key,
                        rememberScale = true,
                        scale = new Vector2(x, y)
                    });
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Sprite Palette could not migrate the legacy scale database: {exception.Message}");
            }

            Save(true);
        }
    }

    internal static class SpritePaletteAssetUtility
    {
        internal static string GetSpriteKey(Sprite sprite)
        {
            if (sprite == null)
                return string.Empty;

            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long localId)
                ? $"{guid}:{localId}"
                : string.Empty;
        }

        internal static Sprite ResolveSprite(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            int separator = key.LastIndexOf(':');
            if (separator <= 0 || !long.TryParse(key[(separator + 1)..], out long localId))
                return null;

            string path = AssetDatabase.GUIDToAssetPath(key[..separator]);
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out _, out long candidateId) &&
                    candidateId == localId)
                    return sprite;
            }

            return null;
        }

        internal static DefaultAsset ResolveFolder(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
        }

        internal static string GetFolderGuid(DefaultAsset folder)
        {
            if (folder == null)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(folder);
            return AssetDatabase.IsValidFolder(path) ? AssetDatabase.AssetPathToGUID(path) : string.Empty;
        }
    }
}