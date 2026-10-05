# M04–M05 implementation handoff

Status: implementation and Editor validation pass; Android APK build succeeded through Unity MCP. Physical-device D12 remains a manual follow-up.

## Implemented

- `FactoryBalanceConfig` owns M04 upgrade costs and curves, roller/sealer unlock thresholds, sale tiers, queue capacity, and value caps; runtime does not carry parallel balance constants.
- `FactoryRuntime` implements three finite L0–L3 upgrades, roller routing and sealer unlock/price progression, transaction order, bounded queues and WIP, saturation, and foreground-only simulation. Normal backgrounding preserves in-memory items and timers without catch-up; cold load drops WIP and timer phase and grants no offline earnings.
- Versioned local JSON v1 stores permanent coins, levels, lifetime sold (unlock state is derived), onboarding, and mute. Writes validate the temp file before rotating primary/backup. Missing/corrupt required root fields recover from verified backup or start fresh with a visible notice; newer schemas are archived and preserved while writes redirect to a fresh slot. Archive names include unique suffixes.
- `FactoryHudView` uses authored Inspector references and persistent UnityEvents. Three uGUI upgrade cards expose locked/insufficient, affordable, and MAX states. World station labels and the roller progress label report unlock/processing state; the existing final station changes to the distinct sealer presentation. Save notices stay visible for at least five seconds and cannot be replaced by short gameplay toasts.
- Editor authoring tools create the card layout, persistent events, roller/sealer view, line anchors/progress display, and preview states. Preview states disable the runtime save path and are memory-only.
- `Editor/M040Validation.cs` covers balance, queue/batch rejection, lifecycle, active WIP timing, upgrades, bounded 300-second traces, backpressure, saturation, and save recovery fixtures.

## Validation evidence

- Unity MCP connected to the single `Tiny Factory@4b2dafe5` instance at the repository project root, Unity `6000.3.9f1`.
- Current authored scripts imported; Unity compilation completed with `is_compiling=false` and no C# diagnostics. Scene authoring menus ran, `SampleScene` was saved and reopened through MCP, and scene validation returned 0 issues, 0 missing scripts, and 0 broken prefabs.
- The final `Tiny Factory/Run M04 M05 Validation` run logged `M040 VALIDATION PASS`. The active 300-second trace matched the accepted checkpoints: t6 sold1/coins4; t42 Speed L1, sold9/coins0; t65 sold15/coins24 and roller unlocked; t127 sold40/coins23 and sealer unlocked; t207 all three branches L3/coins5; t300 sold184/coins563. Passive 300 seconds ended at sold24/coins105.
- The harness also passed all nine purchases/MAX, sale ordinals, intervals/batches/costs, manual/auto overflow, same-tick sale income and purchase ordering, no active-WIP retiming, roller route, 27-item upper bound, background pause/resume, cold-load WIP discard, save field/range validation, primary/backup recovery, future-schema preservation/fresh-slot fallback, and temp-write failure preserving existing primary. The temp-write failure was injected before writing temp contents; process-crash behavior at replacement and Android filesystem atomicity remain unverified.
- UI review passed the locked/startup, affordable, MAX/unlocked, and recovery-notice captures listed in `M040_ENGINEER_CHECKS.md`. A fresh decorated MAX capture after duplicate cleanup is `Assets/Screenshots/M040-MAX-Unlocked-9x16-Decorated.png` (792×1584, 1:2), so its filename does not establish a 9:16 ratio. Two Unity MCP attempts to capture 405×900 still yielded 675×1200 with a 9:16 camera output; 20:9 is unverified, as is 19.5:9. Do not claim narrow-layout runtime/device behavior.
- Unity MCP job `build-5c45e8f29d` succeeded in 56.819734 s with 0 errors / 1 warning. Final artifact: `Builds/TinyFactory-0.4.0.apk`, **40,430,233 bytes**, SHA-256 `b9973088128f46578ff387f3b390c7d6e4d965842961508b7ca46bfe5c3f360d`. APK manifest inspection confirms version `0.4.0`, code `10`, package `com.tinyfactory.prototype`, minSDK 25 / targetSDK 36, and only `arm64-v8a` native libraries. Unity settings confirm IL2CPP. Reported build size 741.13 MB is not the APK file size.
- Post-build MCP: Play off, compiling/updating false, Console errors 0; SampleScene validation 0 issues / 0 missing scripts / 0 broken prefabs. Pre-build saved scene was clean, with coins/sold/levels/WIP zero, no validation hosts, and exactly one roller and one sealer root.
- The sole warning is `Pipeline: No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime). Pipeline will be disabled in Player builds.` It is the existing optional Pipeline configuration warning; no URP failure is inferred from it.
- Final focused HUD regression passed: Productivity L3 authored click queues 4, CTA/feedback +4, one listener; route hints at sold0/15/40 show +4/+5/+6. Final decorated capture `Assets/Screenshots/M040-MAX-L3-Harvest-Batch4-RoutePrice6.png` is 824×1465 (approximately 9:16).
- Candidate `build-796ade3857` preceded these HUD fixes and was superseded. Producer completed and inspected the final build after the specialist usage limit, with user approval.

## Limits

No M06 tutorial/audio, M07–M09 ad/analytics integrations, new packages, SDKs, or other scope were added. Device D12 is pending manual testing; no device-ready claim is made. 19.5:9/20:9 aspect captures, crash-interruption semantics, Android filesystem atomicity, and on-device validation remain unverified.
