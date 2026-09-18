# Changelog

## 1.2.0

- Renamed the public Editor namespace and assembly to WhatIDoToday.SpritePalette.Editor.
- Removed the former game-specific identity from all distributable package files.
- Preserved existing palette and per-sprite preset serialization through unchanged asset GUIDs and field names.
- Fixed Editor window initialization so GUI styles are only accessed from OnGUI.

## 1.1.3

- Fixed named palette sprites not loading automatically when the window opens.
- Restored the placement instructions overlay in the Scene view.
- Focused the Scene view when placement starts so the first rotate, resize or sorting command is captured.

## 1.1.2

- Fixed Scene view placement preview updates in Unity 2022 and Unity 6.
- Fixed the IMGUI exception caused by drawing the placement overlay outside Repaint events.
- Scoped placement hotkeys to active placement so they no longer conflict with Unity's global Q/E bindings.

## 1.1.1

- Consolidated palette Edit and Delete actions in the main toolbar.
- Removed duplicate palette actions from compact, header and sidebar layouts.
- Added MIT licensing and public UPM package metadata.
- Expanded installation and usage documentation.
- Renamed the public package identifier to com.whatidotoday.sprite-palette.

## 1.1.0

- Moved search and preview size controls directly above the sprite grid.
- Added direct sprite-to-palette actions for new and existing palettes.
- Added explicit palette deletion controls in compact and full layouts.

## 1.0.0

- Initial distributable release.
- Added named palettes, favorites, recent sprites, configurable shortcuts, full presets and stage-aware placement.
