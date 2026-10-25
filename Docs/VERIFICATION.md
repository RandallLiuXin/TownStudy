# HDRP / original-module verification — 2026-09-24

Environment: Unity 6000.3.24f1, HDRP 17.3.0, Windows, New Input System 1.20.0, uGUI 2.0.0. Packages are pinned in manifest and lock. Hardware in editor log: RTX 3080, Direct3D 12. Reference projects were read-only. Service-side model/evaluator version is not inferred.

## Current evidence

- Reports/source-verification.txt: 24,671 checks passed; 19,834 directed original structural interfaces; 20 random layouts, full 448-cell board, 30 edit/undo/redo cycles, exact save roundtrip, invalid-load atomicity, side projection and active HDRP. 192 original-associated decorations in the demo, checked against original overlap and neighbor support constraints. The latest elapsed time is recorded in the report.
- Reports/builder-play-check.txt: 11 actual Input System/uGUI event checks passed; zero runtime errors. Checks left build, right erase, floating geometry, undo/redo, toolbar input, UI blocking and duplicate generated-tree prevention.
- The synthetic mouse test temporarily enables focus-independent input in the test harness. It is not a human mouse retest.
- Reports/module-extraction.json: 2,072 original structural modules, 128 decor modules, 4,769 corner lookups, 14,519 side lookups and 892 original mesh pieces. The side-view subset uses 521 oriented structural candidates and 147 decoded pieces including decor.
- Independent read-only verification: corner rotations/reflections tested across 690 source modules with zero conflicts. Native code confirmed position /254, UV /256, prop scale /128 and rotation 360/256. Original overlap hashes independently checked over 6,162 candidate pairs with zero differences. Original decor interface zero semantics and multi-attachment behavior were independently audited.
- Reports/hdrp-build.log: Windows HDRP build succeeded: PLAYER_BUILD_SUCCESS and process exit 0. Do not confuse previous builder-release-build.log with the HDRP build.
- Assets/Reports/hdrp-window-review.png: actual HDRP Game View including HUD. Screenshots from earlier passes are retained as history, not final evidence.

## Failures and fixes retained

The first full-grid check exceeded the solver deadline. Reports/source-verification-failure.txt retains that failure. Propagation now starts from the changed domain after each collapse instead of rescanning all slots. The same full-grid test subsequently passed; typical full suite fell from roughly seven seconds to below one second.

Pipeline eval calls have a five-second main-thread timeout. Some asset creation and initial full-suite requests exceeded it; logs retain those infrastructure errors. Completion is determined from dated output reports, not the CLI timeout alone. A seeded PlayerConnection multicast warning/error 10013 is unrelated to this offline demo.

## Intentional adaptations and limitations

Original module geometry and constraints are recovered data. The solver, ordering, rectangular dual grid, vehicle bottom boundary and scheduling are new implementations. Original irregular quad deformation, full prop modifiers, complete prefab hierarchy and original shader stack are not restored. Recessed windows are moved slightly outward to display without the original stencil wall-cutout shader. House palette, AFK-inspired light rig and Aniimo-inspired gradient are HDRP adaptations. Original Shader visual equivalence is not claimed.

The chassis and wheels are visual placeholders; no vehicle physics. Fast drags can skip cells; narrow windows can clip the fixed toolbar. Human visual acceptance remains pending, independently of automated PASS results.

Final standalone smoke: hdrp-player-smoke.log shows Direct3D 12 / RTX 3080 initialization, scene load and responsive process after startup; no managed exceptions or shader errors were logged. The graphics info-queue query warning 0x80004002 did not prevent device creation. The hidden smoke player had no main window handle, so its verified process was stopped after checking. Final Unity review editor PID 45444 reopened the scene and entered Play.
