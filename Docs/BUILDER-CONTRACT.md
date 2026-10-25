# Side-view builder contract

Unity 6000.3.24f1, HDRP 17.3.0, Windows; single-depth XY occupancy, fixed orthographic view.

- Use the local original corner candidate table, oriented meshes and two-sided face profiles. Do not invent structural adjacency rules.
- Keep empty-corner domains to support arches, overhangs and floating buildings.
- Adapt the bottom boundary to a chassis; remove the ground/track strip and retain placeholder wheels.
- Use original associated decor candidates, zero-interface semantics and original overlap table. All surviving nonzero interfaces must have adjacent support.
- Edits are transactional. Contradiction or bounded solve failure preserves the previous layout.
- Support add/erase, toolbar UI blocking, zoom/pan, undo/redo and exact structural save roundtrip.
- Verify random layouts, full bounds, original face compatibility, decor support/overlap and actual Input System mouse events.
- Use the local AFK lighting and Aniimo gradient as references, with adapted HDRP materials.
- Keep reference projects read-only. No commit/push.
- Report original-data reuse separately from independently written solver/scheduling and visual adaptation. Automated evidence does not replace human visual acceptance.
