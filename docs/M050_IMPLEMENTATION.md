# M050 implementation handoff

**Status:** 0.5.0 implementation and Android APK build complete. Editor validation, authored button traces, required QA cases and bounded UI review passed. Physical-device verification remains with the user.

## Implemented

- Config-driven three-station production chain with shared capped warehouse, manual collect/load, station build/upgrades, two stable offers and one active order, free replaceable ordinary offer and atomic exactly-once turn-in.
- Schema2 state, validated backup/recovery, guarded v1 migration and durable receipt. Save paths derive from the injected test/save root. Only the exact fully empty `activeOrder` serialization sentinel from Unity `JsonUtility` normalizes to null; malformed/partial DTOs remain validation failures.
- Complete recipe loading starts WIP in the same transaction; dryer timing reaches output exactly eight foreground seconds later. Timer-only refresh updates status labels without destroying/rebuilding order rows. Foreground resume skips the first frame, resets partial time on pause/focus changes, and clamps foreground hitch delta to0.25s without changing global Unity time settings.
- Authored 3D Garden/Dryer/Packer plots and uGUI HUD; selected plot details and pinned transfer card; explicit Сбор/Загрузка modes with contextual previews/MAX/disabled states and separate action CTA. Ready output remains collectable while the next WIP runs. Russian product labels, action-gated tutorial, persistent UI state, UI pointer gesture blocking, migration notice with calculated versus credited refund, and restart-required persistence-block feedback.
- Cancel confirmation is visible above the offer rows; without an active order, cancel/turn-in controls are hidden. Disabled actions use a visibly unavailable style.

## Final Editor, QA and UI evidence

- Project was routed through Unity MCP to `Tiny Factory@4b2dafe5` (Unity6000.3.9f1). Current source compiled with0 C# errors; `Tiny Factory/Run M050 Validation` reported `M050 VALIDATION PASS`, including fresh/UI state, exactly-once order/cancel, WIP boundary and upgrade behavior, resume/hitch protection, persistence block, v1 migration and malformed/future-schema handling.
- Authored Button click traces: Dryer Load MAX `Загрузить 4` consumed four FreshLeaf, reserved the exact two-leaf recipe, and began WIP8; Dryer collection added four DryTea while preserving the subsequent WIP8; Packer collect added one TeaPacket; Packer Load MAX consumed/reserved one DryTea and began WIP6. Full-warehouse Garden correctly disabled `Забрать 0`; confirmed cancel cleared the active order and retained both offers.
- Migration cap fixtures verified refund486 credited from wallet17 (result503), and calculated486/credited0 at wallet cap1e12 (result stays1e12). Both showed a newly configured FreshLeaf4 campaign, no +80 fresh-save bonus and exactly-once receipt behavior.
- QA marked supplementary E02/E03/E05/E06/E08/E09 and focused E04/E07 PASS. The transfer-mode and migration-cap additions were verified with targeted authored-click/harness cases rather than rerunning the full suite.
- After Play, the isolated `M050 Editor Save Fixture` was reset through its Unity menu command. `SampleScene.unity` was saved in Edit Mode and reopened; latest active scene read `isDirty=false`, rootCount9; validation returned0 issues,0 missing scripts,0 broken prefabs. Build settings include only the enabled `Assets/Scenes/SampleScene.unity` scene.
- UI reviewer accepted representative Garden, Dryer, Packer, ready/missing/cancel, transfer-zero and migration states. Actual synchronous GameView/Overlay captures are **824×1465 px**. Current evidence includes `M050-Fresh-Garden-Final.png`; `M050-Dryer-Load-MAX-Mode-Actual-v2.png`; `M050-Dryer-Collect-Output-WIP-Actual.png`; `M050-Packer-FactoryTab-Actual.png`; `M050-Packer-Collect-Ready-Actual.png`; `M050-Transfer-Collect-Zero-Disabled-Actual.png`; `M050-PostCancel-Actual.png`; `M050-Migration-ThreeL3-Actual.png`; `M050-Migration-WalletCap-Actual.png`; `M050-Active-Ready-Actual.png`; `M050-Active-Missing-Actual-v2.png`; and `M050-Cancel-Confirmation-Actual-v2.png`. `M050-Dryer-Collect-Mode-Actual.png`, `M050-Packer-Collect-Mode-Actual.png`, `M050-Packer-Load-MAX-Mode-Actual.png` and `M050-Transfer-Load-MAX-Actual.png` are stale async captures of the initial Orders UI and must not be used as evidence.

## Android APK0.5.0

- Unity MCP build job `build-d9abc9d8a7` succeeded in88.16s with0 errors and1 warning. Artifact: `Builds/Android/TinyFactory-0.5.0.apk`.
- Verified manifest: application ID `com.tinyfactory.prototype` (same as0.4), versionName `0.5.0`, versionCode `11`, minSdk25, targetSdk36, native ABI only `arm64-v8a`; backend IL2CPP. Signature v2 verifies, certificate SHA-256 `7763eae5a11cfd879282e8c940eea3d9212cb72b8b46e13d200164d81994b63a` matches the0.4 Android Debug baseline. `zipalign -c -v 4` passes. APK SHA-256 `1c1df75aa26accbc71901379b930c3576b0a351ae33578afd63177a29c5fe2b2`; measured size40,491,069 bytes. Build service separately reports `total_size_mb=742.74`, which does not equal the independently measured APK file size.
- Unity Console’s post-build warning: `Pipeline: No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime). Pipeline will be disabled in Player builds.` This did not fail compilation/build. The editor also recorded MCP bridge port retry/fallback diagnostics.
- The APK was not installed on a physical device. Touch/pan, device safe areas, 48dp target sizes, and exact9:19.5/9:20 layout remain manual device checks.

## Engineering files

- `Assets/TinyFactory/MetaFactoryConfig.cs` and `.meta`
- `Assets/TinyFactory/MetaFactoryRuntime.cs` and `.meta`
- `Assets/TinyFactory/MetaFactoryHudView.cs` and `.meta`
- `Assets/TinyFactory/MetaFactoryWorldView.cs` and `.meta`
- `Assets/TinyFactory/MetaFactoryPlot.cs` and `.meta`
- `Assets/TinyFactory/MetaOrderRowView.cs` and `.meta`
- `Assets/TinyFactory/Resources/MetaFactoryConfig.asset` and `.meta`
- `Assets/TinyFactory/Prefabs/MetaOrderRow.prefab` and `.meta`
- `Assets/TinyFactory/Editor/MetaFactorySceneAuthoring.cs` and `.meta`
- `Assets/TinyFactory/Editor/MetaFactoryValidation.cs` and `.meta`
- `Assets/Scenes/SampleScene.unity`
- `Builds/Android/TinyFactory-0.5.0.apk`
