using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WhatIDoToday.SpritePalette.Editor
{
    internal sealed class SpritePaletteDefinitionEditorWindow : EditorWindow
    {
        private SpritePaletteDefinition palette;
        private readonly List<DefaultAsset> folders = new();
        private readonly List<Sprite> individualSprites = new();
        private DefaultAsset folderToAdd;
        private Sprite spriteToAdd;
        
        private bool showDefaults = true;
private Vector2 scroll;

        internal static void Open(SpritePaletteDefinition definition)
        {
            if (definition == null)
                return;

            SpritePaletteDefinitionEditorWindow window = GetWindow<SpritePaletteDefinitionEditorWindow>(true, "Edit Sprite Palette", true);
            window.minSize = new Vector2(420f, 460f);
            window.Load(definition);
            window.Show();
        }

        private void Load(SpritePaletteDefinition definition)
        {
            palette = definition;
            folders.Clear();
            individualSprites.Clear();

            foreach (string guid in palette.folderGuids)
            {
                DefaultAsset folder = SpritePaletteAssetUtility.ResolveFolder(guid);
                if (folder != null)
                    folders.Add(folder);
            }

            foreach (string key in palette.spriteKeys)
            {
                Sprite sprite = SpritePaletteAssetUtility.ResolveSprite(key);
                if (sprite != null)
                    individualSprites.Add(sprite);
            }
        }

        private void OnGUI()
        {
            if (palette == null)
            {
                EditorGUILayout.HelpBox("The palette is no longer available.", MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Named Palette", EditorStyles.boldLabel);
            palette.name = EditorGUILayout.TextField("Name", palette.name);
            palette.includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", palette.includeSubfolders);

            EditorGUILayout.Space(8f);
            DrawDragAndDropArea();
            EditorGUILayout.Space(8f);
            DrawFolders();
            EditorGUILayout.Space(8f);
            DrawSprites();
            EditorGUILayout.Space(8f);
            DrawDefaults();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel"))
                    Close();

                if (GUILayout.Button("Save"))
                {
                    SavePalette();
                    Close();
                }
            }
        }

        private void DrawDragAndDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop sprite folders or individual sprites here", EditorStyles.helpBox);

            Event current = Event.current;
            if (!dropArea.Contains(current.mousePosition) ||
                (current.type != EventType.DragUpdated && current.type != EventType.DragPerform))
                return;

            bool acceptsAny = false;
            foreach (UnityEngine.Object reference in DragAndDrop.objectReferences)
            {
                if (reference is Sprite)
                {
                    acceptsAny = true;
                    break;
                }

                if (reference is DefaultAsset folder &&
                    !string.IsNullOrEmpty(SpritePaletteAssetUtility.GetFolderGuid(folder)))
                {
                    acceptsAny = true;
                    break;
                }
            }

            DragAndDrop.visualMode = acceptsAny ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (current.type != EventType.DragPerform || !acceptsAny)
            {
                current.Use();
                return;
            }

            DragAndDrop.AcceptDrag();
            foreach (UnityEngine.Object reference in DragAndDrop.objectReferences)
            {
                if (reference is Sprite sprite && !individualSprites.Contains(sprite))
                    individualSprites.Add(sprite);
                else if (reference is DefaultAsset folder &&
                         !string.IsNullOrEmpty(SpritePaletteAssetUtility.GetFolderGuid(folder)) &&
                         !folders.Contains(folder))
                    folders.Add(folder);
            }

            current.Use();
            Repaint();
        }

        private void DrawFolders()
        {
            EditorGUILayout.LabelField("Source Folders", EditorStyles.boldLabel);
            for (int i = 0; i < folders.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    folders[i] = (DefaultAsset)EditorGUILayout.ObjectField(folders[i], typeof(DefaultAsset), false);
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        folders.RemoveAt(i);
                        i--;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                folderToAdd = (DefaultAsset)EditorGUILayout.ObjectField("Add Folder", folderToAdd, typeof(DefaultAsset), false);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(SpritePaletteAssetUtility.GetFolderGuid(folderToAdd))))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(54f)))
                    {
                        if (!folders.Contains(folderToAdd))
                            folders.Add(folderToAdd);
                        folderToAdd = null;
                    }
                }
            }
        }

        private void DrawSprites()
        {
            EditorGUILayout.LabelField("Individual Sprites", EditorStyles.boldLabel);
            for (int i = 0; i < individualSprites.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    individualSprites[i] = (Sprite)EditorGUILayout.ObjectField(individualSprites[i], typeof(Sprite), false);
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        individualSprites.RemoveAt(i);
                        i--;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                spriteToAdd = (Sprite)EditorGUILayout.ObjectField("Add Sprite", spriteToAdd, typeof(Sprite), false);
                using (new EditorGUI.DisabledScope(spriteToAdd == null))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(54f)))
                    {
                        if (!individualSprites.Contains(spriteToAdd))
                            individualSprites.Add(spriteToAdd);
                        spriteToAdd = null;
                    }
                }
            }
        }

        private void DrawDefaults()
        {
            showDefaults = EditorGUILayout.Foldout(showDefaults, "Palette Defaults", true);
            if (!showDefaults)
                return;

            palette.useDefaults = EditorGUILayout.Toggle("Use Palette Defaults", palette.useDefaults);
            if (!palette.useDefaults)
                return;

            palette.defaults ??= new SpritePlacementPreset();
            EditorGUI.indentLevel++;
            palette.defaults.scale = EditorGUILayout.Vector2Field("Scale", palette.defaults.scale);
            palette.defaults.rotationZ = EditorGUILayout.FloatField("Z Rotation", palette.defaults.rotationZ);
            palette.defaults.positionZ = EditorGUILayout.FloatField("Z Position", palette.defaults.positionZ);
            palette.defaults.color = EditorGUILayout.ColorField("Color", palette.defaults.color);
            palette.defaults.material = (Material)EditorGUILayout.ObjectField("Material", palette.defaults.material, typeof(Material), false);
            palette.defaults.sortingLayer = DrawSortingLayerPopup("Sorting Layer", palette.defaults.sortingLayer);
            palette.defaults.sortingOrder = EditorGUILayout.IntField("Order in Layer", palette.defaults.sortingOrder);
            palette.defaults.flipX = EditorGUILayout.Toggle("Flip X", palette.defaults.flipX);
            palette.defaults.flipY = EditorGUILayout.Toggle("Flip Y", palette.defaults.flipY);
            palette.defaultOverlapOrderMode = (OverlapOrderMode)EditorGUILayout.EnumPopup("Overlapping Auto Order", palette.defaultOverlapOrderMode);
            palette.defaultSnapToGrid = EditorGUILayout.Toggle("Grid Snap", palette.defaultSnapToGrid);
            using (new EditorGUI.DisabledScope(!palette.defaultSnapToGrid))
                palette.defaultGridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", palette.defaultGridSize));
            EditorGUI.indentLevel--;
        }

        private static string DrawSortingLayerPopup(string label, string selected)
        {
            SortingLayer[] layers = SortingLayer.layers;
            string[] names = new string[layers.Length];
            int selectedIndex = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i].name;
                if (names[i] == selected)
                    selectedIndex = i;
            }

            return names.Length == 0 ? "Default" : names[EditorGUILayout.Popup(label, selectedIndex, names)];
        }

        private void SavePalette()
        {
            palette.name = string.IsNullOrWhiteSpace(palette.name) ? "Unnamed Palette" : palette.name.Trim();
            palette.folderGuids.Clear();
            foreach (DefaultAsset folder in folders)
            {
                string guid = SpritePaletteAssetUtility.GetFolderGuid(folder);
                if (!string.IsNullOrEmpty(guid) && !palette.folderGuids.Contains(guid))
                    palette.folderGuids.Add(guid);
            }

            palette.spriteKeys.Clear();
            foreach (Sprite sprite in individualSprites)
            {
                string key = SpritePaletteAssetUtility.GetSpriteKey(sprite);
                if (!string.IsNullOrEmpty(key) && !palette.spriteKeys.Contains(key))
                    palette.spriteKeys.Add(key);
            }

            SpritePaletteProjectSettings.instance.Commit();
        }
    }
}