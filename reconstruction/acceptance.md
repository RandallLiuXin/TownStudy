# Capture-based rendering handoff

## Delivery
Project: E:/others/TownStudy. Unity 6000.3.24f1, HDRP 17.3.0, Windows D3D12.
Scene: Assets/Scenes/SideViewBuilder.unity. Standalone: Builds/CastleWorkshop/CastleWorkshop.exe.
The single-depth interactive builder and original-data constraints remain unchanged.
Before: Reports/hdrp-preview.png. Reference: evidence-001/492-final.png. After: ../Assets/Reports/rdc-window.png (actual Game View).

## Provenance
| Part | Method | Evidence | Limits |
|---|---|---|---|
| House/Type/Palette | Extracted, vertically converted for Unity, imported as linear uncompressed data | bindings-001/texture-187.png, 208, 526 | Personal authorized capture |
| House colors | Reconstructed double alpha blend and roof type blend | EID185 PS31-47 | No claim of full shader equivalence |
| Palette and outlines | Original module corner/orientation and packed outline byte decoding | Recovered module data; EID185 VS | Side-view occupancy chooses indices 0/18; fence uses 15 |
| Lighting | Captured sun/ambient constants and color grading | EID185 PS107-122 | Constant occlusion replaces spatial volume light; no original shadow map reconstruction |
| Windows | Discard negative-U helper faces | EID261 PS31-32 | Props use approximate atlas lighting; window surface is moved forward instead of full stencil wall holes |
| Cursor | New ScreenSpaceOverlay Canvas with four nonblocking borders | BuilderController and live input tests | Fixed two-pixel border |
| Floor | Restored HDRP/Lit platform; retained chassis; removed wheels/hubs | Scene and live input tests | No driving physics |

## Validation
Current-round automated reports: Reports/builder-play-check.txt (18 checks, zero runtime errors), Reports/source-verification.txt (24,671 checks, 19,834 original socket edges, 20 layouts, full 448-cell grid, 30 edit/undo/redo cycles). Both passed after the rendering changes.
Compilation completed with no C# errors. SourceSceneSetup ran through live Unity APIs and saved the scene. An initial CopySerialized update retained stale GPU vertex layout; Clear(false) plus explicit channel writes corrected this, confirmed by attribute-debug screenshots and a fresh scene/play transition.
Independent read-only review inspected the shaders, cursor path, floor/wheels, and actual screenshot. Its two suggestions (clear old vertex layout and skip house edge decoding on props) were applied before the final regression run.
Build and reopen evidence will be appended below after completion.
User visual acceptance: PENDING. Agent screenshots and automated checks are not user approval.

## Remaining differences
The captured perspective layout, irregular ground grid, water, reflections, birds and original UI are outside this side-view vehicle prototype. Spatial AO/shadows, full prop shading and stencil cutouts are not exact reconstructions. This is an editable rendering adaptation, not a pixel-identical replay of the captured scene.

Final verification: Reports/capture-build.log contains PLAYER_BUILD_SUCCESS and exit code 0 (21:43). Reports/capture-player-smoke.log confirms D3D12 startup on RTX3080, driver32.0.16.1692, without managed exceptions or missing shader messages. This is a startup smoke check, not standalone interaction coverage; interactive tests ran in Editor. The helper player was closed after the smoke check.
Unity was reopened and entered Play successfully. Assets/Reports/capture-final.png is the fresh Game View after disk reload; colors and windows persisted. Assets/Reports/cursor-a.png demonstrates the thin screen overlay without a halo. The attempted two-position pixel comparison was discarded because board content changed between captures; it is not counted as brightness-equivalence evidence. Runtime focus routing was restored and the temporary synthetic mouse removed. Existing interaction state was preserved.
Reopen diagnostics: one editor PlayerConnection multicast discovery warning/error (10013) was seeded in CLI console. Compilation remained successful and the Game View/local CLI worked. This is editor connection infrastructure, not a building-script exception; no claim is made that every editor infrastructure message is absent.
