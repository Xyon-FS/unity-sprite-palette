# Sprite Palette

Sprite Palette is an Editor-only Unity tool for organizing project sprites into reusable collections and placing SpriteRenderer GameObjects directly in the Scene view.

It is designed for level-design workflows where sprites need to be found, previewed and placed repeatedly with consistent transform and rendering settings.

## Highlights

- Named palettes built from sprite folders and individual sprites
- Built-in **All Sprites**, **Favorites** and **Recent** collections
- Searchable, resizable sprite browser
- Direct add-to-palette actions from every sprite thumbnail
- Live placement preview in the Scene view
- Grid snapping, custom Z position and optional parent object
- Sorting Layer, Order in Layer, color, material and flip controls
- Automatic sorting order for overlapping sprites
- Rotation, resizing and sorting shortcuts during placement
- Per-palette default placement settings
- Per-sprite transform, rendering and sorting presets
- Undo support, Prefab Mode support and multi-scene-safe placement
- Project-shared palette data with per-user interface preferences

## Requirements

- Unity 2022.3 or newer
- Git 2.14 or newer when installing from a Git URL

The package contains Editor code only and does not add runtime components to a build.

## Installation

### Unity Package Manager

1. Open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

       https://github.com/Xyon-FS/unity-sprite-palette.git#v1.2.0

4. Select **Add**.

Using a version tag is recommended so the project receives a predictable package revision.

### Project manifest

The package can also be added to the dependencies object in Packages/manifest.json:

    "com.whatidotoday.sprite-palette": "https://github.com/Xyon-FS/unity-sprite-palette.git#v1.2.0"

## Quick start

1. Open **Tools > Sprite Palette**.
2. Select **New Palette**.
3. Give the palette a name.
4. Drag sprite folders or individual sprites into the palette editor, or add them through the object fields.
5. Save the palette.
6. Select a sprite thumbnail to start placement.
7. Move the cursor in the Scene view and left-click to create copies.
8. Press Escape or right-click to stop placement.

Deleting a palette never deletes its sprite assets.

## Window overview

The window automatically adapts to its width.

- In the wide layout, built-in collections and named palettes appear in a sidebar.
- In the compact layout, the same collections are available from the **Palette** dropdown.
- **Refresh** reloads sprite references and collection counts.
- **New Palette** creates a named palette.
- **Edit** and **Delete** operate on the selected named palette. They are disabled for built-in collections.

The main area contains collapsible **Source and Target**, **Rendering**, **Transform**, **Placement** and **Per-Sprite Preset** sections, followed by the Sprite Browser.

## Palettes and collections

### Named palettes

A named palette can include any combination of:

- Source folders
- Individual Sprite assets
- Sprites dragged directly into the palette editor

Enable **Include Subfolders** to include sprites below each selected source folder. When it is disabled, only sprites located directly inside the selected folders are loaded.

Folders and sprites can be dragged together into the drop area. Duplicate references are ignored.

### Built-in collections

- **All Sprites**: the union of sprites contained in all named palettes
- **Favorites**: sprites marked with the star button
- **Recent**: the 24 most recently selected sprites

**All Sprites** does not scan every Sprite asset in the Unity project. It combines the contents of the named palettes.

### Adding from the browser

Each thumbnail has two small actions:

- The star toggles the sprite as a favorite.
- The **+** button opens a menu for creating a new palette from that sprite or adding it to an existing palette.

A sprite already included directly or through one of the palette folders cannot be added to that palette again.

## Sprite Browser

- **Search** filters the active collection by sprite name, without case sensitivity.
- **Clear** removes the current search.
- **Preview Size** changes thumbnail size from 50 to 150 pixels.
- The grid automatically changes its column count when the window is resized.
- Clicking a thumbnail selects it and begins placement.

Multi-sprite textures are supported because every Sprite sub-asset found at an asset path is loaded individually.

## Placement settings

### Source and Target

- **Palette Sources** reports the number of folders and individual sprites in the selected palette.
- **Parent Object** optionally parents every newly created GameObject to a selected Transform.
- **Placement Target** shows the active scene, selected parent, or current Prefab Stage.

