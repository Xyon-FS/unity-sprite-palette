# Sprite Palette user guide

Sprite Palette is an Editor-only workflow for collecting project sprites and creating SpriteRenderer GameObjects from a searchable Scene view palette.

## 1. Opening and resizing the window

Open **Tools > Sprite Palette**.

The minimum window size is 380 x 420. At widths below 620 pixels, the palette sidebar becomes a dropdown so the remaining controls remain usable.

The top toolbar contains:

- **Refresh**: reloads sprites and collection counts.
- **New Palette**: creates and opens a new named palette.
- **Edit**: edits the active named palette.
- **Delete**: deletes the active named palette after confirmation.

Edit and Delete are disabled for All Sprites, Favorites and Recent.

## 2. Creating and editing palettes

Select **New Palette**, then configure the palette in the modal editor.

### Name

Names are trimmed when saved. An empty name becomes Unnamed Palette. New palettes automatically receive a unique name.

### Include Subfolders

When enabled, every Sprite in every descendant of a source folder is included. When disabled, only Sprite assets located directly inside that folder are included.

### Drop area

Drag any combination of valid project folders and Sprite assets into the drop area. Unsupported objects are rejected and duplicates are ignored.

### Source Folders

A palette can reference multiple project folders. Use Add Folder or the drop area to add them. Use Remove to exclude a folder without touching its assets.

### Individual Sprites

Individual Sprite assets can be mixed with folder sources. This is useful for small curated palettes or sprites spread across unrelated folders.

### Palette Defaults

Enable **Use Palette Defaults** to apply a shared placement configuration whenever this palette is selected.

Available defaults:

- Scale
- Z Rotation
- Z Position
- Color
- Material
- Sorting Layer
- Order in Layer
- Flip X
- Flip Y
- Overlapping Auto Order
- Grid Snap
- Grid Size

Select **Save** to commit the palette sources and settings. Use **Cancel** to close the editor when no changes should be kept.

Deleting a palette removes only its definition. Sprite assets are never deleted.

## 3. Collections

### All Sprites

Contains the de-duplicated union of all named palette sources. It does not scan unrelated project folders.

### Favorites

Contains sprites marked with the star action below a thumbnail. Favorites are personal to the current Unity user.

### Recent

Contains up to 24 sprites ordered by most recent selection. Selecting an existing recent sprite moves it back to the beginning.

### Named palettes

Named palettes are shared through ProjectSettings and display their resolved sprite count in the wide sidebar.

Switching palettes stops the current placement, applies palette defaults when enabled, reloads sprites and updates the window.

## 4. Adding sprites from the browser

Every sprite card contains:

- A thumbnail button that starts placement
- The sprite name
- A favorite star
- A **+** action

The **+** menu offers:

- **Create New Palette From This Sprite**
- **Add to Existing Palette/<palette name>**

A target palette is disabled in the menu if the sprite is already included directly or is covered by one of its source folders.

## 5. Searching and previews

Search matches the Sprite name using a case-insensitive substring comparison.

Preview Size ranges from 50 to 150 pixels. Grid columns are calculated from the available window width.

For sprites inside a multi-sprite texture, the preview uses the individual sprite texture rectangle instead of displaying the entire source texture.

## 6. Starting and stopping placement

Click a sprite thumbnail to:

1. Make it the active sprite.
2. Add it to Recent.
3. Load its saved preset when preset loading is enabled.
4. Create a non-saved preview GameObject.
5. Begin listening to Scene view input.

Stop with:

- Escape
- Right-click
- The **Stop Placement** button
- Selecting another palette
- Closing or disabling the Sprite Palette window

The preview is hidden and is never saved into the scene.

## 7. Source and Target section

### Parent Object

When assigned, every placed GameObject is parented to this Transform using Unity Undo. The target description shows the parent scene and object name.

If no parent is assigned, the tool places the GameObject into the current Unity stage:

- Current Prefab Stage while editing a prefab
- Active scene otherwise

### Palette Sources

For a named palette, the window reports folder and individual-sprite counts. Built-in collections are described as combined or automatic collections.

## 8. Rendering section

### Sorting Layer and Order in Layer

The new SpriteRenderer receives the selected Sorting Layer and numeric Order in Layer.

### Overlapping Auto Order

After creating the object, the tool compares its world-space 2D renderer bounds with other enabled SpriteRenderers.

- **Disabled**: keeps the configured order.
- **Bring Forward**: highest overlapping order plus one.
- **Send Backward**: lowest overlapping order minus one.

Only renderers in the same scene and Sorting Layer participate. The preview object, disabled renderers and renderers without sprites are ignored. Integer overflow is clamped.

This is a bounds-based layout convenience, not a physics or collider test.

### Color, Material and flips

Color, shared material, Flip X and Flip Y are applied both to the translucent preview and to created SpriteRenderers. If Material is empty, placed renderers retain Unity's default sprite material.

## 9. Transform section

### Scale

