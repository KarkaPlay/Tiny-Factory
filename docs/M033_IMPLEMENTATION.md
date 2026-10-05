# M03-R03 — authoring refactor 0.3.3

**Status:** implementation and Android build complete; device verification pending.

## What changed

`SampleScene` now authors the factory, UI, materials, prefabs, anchors and event wiring in the Editor. Runtime components consume serialized references and update simulation-facing visuals; they no longer construct the factory or HUD. The gameplay model, balance and M03 production rules were preserved.

- `FactoryRuntime` owns deterministic simulation and reads `FactoryBalanceConfig` from its serialized scene reference. The existing loop remains: sale value +4, automatic harvest every 12 seconds, dryer upgrade cost 36 coins (3 s → 2 s), and buffer capacity 8.
- `FactoryHudView` owns HUD projection, safe-area and camera-aspect adjustment, persistent button handlers and feedback tweens.
- `FactoryProductionLineView` projects runtime state onto authored world anchors, instantiates the assigned item prefab for runtime items, and owns bounded transport/progress tweens.
- `FactoryItemVisual` switches the leaf/package child renderers on the authored item prefab.

The authored assets live under `Assets/TinyFactory/`: `Prefabs/` contains TeaBush, DryerStation, PackagerSaleStation, ConveyorSegment, TeaItem and the two UI card/button prefabs; `Materials/` contains the editable URP material palette; `Fonts/RobotoCyrillic.asset` is the baked TMP font used by the HUD. `SampleScene` keeps the camera and scene pose as authored, with Runtime, factory/world, production-line view, Canvas/HUD and the existing EventSystem/InputSystem UI module wired in the Inspector. The factory is visible in Edit Mode without entering Play.

`TeaItem.prefab` is the reusable item. Its leaf and packaged-tea children retain distinct authored meshes/materials; package display is selected from runtime stage state. Station cards and action buttons can be edited as prefab assets, while scene overrides hold the specific placement, text, scene targets and anchors.

## Editing guide

1. Open `Assets/Scenes/SampleScene.unity` to move stations, conveyor segments, queue slots, processing anchors, progress-bar references and HUD panels. The camera fit values are serialized on `FactoryHudView`; change them there if the authored factory width changes.
2. Edit color and surface appearance in `Assets/TinyFactory/Materials/`, then assign the material to the relevant renderer in its prefab. The scene uses explicit shared materials and does not rely on Unity primitive defaults.
3. Edit factory shapes in the prefabs under `Assets/TinyFactory/Prefabs/`. Edit the leaf/package child visuals in `TeaItem`; keep `FactoryItemVisual` child references assigned.
4. Edit HUD layout and default preview text in the Canvas hierarchy or the `HarvestButton` / `SpeedUpgradeCard` prefabs. Runtime-filled labels show useful startup preview text in Edit Mode and receive live values in Play Mode. Text uses `RobotoCyrillic.asset`.
5. The two scene Button components have saved persistent UnityEvents: Harvest → `FactoryHudView.HandleHarvestButton`; Speed → `FactoryHudView.HandleSpeedUpgradeButton`. Keep the methods and target references assigned in the Button Inspector. The EventSystem uses the existing UI action references embedded in `Assets/InputSystem_Actions.inputactions`; six redundant standalone InputActionReference assets created during migration were removed.
6. Balance values remain in `Assets/TinyFactory/Resources/FactoryBalanceConfig.asset`, assigned to the scene `FactoryRuntime` field. The Resources location is retained for compatibility, but scene composition does not load it dynamically.

## Verification evidence

- Unity `6000.3.9f1`, project `Tiny Factory@4b2dafe5`. Unity changes were made through Unity MCP. Script formatting changes were whitespace/indentation only; the gameplay implementation was not changed for this refactor.
- After refresh/compile, Unity reported `is_compiling=false`; Console query returned no Unity errors or warnings (only MCP reconnect diagnostics). `SampleScene` validation: 0 issues, 0 missing scripts, 0 broken prefabs.
- Persistent button targets were saved, the scene was reopened, and each Button retained one persistent listener with the expected handler. All six InputSystem UI actions resolved after reload.
- Synthetic Play lifecycle smoke after Play re-entry: two scene Harvest UnityEvent invocations produced runtime in-flight count 2 and visual count 2. Disabling the line view during one manual runtime tick and re-enabling it reconciled back to visual count 2. Scene contained exactly one `FactoryProductionLineView` and one `FactoryHudView`; Harvest persistent listener count was 1. This was an Editor synthetic check, not device touch input.
- Three actual Play GameView startup captures were made at the selected exact resolutions: [9:16, 540×960](../Assets/Screenshots/M033/m033-final-startup-9x16.png), [19.5:9, 432×936](../Assets/Screenshots/M033/m033-final-startup-19_5x9.png), and [20:9, 405×900](../Assets/Screenshots/M033/m033-final-startup-20x9.png). They show the authored HUD and station tags fitting without overlap. These captures do not establish device safe-area or hardware rendering behavior.
- Android build succeeded: Unity MCP job `build-80e2f02fda`, Android, 0 errors / 1 warning, reported uncompressed build size 740.64 MB. Artifact: [`TinyFactory-0.3.3-android.apk`](../Builds/TinyFactory-0.3.3-android.apk), 40,406,317 bytes; SHA-256 `986f6634a9dd7e4d1efc4f83845ad32df8d200336d24dec4aef15de98c1f3011`. Build settings were version 0.3.3, Android bundle code 9, bundle ID `com.tinyfactory.prototype`; Unity API readback confirmed `PlayerSettings.Android.targetArchitectures = ARM64`.
- The Unity MCP Editor instance disconnected immediately after the successful build (`instance_count=0`). Therefore the warning text, post-build Console/scene state and `BuildReport.packedAssets` could not be read back. Do not treat source dependencies or the successful build as proof that the material, shader or font were present in PackedAssets.
- The APK was not installed or launched on hardware. Device rendering, safe areas, touch input, background/resume and performance remain unverified.

## Follow-up

Producer/QA can reconnect Unity to retrieve the last BuildReport warning and PackedAssets evidence, then request user device verification. If the build warning or stripping report identifies a missing dependency, make the smallest scoped fix and rebuild with a higher Android bundle code. A full tween pause-position and OnDestroy cleanup test was not run in this final compact smoke; current lifecycle code pauses/kills the owned tweens, but the behavior is not asserted here.
