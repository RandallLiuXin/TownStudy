# TownStudy rendering reconstruction

Target: E:/others/TownStudy, Unity 6000.3.24f1, HDRP 17.3.0, Windows D3D12. Keep interactive single-depth side-view builder and original-data WFC.
User corrections: remove wheels, restore floor/chassis presentation; remove cursor light/glow; reconstruct building rendering from the newly captured original Townscaper frame.
Input: captures/Townscaper-frame823.rdc, copied from user's RenderDoc temporary capture (97 MB), D3D11 replay verified. SHA256: f14eb9610eafe0b6774ac8b70d207c0e77870d9f50db7dab5a1d7bfa73e44849. Captured 2026-09-24 21:15 local, frame823, 2560x1440. Existing recovered modules/textures are authorized inputs.
Scope: reusable runtime building shader/material and lighting adaptation, not a fixed image. Side-view camera and layout may differ from captured camera. Reference UI need not be reproduced. Source projects, original capture, WFC logic and skill repository stay unchanged.
Tools: RenderDoc 1.46 qrenderdoc native Python replay verified through skill runner; no RenderDoc MCP exposed. Unity Pipeline CLI target verified port7800 PID45444. No installs or global hooks required.
Visual acceptance: user review pending; automatic checks cover rendering setup, cursor, wheel removal, scene load and input regressions. Compare actual before/after game captures.
Current: shader/material adaptation and input fixes implemented; live input and original constraint regression passed. Standalone build, startup smoke and Editor reopen verified. User visual acceptance pending. See acceptance.md and operations.md.


