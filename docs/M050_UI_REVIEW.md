# M05 UI review

**Task / status:** M05 UI review · bounded visual review complete for the agreed representative states. Packer load-mode preview and constrained/zero load visuals were not captured; Engineer/QA separately reports authored Packer load/collect smoke and load-capacity logic PASS.

**Scope:** Current `MetaFactoryHudView`, `MetaFactoryWorldView`, and `MetaFactorySceneAuthoring` helper. Read-only; no Unity MCP calls or scene changes.

## Final targeted recheck

- Tutorial panel is now a child of the safe-area root at y=.415–.485, separate from the pinned lower Factory card and its Build Status region at y=.36–.47 relative to the Factory panel. The source-level overlap is resolved.
- The scene helper creates a screen-space overlay Canvas with a pinned header and lower card; the separate world camera remains orthographic and contains exactly three vertically ordered plots. Static hierarchy/source preserves the authored UI-over-3D composition.
- Producer accepted the gesture policy: movement past threshold claims the gesture and cancels tap; camera pan begins only for vertical-dominant movement. A horizontal gesture suppressing tap without moving the camera is therefore intentional. The dp conversion and threshold are present in source; device behavior is not verified.
- Previously rechecked source fixes remain present: tutorial progression follows offer acceptance/hand-in, Dryer build/load and output collection; migration notice spells out actual wallet/refund, configured FreshLeaf start, no fresh-save coin bonus and new campaign; station output has an independent `Готово к сбору` state; foreground-only disclosure is always assigned.

## Actual capture: fresh clean HUD

Reviewed `Assets/Screenshots/M050-Play-GameView-Fresh-CleanHud.png` at **562 × 1000 px** (approximately 9:16).

- Header hierarchy reads in order: wallet, active-order prompt, foreground-only disclosure. In this capture those lines fit without overlap and remain legible.
- Bottom navigation, selected-station card, upgrade action, collect preview and controls are visible in one frame. The tutorial strip sits directly above the tabs and does not collide with the lower card in this capture. The transfer row is close to the lower screen edge; its real safe-area clearance and dp size remain unverified.
- These findings are superseded by the later state captures below: the tutorial offer is labeled and the Orders tab has an active visual state.
- The image shows a blank world slab; this is the already-known world/camera blocker and is excluded from this UI review. This capture does not show the production visuals needed to judge whether the tutorial overlay obscures a plot.

## Actual capture: onboarding and orders

Reviewed `Assets/Screenshots/M050-Play-GameView-Onboarding-Orders-Reviewed.png` at **824 × 1465 px** (approximately 9:16).

- Header and Russian labels read clearly; `Заказы` has a visibly selected gold state, the prompt matches the open panel, and the first offer is labeled `Учебный заказ`. Empty active-order actions are hidden, and both offers show their stock, shortage and reward. The earlier tutorial-offer and empty-action findings are resolved in this capture.
- In this older reviewed image the Garden trunk reaches the tutorial strip. The latest `M050-Fresh-Garden-Final.png` has the trunk fully visible above that strip, so the earlier overlap concern is resolved for the fresh Garden view.

## Earlier state-capture set (superseded)

All five files below are **824 × 1465 px** (approximately 9:16).

- `M050-Fresh-Garden-Final.png`: Garden model is fully visible above the tutorial strip. Orders remains selected, so this does not show the Garden station card/status.
- `M050-Dryer-WIP-Final.png`: shows a large Dryer model cropped at the pinned header; the Orders tab is still selected and no Dryer WIP/timer/output status appears. This is not evidence for the WIP UI state or full-chain framing.
- `M050-Active-Missing-2026-10-05.png` and `M050-Cancel-Confirmation-2026-10-05.png` have identical PNG hashes. Both show the initial empty active-order state and offer board, with no active order or cancel confirmation.
- `M050-Active-Ready-2026-10-05.png` has the same PNG hash as `M050-Fresh-Garden-Final.png`. It also shows the initial empty active-order state, not a ready active order.
- The requested transfer-preview and migration-state captures are not present in this set.

These earlier files confirm fresh Garden visibility and offer labeling, but the named active/ready/cancel images were duplicate initial-state renders and do not count as state evidence. They are superseded below.

## Updated Dryer capture

Reviewed the newer `Assets/Screenshots/M050-Dryer-WIP-Final-1.png` at **824 × 1465 px**. It supersedes the earlier `M050-Dryer-WIP-Final.png` / `Final2.png` images for the Dryer view.

- Factory tab and pinned card are now shown. Dryer name, input `2/8`, work timer `7 s`, output `0/8`, cycle `8 s`, unaffordable upgrade reason, empty-warehouse reason, and transfer control row are visible. This confirms one WIP/zero-output frame and the no-input reason.
- The camera shows one complete Dryer model, but this capture does not show the Garden and Packer together or a scroll transition, so it does not confirm the full three-plot chain.
- The old `Final-1` frame has a bilingual duplicate SKU (`Свежий лист ×4 · FreshLeaf ×4`); this is superseded by `M050-Dryer-WIP-Localized-Actual.png`, whose current Dryer labels are localized. No localization blocker remains in the fresh frame.
- The transfer buttons sit close to the lower edge of this GameView. Physical safe-area clearance and target size in dp still need device verification.

