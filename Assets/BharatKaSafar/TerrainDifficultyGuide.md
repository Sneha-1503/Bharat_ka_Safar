# Terrain Difficulty V1

Open **Bharat Ka Safar → Terrain Difficulty Builder**.

The current mission planner supplies the 28-state order. Each state has five plans. Existing mission IDs, stories, scenes and progression data are preserved. The new `Terrain Plan` field connects each MissionData asset to its terrain definition; it does not replace an authored scene automatically.

| State order | Missions 1–5 |
|---|---|
| 1–7 | Easy, Easy, Medium, Easy, Medium |
| 8–14 | Easy, Medium, Medium, Medium, Hard |
| 15–21 | Medium, Medium, Hard, Hard, Hard |
| 22–28 | Medium, Hard, Super Complex, Hard, Super Complex |

Total: 28 Easy, 56 Medium, 42 Hard, 14 Super Complex.

## Assets

- `Data/TerrainDifficulty/Plans`: 140 individually editable mission plans.
- `Data/TerrainDifficulty/Sections`: 24 reusable definitions: 6 Easy, 8 Medium, 6 Hard, 4 Super Complex.
- `Prefabs/Terrain/Progression`: 140 route prefabs. Drag one into an empty scene; mesh and continuous 2D collider rebuild in the editor and at runtime.
- `Data/TerrainDifficulty/TerrainDifficultyManifest.csv`: mapping of planner index, state order, mission, difficulty and existing mission GUID.

Plans share sections. Editing a section changes every plan using it; duplicate a section before making a mission-specific variation. Materials can be changed per plan using existing regional materials. Reference terrain prefabs are retained on section definitions for designers; the generated route uses continuous geometry instead of unsafe concatenation of untested colliders.

## Terrain rules

Every route starts with 220 flat units and ends with 100 flat units. Challenge pieces return to the same height with zero entry/exit slope. Easy routes have frequent long recovery roads; later tiers have shorter recovery cadence. The first challenge of a higher-tier mission is one tier easier as a warm-up. Slopes are bounded by each section: Easy up to 12°, Medium up to 22°, Hard up to 30°, Super Complex up to 36°. These are starting design limits, not validated vehicle capabilities.

V1 deliberately keeps the main road grounded. Mandatory gap jumps, dynamic bridges and optional branching routes require authored sections and vehicle testing before release. Difficulty labels do not imply that fuel, collectibles, obstacles or completion objectives have been balanced.

## Test and publish workflow

1. Click **Select Tamil Nadu Easy Reference**. It selects mission #1 in the CURRENT chapter order, which may differ from legacy scene/file numbering.
2. **Create Playable Terrain Draft Copy** creates and opens a new copy of that mission's gameplay scene. Source scenes and build settings are preserved.
3. The draft replaces ground in the copy, aligns new terrain below the vehicle, moves the finish target, assigns MissionData and gives generous fuel. Old template pickups and checkpoints are disabled because they belong to the old road. Other template scenery/obstacles may still need relocation. This is a terrain driving test, not a fully balanced mission.
4. Drive the default vehicle on slopes, crests and valleys. Check crashes, visibility, recovery space and traversal without upgrades. Adjust section slopes/lengths, then rebuild.
5. Place checkpoints and mission pickups on the new route. Rebalance fuel and objectives, apply regional materials/art and verify result/unlock flow.
6. Mark **Playtested** and record the vehicle/settings in Test Notes after actual testing. Add the approved draft to build settings and update its MissionData gameplay scene through the existing mission workflow.

**Validate 140 Plans and Slopes** checks catalogue links, difficulty distribution, sample ordering and slope bounds. It is separate from vehicle physics testing. **Add Terrain Preview** leaves existing terrain intact, so use an empty scene for a visual preview or use the draft action for a driving test.
