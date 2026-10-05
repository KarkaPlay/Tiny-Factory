# M051 Engineer checks — 0.5.1 / code 12

## Editor and authored scene

- Unity MCP connected to `Tiny Factory@4b2dafe5`, repository root verified, Unity `6000.3.9f1`. Final editor state: Edit Mode, not compiling, no external asset changes, `Assets/Scenes/SampleScene.unity` clean (`isDirty=false`, 10 roots). Scene validation returned 0 issues, 0 missing scripts, 0 broken prefabs. Scene was saved and reopened after the final sidecar margin change.
- Final source compile after changing screen-edge clamp margin from 6dp to 10dp completed. Unity Console has 0 errors. One project warning remains: `Pipeline: No RuntimePipelineConfig asset found ... Pipeline will be disabled in Player builds.` MCP reconnect/port-fallback messages are transport diagnostics.
- The isolated M050 Editor Save Fixture was reset after Play Mode. No production save was used or migrated.

## Focused live interaction evidence

- Actual `MetaFactoryWorldView` pointer handler path (synthetic coordinates through the authored Begin/Move/End handlers) previewed both scroll directions: down sequence selected `Dryer` at camera Y 2.0 and −1.0, then `Packer` at −3.9 and −5.0; reverse movement retained `Packer` at −3.9 then returned to `Dryer` at −1.0, 0.8 and 2.0. This exercises live nearest selection and its hysteresis during movement. It is an Editor handler smoke, not a physical touchscreen test.
- Loaded saved numeric offset `−4.2`, stopped and re-entered Play Mode. Startup selected `Packer` and moved the camera from the saved-offset origin to Y `0.69` to center the nearest plot. The value `−4.2` remained the loaded save value; automatic startup snap was observed, but a settled save rewrite from that startup snap was not confirmed.
- With the live EventSystem, an authored Garden button point produced raycast hits and `IsPointerOverUi=true`; starting the world pointer at that point left the world pointer idle. A live Dryer `+1` button EventSystem press/up/click decremented FreshLeaf `4→3`, left input `1/2`, and created one transfer token. A second authored click consumed another FreshLeaf and started the unchanged 2-unit Dryer recipe (`reservedInput=2`, `remainingSeconds=8`); final temporary stock was 2.
- The earlier button swipe smoke remains in the recorded implementation notes: down→move 40px→up on the actual authored button suppressed click and did not transfer; valid tap produced one transaction. QA focused runtime/core and lifecycle checks are recorded in `M051_QA.md`.

## Visual evidence and limits

- Dryer sidecar and action state: [M051-dryer-sidecar-final-1.png](../Assets/Screenshots/M051-dryer-sidecar-final-1.png). It shows the built Dryer, FreshLeaf/DryTea stock, `+1`, collection row and working presentation without covering the machine body.
- One actual committed-transfer image: [M051-transfer-temporal-a.png](../Assets/Screenshots/M051-transfer-temporal-a.png). A token is visible during the temporary 6-second Editor capture fixture while the source/serialized duration remains 0.62 seconds. [M051-transfer-temporal-b.png](../Assets/Screenshots/M051-transfer-temporal-b.png) is a later capture after the Editor lost focus and lifecycle cleanup removed the transient token. These two images do **not** prove motion over time; visual temporal motion remains unverified in this Editor capture environment. The verified tween lifecycle stops the token on focus loss, and QA verified committed state is not replayed.
- `M051-packer-sidecar-final.png` was mistakenly named as Packer but the screenshot heading and inbound/output labels identify the Dryer; it is excluded as Packer evidence. No correct final Packer screenshot was obtained. The Garden sidecar screenshot predates the final 10dp source margin: [M051-garden-sidecar-final.png](../Assets/Screenshots/M051-garden-sidecar-final.png).
- Physical-device touch, real background/resume and Android runtime checks remain unverified. Build success does not establish device readiness.

## Android build

- Unity MCP job `build-d5e36bb17d`: **Succeeded** for Android, output `Builds/Android/TinyFactory-0.5.1.apk`, build duration 79.0s, 0 build errors, 1 warning. Settings checked before build: version `0.5.1`, Android version code `12`, bundle ID `com.tinyfactory.prototype`, IL2CPP, ARM64, custom keystore disabled (Unity debug signing). Parent/Producer independently verified the APK: 40,506,481 bytes, SHA-256 `9b82885f8a9fb9b58715f4b7a0be61ff7f8329a514c7a7b7050665b96df02071`; manifest package/version/code/SDKs/ABI and signature verified, prior APK hashes unchanged.
- The one Console/build warning is the missing RuntimePipelineConfig note above. No project error appeared after the final compile/build. Do not treat this Editor/build record as physical-device acceptance.
