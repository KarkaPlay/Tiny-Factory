# M051 implementation — 0.5.1 / code 12

## Scope implemented

- `MetaFactoryRuntime.Load` accepts one input SKU even when that quantity does not complete the recipe. The existing Dryer recipe still reserves and processes exactly two FreshLeaf; a partial input stays in the station and in schema2 saves. `MaxLoad` reports the live capacity/stock limit. Collect still moves only the amount that fits and leaves the rest at the station.
- `MetaFactoryQuickActionView` adds building-local `+1` / `Забрать N` controls and inbound/output warehouse labels. The Garden exposes collect only. The lower transfer quantity picker is no longer required or bound; the bottom card continues to own building details/upgrades, and tabs retain warehouse/orders.
- Each action plate projects beside the building's renderer bounds with a 10dp gap and 10dp screen-edge margin, preferring the right and using the left when the right side does not fit. Plates are clamped to the play viewport; the compact 340×450 reference-unit panel stacks full-width action rows so the station silhouette stays visible. Panels follow visible buildings while the controller stays enabled for LateUpdate and action refresh.
- `MetaFactoryQuickActionButton` cancels a release after its pointer crossed the movement threshold. World drags ignore gestures beginning on UI, preview the nearest visible station with hysteresis, then snap and persist the existing numeric camera offset only after settle. Interrupting an active drag or snap on pause/focus loss clears pointer state and immediately settles selection.
- `MetaFactoryWorldMotionView` subscribes only to successful transfer commits and displays a resource-count token along the manual station↔shared-warehouse path. Work bob and output pulse are presentation only. All motion stops on interruption/disable; state and saves commit before the tween. No automatic transfers or save-schema change were added.
- Sidecar depot pulse, station pulse, early `Refresh` null-state guard, and token timing are visual/lifecycle behavior only. Final source `transferDuration` is 0.62 seconds; longer runtime-only Editor values were used solely to try to capture frames and were never authored to the scene.

## Relevant source and authored assets

- `Assets/TinyFactory/MetaFactoryRuntime.cs`
- `Assets/TinyFactory/MetaFactoryHudView.cs`
- `Assets/TinyFactory/MetaFactoryWorldView.cs`
- `Assets/TinyFactory/MetaFactoryPlot.cs`
- `Assets/TinyFactory/MetaFactoryQuickActionButton.cs` and matching `.meta`
- `Assets/TinyFactory/MetaFactoryQuickActionView.cs` and matching `.meta`
- `Assets/TinyFactory/MetaFactoryWorldMotionView.cs` and matching `.meta`
- `Assets/TinyFactory/Editor/MetaFactorySceneAuthoring.cs`
- `Assets/Scenes/SampleScene.unity`

No package was added. Existing DOTween and uGUI are reused. Runtime state remains in the existing schema2 save contract.

## Delivery and evidence

Android build `Builds/Android/TinyFactory-0.5.1.apk` succeeded via Unity MCP after focused QA handoff. Version is 0.5.1 / code 12, package `com.tinyfactory.prototype`, IL2CPP ARM64, Unity debug signing. Producer independently verified the 40,506,481-byte APK, manifest, signature, ABI and unchanged earlier APK hashes. Build/Console warning: no RuntimePipelineConfig asset is present, so that pipeline package reports it will be disabled in Player builds; no compile or build errors.

QA focused runtime/save/capacity, snap/clamp/one-settle and motion lifecycle cases passed as recorded in [M051_QA.md](M051_QA.md). Live authored pointer-handler preview exercised nearest selection and hysteresis in both drag directions. A saved −4.2 offset restored with Packer selected and camera snap visible; a rewrite of that startup offset after settle was not confirmed. The correct Packer final capture and temporal two-frame token movement were not obtained. The actual Dryer button transaction and one committed-token frame are recorded in [M051_ENGINEER_CHECKS.md](M051_ENGINEER_CHECKS.md); the following image was captured after Editor focus loss cleaned up the token, so it is not temporal-motion proof. Physical-device acceptance remains open.
