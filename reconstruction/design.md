# Design: Townscaper capture adaptation

## Observed target
Reference: evidence-001/492-final.png, replayed frame823, 2560x1440. Building surfaces are saturated coral/red with large clean brick color patches; roofs are light golden orange with visible tile divisions. Narrow dark blue/purple edges separate surfaces, with warm pale eaves and window frames, dark teal glass. Supports are cool dark grey, not wall-colored. Lit faces remain bright with cool shadow contrast. Background is a desaturated teal atmospheric field. The current Unity image is dull orange, mottled, over-tonemapped, with box-like tinted windows and a glowing cursor.

## Priorities and allowed differences
1. Correct atlas/palette decoding and brightness; no cursor glow.
2. Legible pale windows, gold roofs, cool supports and restrained edge contrast.
3. Restore the floor/platform and remove all wheels/hubs.
Keep the side-view orthographic camera, WFC and interactive layout. Exact captured pose/camera, birds, water reflection, original UI and irregular grid deformation are not required for this vehicle demo. Do not substitute the reference screenshot as a background.

## Evidence and hypotheses
Confirmed draw185 binds House, House_Type, palette resource526 and shadow target599. Pixel instructions31-47 derive a color from palette, type.g, source luminance and source alpha; the old hand-painted palette did not reproduce that operation. Vertex shader encodes edge information in UV.y. Original volume lighting/shadow maps are scene dependent and cannot simply be pasted into the editable scene.
Need captured constants, palette and filtering before implementing the material. Rebuild readable parameterized HDRP shading; explicitly label dynamic lighting/AO/edge adaptations versus recovered formulas.

## Stages
Inspect bindings/constants; fix cursor via post-processing-free overlay and replace wheels with platform; implement captured color decode and lighting adaptation; capture actual Unity images and verify input/runtime/scene reopen. Record automated results separately from user visual acceptance.