Without a parent, objects are placed in the current stage. This makes the tool safe to use while editing a prefab in Prefab Mode.

### Rendering

- **Sorting Layer**
- **Order in Layer**
- **Overlapping Auto Order**
- **Color**
- **Material**
- **Flip X**
- **Flip Y**

Overlapping Auto Order has three modes:

- **Disabled** keeps the configured Order in Layer.
- **Bring Forward** uses one order above the highest overlapping SpriteRenderer.
- **Send Backward** uses one order below the lowest overlapping SpriteRenderer.

Only enabled SpriteRenderers in the same scene and Sorting Layer are considered. Overlap is evaluated from their 2D world bounds.

### Transform

- **Scale** controls X and Y local scale.
- **Z Rotation** controls rotation around the Z axis.
- **Z Position** selects the XY placement plane.
- **Object Name** optionally overrides the sprite name used for created GameObjects.

### Placement

- **Grid Snap** snaps X and Y to the configured grid.
- **Grid Size** controls spacing and has a minimum value of 0.01.
- **Rotation Step** controls shortcut rotation increments.
- **Resize Step (%)** controls shortcut scale increments.
- **Reset Properties** restores standard placement values.
- **Copy From Selection** copies Transform and SpriteRenderer values from the selected GameObject.

Copy From Selection reads scale, Z rotation, Z position, object name, Sorting Layer, Order in Layer, color, material and flip values. The selected GameObject must contain a SpriteRenderer.

## Scene view controls

Default shortcuts while a sprite is active:

| Action | Default input |
| --- | --- |
| Place a sprite | Left-click |
| Stop placement | Escape or right-click |
| Rotate left | Q |
| Rotate right | E |
| Increase size | Shift + Equals |
| Decrease size | Minus |
| Bring forward | Page Up |
| Send backward | Page Down |

The rotation and resize amounts come from the Placement section. Sorting shortcuts change Order in Layer by one.

These controls are handled only while Sprite Palette placement is active, preventing conflicts with Unity's global Scene view shortcuts. The Scene view overlay displays the active controls.

Holding Alt while left-clicking does not place a sprite, allowing normal Scene view navigation.

## Per-sprite presets

Presets remember placement properties for a specific Sprite asset.

- **Load Presets** loads an available preset when the sprite is selected.
- **Remember Scale** stores scale.
- **Remember Rotation** stores Z rotation and Z position.
- **Remember Rendering** stores color, material, Flip X and Flip Y.
- **Remember Sorting** stores Sorting Layer and Order in Layer.
- **Save Preset** saves the current values for the active sprite.
- **Forget Preset** removes its saved preset.

When Load Presets is enabled, placing a sprite also updates its preset using the enabled Remember options.

## Palette defaults

A named palette can optionally provide defaults that are applied whenever the palette is selected:

- Scale
- Z rotation and position
- Color and material
- Sorting Layer and Order in Layer
- Flip X and Flip Y
- Overlapping Auto Order mode
- Grid Snap and Grid Size

Palette defaults provide a starting configuration. Per-sprite presets can then override the fields they are configured to remember when a sprite is selected.

## Undo and object creation

Every placed item is a new GameObject with a SpriteRenderer. Creation and optional parenting are registered with Unity Undo, so normal Undo removes placed objects.

The newly placed object becomes the active Unity selection.

## Data storage

Shared project data is stored in:

    ProjectSettings/SpritePaletteProjectSettings.asset

This includes named palette definitions, palette defaults and per-sprite presets. Commit this file when palettes should be shared with the team.

Per-user data is stored in:

    UserSettings/SpritePaletteUserPreferences.asset

This includes favorites, recent sprites, active palette, search text, preview size, foldout state and placement preferences. UserSettings is normally not committed.

The tool stores asset identifiers and does not copy or move source sprites.

## Detailed documentation

The complete field-by-field guide is available in [Documentation~/index.md](Documentation~/index.md).

## License

Sprite Palette is available under the [MIT License](LICENSE.md).
