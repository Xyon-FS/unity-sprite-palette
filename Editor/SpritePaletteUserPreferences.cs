using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WhatIDoToday.SpritePalette.Editor
{
    [FilePath("UserSettings/SpritePaletteUserPreferences.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class SpritePaletteUserPreferences : ScriptableSingleton<SpritePaletteUserPreferences>
    {
        [SerializeField] internal string activePaletteId = string.Empty;
        [SerializeField] internal string searchText = string.Empty;
        [SerializeField] internal int previewSize = 80;
        [SerializeField] internal bool snapToGrid = true;
        [SerializeField] internal float gridSize = 1f;
        [SerializeField] internal float rotationHotkeyStep = 15f;
        [SerializeField] internal float scaleHotkeyPercent = 10f;
        [SerializeField] internal bool rememberPresetPerSprite = true;
        [SerializeField] internal bool presetRememberScale = true;
        [SerializeField] internal bool presetRememberRotation;
        [SerializeField] internal bool presetRememberRendering;
        [SerializeField] internal bool presetRememberSorting;
        [SerializeField] internal bool showSource = true;
        [SerializeField] internal bool showRendering = true;
        [SerializeField] internal bool showTransform = true;
        [SerializeField] internal bool showPlacement = true;
        [SerializeField] internal bool showPreset;
        [SerializeField] internal List<string> favorites = new();
        [SerializeField] internal List<string> recent = new();

        internal bool IsFavorite(string key)
        {
            favorites ??= new List<string>();
            return favorites.Contains(key);
        }

        internal void ToggleFavorite(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            favorites ??= new List<string>();
            if (!favorites.Remove(key))
                favorites.Add(key);
            Save(true);
        }

        internal void AddRecent(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            recent ??= new List<string>();
            recent.Remove(key);
            recent.Insert(0, key);
            if (recent.Count > 24)
                recent.RemoveRange(24, recent.Count - 24);
            Save(true);
        }

        internal void Commit()
        {
            previewSize = Mathf.Clamp(previewSize, 50, 150);
            gridSize = Mathf.Max(0.01f, gridSize);
            rotationHotkeyStep = Mathf.Max(0.1f, rotationHotkeyStep);
            scaleHotkeyPercent = Mathf.Clamp(scaleHotkeyPercent, 0.1f, 100f);
            Save(true);
        }
    }
}