Controls local X and Y scale. Z scale remains 1.

### Z Rotation

Controls rotation about the Z axis. Shortcut rotation wraps between 0 and 360 degrees.

### Z Position

Defines the placement plane. The Scene view mouse ray is projected onto an XY plane at this Z value.

### Object Name

When empty, the created GameObject uses the Sprite name. Otherwise it uses the custom name.

## 10. Placement section

### Grid Snap and Grid Size

When enabled, X and Y are rounded to the nearest Grid Size increment. Z remains the configured Z Position. Grid Size cannot be below 0.01.

### Rotation Step

Controls the amount used by Rotate Left and Rotate Right. Minimum value: 0.1 degrees.

### Resize Step

Controls percentage scaling. Increase Size multiplies the current scale by the percentage factor; Decrease Size divides by the same factor. Valid range: 0.1 to 100 percent.

### Reset Properties

Restores:

- Default Sorting Layer
- Order in Layer 0
- Bring Forward overlap mode
- White color
- No custom material
- No flips
- Scale 1, 1
- Rotation 0
- Z Position 0
- Empty object-name override

### Copy From Selection

Requires the selected GameObject to have a SpriteRenderer. It copies:

- Sorting Layer and Order in Layer
- Color and shared material
- Flip X and Flip Y
- Local X and Y scale
- Z rotation
- World Z position
- GameObject name

## 11. Scene view shortcuts

| Action | Default binding | Effect |
| --- | --- | --- |
| Rotate Left | Q | Subtracts Rotation Step |
| Rotate Right | E | Adds Rotation Step |
| Increase Size | Shift + Equals | Multiplies by Resize Step |
| Decrease Size | Minus | Divides by Resize Step |
| Bring Forward | Page Up | Adds one to Order in Layer |
| Send Backward | Page Down | Subtracts one from Order in Layer |
| Place | Left-click | Creates a SpriteRenderer GameObject |
| Stop | Escape or right-click | Ends placement |

Keyboard actions are handled only while Sprite Palette placement is active. This prevents conflicts with Unity's global Scene view shortcuts, and the overlay displays the active controls.

Alt + left-click is left available for normal Scene view navigation.

## 12. Per-Sprite Preset section

A preset is keyed to the exact Sprite asset, including a sub-sprite identifier where applicable.

### Load Presets

When enabled, selecting a sprite loads its saved fields. Placing that sprite also saves its current values.

### Remember options

- **Remember Scale**: local X and Y scale
- **Remember Rotation**: Z rotation and Z position
- **Remember Rendering**: color, material and both flip values
- **Remember Sorting**: Sorting Layer and Order in Layer

Disabled groups remain unchanged when a preset is loaded.

### Save and Forget

**Save Preset** writes the current values for the active sprite. **Forget Preset** removes that sprite's record. The status line reports whether the active sprite has a saved preset.

## 13. Palette defaults versus sprite presets

The application order is:

1. Selecting a named palette applies its defaults.
2. Selecting a sprite loads that sprite's enabled preset fields.
3. Manual changes in the window or through shortcuts update the current placement.
4. Placing the sprite updates its preset when Load Presets is enabled.

This allows a palette to define a common baseline while individual sprites retain exceptions.

## 14. Created objects and Undo

A placed object receives:

- A GameObject name
- A SpriteRenderer
- The selected sprite
- Rendering settings
- Transform settings
- Optional parent

Object creation and parenting are registered with Unity Undo. The newly created object becomes the active Selection.

## 15. Stored data

### Shared project data

    ProjectSettings/SpritePaletteProjectSettings.asset

Contains named palettes, folder and sprite references, palette defaults and per-sprite presets. Commit this file when the definitions should be shared by a team.

### Per-user data

    UserSettings/SpritePaletteUserPreferences.asset

Contains the active palette, search, preview size, placement steps, foldout state, favorites and recents. It is normally excluded from version control.

Asset references use GUID-based keys, including local identifiers for sub-sprites. Moving an asset inside the project therefore keeps the reference as long as its .meta GUID remains unchanged.

## 16. Current scope

- Creates SpriteRenderer GameObjects only.
- Places sprites on an XY plane at the configured Z position.
- Searches by sprite name only.
- All Sprites means all sprites referenced by named palettes.
- Automatic overlap ordering uses renderer bounds, not colliders.
- The package is Editor-only and adds no runtime component.

## 17. Troubleshooting

### A folder contains no sprites

Check **Include Subfolders**. When disabled, nested folders are intentionally skipped.

### A sprite is missing from Favorites or Recent

The original asset may have been deleted or its GUID may have changed. Refresh the palette after restoring the asset.

### Shortcuts do not respond

Make sure a sprite is currently active for placement and that the Scene view has keyboard focus.

### Copy From Selection shows a dialog

Select a GameObject containing a SpriteRenderer before using the command.

### Git URL installation fails

Confirm Git is installed and available on PATH, the repository is accessible, and the requested version tag exists.