## Actual Garden and collect-preview captures

Reviewed `M050-Garden-Factory-Actual.png` and `M050-Transfer-Collect-MAX-Actual.png`, `M050-Transfer-Collect-Constrained-Actual.png`, and `M050-Transfer-Collect-Zero-Actual.png`. Each is **824 × 1465 px** (approximately 9:16).

- **Garden station — PASS for pinned layout/status:** the selected `Завод` tab, `Чайный сад` station card, level/output/timer, upgrade action, no-ready-output notice, and transfer row are visible together. The offer/tutorial strip stays above the pinned station card. The underlying world slab is still blank; that known world-render blocker is outside this UI review.
- **Collect MAX — PASS for preview clarity:** preview states `Забрать 8`, remaining output 0, product warehouse 12/40 and total 12/40; output and resulting quantities are visible above the controls.
- **Constrained collect — PASS for preview clarity:** preview caps collection at 1, leaves output 7, and shows warehouse 20/40. The one-unit limit is explicit in the sentence.
- **Full warehouse / zero collect — pre-fix partial:** `Склад заполнен · освободите место для Свежий лист` is visible while output remains 7. In this capture the `Забрать` control looks available despite zero transfer quantity; Engineer is changing disabled styling and will provide a post-fix frame. Do not count the current image as final disabled-state evidence.
- These are Garden/collect states only. They do not establish Dryer or Packer load-preview behavior.

## Actual Dryer and load captures

Reviewed `M050-Transfer-Load-MAX-Actual.png`, `M050-Dryer-WIP-Localized-Actual.png`, and `M050-Dryer-OutputReady-WIP-Actual.png`; each is **824 × 1465 px** (approximately 9:16).

- **Dryer WIP localization — PASS:** `Сушильня`, input/output counts, cycle, wait-for-input status, upgrade cost and load preview are readable; the former `FreshLeaf` duplicate is gone. The station model fits between the pinned header and tutorial strip.
- **Dryer load MAX — PASS for preview clarity:** preview says load 8 FreshLeaf, resulting input 8/8, warehouse remaining 12. Buttons and result stay in frame.
- **Previous ready-output action blocker:** pre-fix Dryer/Packer frames exposed a ready output with load-only transfer context. The latest Dryer and Packer collect-mode captures below resolve this visibly for those states. Packer load-mode preview is not captured, while its authored button smoke is reported PASS by Producer/Engineer.
- These captures show one Dryer model, not all three plots or a scroll transition.

`M050-Packer-WIP-Actual.png` and `M050-Packer-OutputReady-Actual.png` also show the Packer WIP and ready-output card at **824 × 1465 px**. These are pre-transfer-mode-refactor captures: the ready state says `Готово к сбору · Пакетики ×1`, but the transfer row remains in load context with no DryTea available. They reproduce the same P1 actionability issue and are not evidence for the pending collect/load-mode fix or final control-row readability.

The later files named `M050-Dryer-Collect-Mode-Actual.png`, `M050-Packer-Collect-Mode-Actual.png`, and `M050-Packer-Load-MAX-Mode-Actual.png` are **not counted as mode evidence**: the Dryer file visibly shows the Garden/Orders initial view, and the two Packer files have identical PNG hashes and show that same stale Orders view. These files are superseded by the validated synchronous Dryer captures below.

Latest valid Dryer mode frames: `M050-Dryer-Load-MAX-Mode-Actual.png` and `M050-Dryer-Collect-Output-WIP-Actual.png`, both **824 × 1465 px**.

- **Dryer load mode — PASS for visible context:** `Загрузка` is selected in gold, preview gives the load quantity/result and remaining warehouse, and CTA reads `Загрузить 4`.
- **Dryer collect while next WIP runs — PASS for visible context:** `Сбор` is selected in gold, the independent card status shows ready DryTea alongside active WIP, preview reads `Забрать 4`, and CTA reads `Забрать 4`. The alternate `Загрузка` mode remains visible.
- **Control row — PASS at this GameView size:** all six transfer controls fit on one row and labels are readable in the capture. This does not verify physical touch-target size or device safe area. Grey disabled `+`/upgrade controls are visually distinct from available actions.
- Engineer reports authored Dryer collect smoke moved DryTea into the warehouse while the next WIP timer remained active; this reviewer did not operate the button.

## Actual Packer collect-ready capture

