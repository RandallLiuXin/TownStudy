# TownStudy / Castle Workshop

Unity 6000.3.24f1 / HDRP 17.3.0 / Windows. Personal learning prototype.

## Start

Clone this repository and open its folder in Unity Hub using Unity 6000.3.24f1. Open `Assets/Scenes/SideViewBuilder.unity`, then Play.
Standalone builds are generated locally at `Builds/CastleWorkshop/CastleWorkshop.exe` and are not included in Git.
Left click/drag builds; right click erases. Wheel zooms, middle drag pans, F fits the view.
Toolbar: Undo / Redo, Save / Load, Example, Clear, Remix, Fit. Ctrl+Z / Ctrl+Y also work.
Save location is displayed by `BuilderController.SavePath`; saves contain cells, seed and exact structural module placements.

## Current implementation

Single-depth XY grid (28 by 16), perspective side camera (50-degree field of view). Floating placements are allowed.
The original serialized ModuleLibrary supplies 32 side-view corner cases and 521 oriented structural candidates. Its compressed mesh data is decoded, rather than replacing each cell with a box. Six-face compatibility uses both original directional profile halves. Empty corner cases participate in solving, allowing original support and arch pieces below and between buildings. A floor/platform and chassis replace the original water/ground boundary. Wheels and hubs are removed.

Original associated decor modules supply windows and architectural attachments. Their zero interfaces are unconstrained; nonzero interfaces need neighboring decor support. The original hashed overlap table removes incompatible attachments in the same slot. Multiple compatible decorations may coexist. Cost/tie ordering and incremental scheduling are independently implemented, not recovered game logic.

The HDRP building shader reconstructs House atlas/palette mixing from the user's RenderDoc frame823, including recovered corner palette indices and packed outline flags. Warm sunlight and cool ambient constants come from that frame; spatial volume lighting/shadows remain approximations. Prop negative-U helper faces are discarded to expose the proper window faces. The backdrop adapts the local AniimoStage gradient. HDRP/Lit floor/chassis retain scene key/fill/rim lighting. Tonemapping and Bloom are disabled; the cursor is a post-processing-free screen overlay. Source projects are read-only. See `reconstruction/acceptance.md` for evidence and limits.

## Source and code

- `Tools/extract_module_catalog.py`: reproducible extraction from the user's local ModuleLibrary.
- `Assets/TownBuilder/Runtime/SourceWfc.cs`: original-data structural constraints and decor selection.
- `RecoveredCatalog.cs`: decoded meshes, transforms and original catalog.
- `BuildBoard.cs`: transactional edits, persistence, undo/redo.
- `BuilderController.cs`: screen projection, mesh placement and chassis.
- `Editor/SourceSceneSetup.cs`, `HdrpSetup.cs`: scene/material generation through Unity APIs.
- `Editor/BuilderSetup.cs`, `BuilderPlayCheck.cs`: deterministic and runtime input checks.
- `Reports/`: raw logs, reports and actual Game View captures.

The previous invented band/cross-brace rules and box geometry generator have been removed.
114 ordinary meshes, 35 PNGs and source material adaptations remain in Assets/Art. Architectural modules and decor pieces are separately decoded from ModuleLibrary. No original executable or stub scripts are imported.

References: local `E:\others\Townscaper-Recovered` serialized data and native-method analysis; [TownBuilder](https://github.com/eliemichel/TownBuilder); [WFC](https://github.com/mxgmn/WaveFunctionCollapse). TownBuilder is an educational reconstruction, not Townscaper source code.

## Limits

This is not a complete restoration: original irregular quad deformation, randomized prop modifiers, complete prefab child effects, stencil holes and the full original shader are not recovered. Rectangular side-view geometry and a chassis boundary intentionally adapt the source rules to this vehicle demo. There is no driving or physics simulation. Rapid dragging can skip cells. Very narrow windows may clip the fixed toolbar.

Unity Search automatic indexing remains disabled after the earlier indexing hang. Automated results do not replace the user's visual acceptance.

## Repository contents and resource provenance

This repository includes project source, scenes, settings, and study assets. Unity caches, builds, backups, raw logs, reference checkouts, and RenderDoc captures are excluded. Documentation may refer to local evidence files that are not published. Extraction tools require the original local inputs and are not needed to open the prepared scene.

The project contains third-party Townscaper-derived models, textures, and module data used in this learning prototype. These resources are not original artwork by this repository's author. Public repository visibility does not grant a license to reuse third-party resources; no blanket open-source license is applied to them. No original game executable is included.