Reviewed `M050-Packer-Collect-Ready-Actual.png` and the clearer `M050-Packer-FactoryTab-Actual.png`, each **824 × 1465 px**. The latter confirms bright-gold selected `Завод` with dark text; the earlier muted olive was a capture tint, not a UI defect. The Packer card shows `Готово к сбору · Пакетики ×1`; `Сбор` is selected in gold; preview says collect 1, output becomes 0, product warehouse 1/20 and total 5/40; CTA says `Забрать 1`. Quantity, action and selected mode agree, resolving the earlier load-only context in this captured ready state. Disabled `−`/`+` and upgrade controls are visibly grey. The six-button transfer row remains readable within the GameView width, without confirming device dp/touch size.
- Packer load-mode/MAX preview is not evidenced by a valid screenshot. Producer/Engineer reports authored Packer collect and `Загрузка`+MAX smoke passed: collecting one packet, then loading one DryTea and starting a 6 s WIP. This reviewer did not operate the controls.

## Representative station sequence and post-cancel state

- **Station sequence — PASS by the agreed series criterion:** `M050-Garden-Factory-Actual.png`, `M050-Dryer-Load-MAX-Mode-Actual-v2.png`, and `M050-Packer-FactoryTab-Actual.png` show Garden, Dryer and Packer respectively with their matching pinned station cards at **824 × 1465 px**. The selected Factory tab and lower card remain visible across the frames. This confirms each station view as a sequence, not a single frame containing the whole field or a hardware-tested pan gesture. The known Garden blank-slab render issue remains outside the pinned-UI review.
- **Post-cancel — PASS for visible state:** `M050-PostCancel-Actual.png` at **824 × 1465 px** shows the confirmation modal closed, no active order, and both replacement offers. Stock remains available and is reflected in the offer deficit. Engineer reports the authored cancel+confirm path passed; this reviewer did not operate the controls.

## Actual migration notices

Reviewed `M050-Migration-ThreeL3-Actual.png` and `M050-Migration-WalletCap-Actual.png`, each **824 × 1465 px**.

- **Migration copy — PASS:** the modal shows the actual credited refund and resulting wallet, the fresh start of 4 FreshLeaf, the omitted fresh-save bonus of 80 coins, and that old-version sales do not count as new orders. The acceptance CTA reads `Принять и начать кампанию` and is fully visible.
- **Wallet cap case — PASS for disclosed result:** body states calculated refund 486, actual credit 0 at the 1,000,000,000,000 cap, unchanged final wallet, and the same 4 FreshLeaf / no-80 new campaign terms.
- **P2 polish:** while the modal is open, the persistent migration notice remains visible behind it and repeats the same figures; in the cap screenshot that top notice renders the large cap as ungrouped digits while the modal uses grouped digits. The central modal itself remains readable and has the correctly formatted amount. Consider suppressing the duplicate background notice during the modal.

## Actual order-state captures

Latest validated files `M050-Active-Missing-Actual-v2.png` and `M050-Cancel-Confirmation-Actual-v2.png`, plus `M050-Active-Ready-Actual.png`, are each **824 × 1465 px** (approximately 9:16). Earlier non-v2 missing/cancel captures had ambiguous shortfall copy and an off-screen dialog; they are superseded.

- **Active missing — PASS:** the header explicitly labels `Не хватает: Свежий лист ×4`; the order cannot be handed in, and the cancellation action is available. The offer cards show stock and deficit.
- **Active ready — PASS:** header says `готов`, all four items are visible in stock, and `Сдать заказ` is enabled. This remains visually distinct from missing stock.
- **Cancel confirmation — PASS for visibility/layout:** the warning and both `Подтвердить` / `Вернуться` actions are fully visible over the offer list in v2. This does not verify the result after confirming cancellation.

## Evidence and limits

- Static inspection plus actual GameView captures at 562 × 1000 and 824 × 1465, including the newer Dryer WIP and valid active missing/ready captures. Earlier duplicate state renders are marked superseded; conclusions rely only on visible pixels.
- **Visual reviewer did not independently run:** Unity import/compile/scene validation, Play Mode, Console or device checks. Producer reports core Editor suites and transfer load-capacity logic PASS. **Not visually captured:** Packer load-mode preview and constrained/zero load states. **Device checks not run:** 9:19.5/9:20 aspect captures, touch gesture checks, hardware safe-area validation, or measured 48 dp targets.

## Bounded verdict

No remaining visual blocker was found in the reviewed representative 824 × 1465 GameView states. The screenshots cover pinned HUD and tab hierarchy, Garden/Dryer/Packer cards in the agreed station sequence, collect/load mode and CTA/preview agreement on Dryer, Packer ready-output collection, full/zero collect disabled treatment, order states and cancel confirmation/result, plus both migration outcomes. Producer accepted the station sequence across screenshots rather than requiring all plots simultaneously.

The duplicate migration notice behind its modal, including ungrouped cap digits in that top line, remains P2 polish and Producer accepted it as non-blocking. The Garden blank-slab render issue is tracked outside this UI review. Packer load preview and constrained/zero load states were not visually reviewed; Engineer/QA reports its interaction and capacity checks pass. Physical device touch, safe area, 48 dp targets and 9:19.5/9:20 layouts remain unverified.
