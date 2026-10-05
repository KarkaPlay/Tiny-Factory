#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TinyFactory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory.Editor
{
    internal static class M040Validation
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly System.Collections.Generic.List<GameObject> Hosts = new();

        [MenuItem("Tiny Factory/Run M04 M05 Validation")]
        private static void Run()
        {
            Cleanup();
            string directory = Path.Combine(Path.GetTempPath(), "tinyfactory-m040-validation");
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
                Directory.CreateDirectory(directory);
                FactoryBalanceConfig config = Resources.Load<FactoryBalanceConfig>("FactoryBalanceConfig");
                Require(config != null && config.IsValid(), "authored balance config is valid");
                ValidateBalanceAndSimulation(config);
                ValidateTraceAndLifecycle(config);
                ValidateSaves(config, directory);
                ValidateSceneAuthoring();
                Debug.Log("M040 VALIDATION PASS: " + directory + "\nExact branch balance, price tiers/unlocks, third-stage routing, 27 cap, persistent UI listeners/references and save recovery cases passed.");
            }
            catch (Exception exception) { Debug.LogError("M040 VALIDATION FAIL: " + exception); }
            finally
            {
                Cleanup();
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void ValidateBalanceAndSimulation(FactoryBalanceConfig config)
        {
            Require(config.UpgradeCost(0) == 36 && config.UpgradeCost(1) == 54 && config.UpgradeCost(2) == 72, "all three upgrade costs are 36/54/72");
            Require(config.DryerDuration(0) == 3 && config.DryerDuration(1) == 2 && config.DryerDuration(2) == 2 && config.DryerDuration(3) == 1, "dryer timing L0-L3");
            Require(config.RollerDuration(0) == 2 && config.RollerDuration(1) == 1 && config.RollerDuration(2) == 1 && config.RollerDuration(3) == 1, "roller timing L0-L3");
            Require(config.FinalDuration(0) == 2 && config.FinalDuration(1) == 2 && config.FinalDuration(2) == 1 && config.FinalDuration(3) == 1, "final timing L0-L3");
            Require(config.ManualBatch(0) == 1 && config.ManualBatch(1) == 2 && config.ManualBatch(2) == 3 && config.ManualBatch(3) == 4, "productivity batch L0-L3");
            Require(config.AutomationInterval(0) == 12 && config.AutomationInterval(1) == 10 && config.AutomationInterval(2) == 8 && config.AutomationInterval(3) == 6, "automation interval L0-L3");

            FactoryRuntime runtime = NewRuntime(config, null);
            Set(runtime, "sold", 14L);
            Set(runtime, "packagerHasWip", true);
            Set(runtime, "packagerRemaining", 1);
            runtime.Tick();
            Require(runtime.Sold == 15 && runtime.Coins == 4 && runtime.RollerUnlocked, "sale #15 pays 4 then unlocks roller");
            Set(runtime, "packagerHasWip", true);
            Set(runtime, "packagerRemaining", 1);
            runtime.Tick();
            Require(runtime.Sold == 16 && runtime.Coins == 9, "sale #16 pays 5");
            Set(runtime, "sold", 39L);
            Set(runtime, "coins", 0L);
            Set(runtime, "packagerHasWip", true);
            Set(runtime, "packagerRemaining", 1);
            runtime.Tick();
            Require(runtime.Sold == 40 && runtime.Coins == 5 && runtime.SealerUnlocked, "sale #40 pays 5 then unlocks sealer");
            Set(runtime, "packagerHasWip", true);
            Set(runtime, "packagerRemaining", 1);
            runtime.Tick();
            Require(runtime.Sold == 41 && runtime.Coins == 11, "sale #41 pays 6");

            runtime = NewRuntime(config, null);
            Set(runtime, "coins", 36L);
            Require(runtime.RequestUpgrade(UpgradeBranch.Speed), "Speed purchase queues once");
            Require(!runtime.RequestUpgrade(UpgradeBranch.Speed), "double tap cannot queue another purchase");
            runtime.Tick();
            Require(runtime.SpeedLevel == 1 && runtime.Coins == 0, "Speed L1 commits exact funds");
            runtime = NewRuntime(config, null);
            Set(runtime, "coins", 36L);
            Require(runtime.RequestUpgrade(UpgradeBranch.Productivity), "Productivity L1 purchase queues");
            runtime.Tick();
            Require(runtime.ProductivityLevel == 1 && runtime.RequestHarvest() && runtime.SourceBuffer == 2, "Productivity L1 changes manual batch to two");
            Set(runtime, "sourceBuffer", 7);
            Require(!runtime.RequestHarvest() && runtime.SourceBuffer == 7 && runtime.RejectedHarvests == 1,
                "manual harvest rejects an incomplete batch at a full buffer");
            runtime = NewRuntime(config, null);
            Set(runtime, "coins", 36L);
            Set(runtime, "secondsUntilAuto", 1);
            Require(runtime.RequestUpgrade(UpgradeBranch.Automation), "Automation L1 purchase queues");
            runtime.Tick();
            Require(runtime.AutomationLevel == 1 && runtime.SecondsUntilAuto == 10, "Automation purchase resets full first interval");
            Set(runtime, "sourceBuffer", 7);
            Set(runtime, "productivityLevel", 1);
            Set(runtime, "dryerHasWip", true); Set(runtime, "dryerRemaining", 2);
            Set(runtime, "secondsUntilAuto", 1);
            runtime.Tick();
            Require(runtime.SourceBuffer == 7 && runtime.RejectedHarvests == 1,
                "automatic harvest drops an incomplete batch instead of overflowing the buffer");
            runtime = NewRuntime(config, null);
            Set(runtime, "coins", 32L); Set(runtime, "packagerHasWip", true); Set(runtime, "packagerRemaining", 1);
            Require(runtime.RequestUpgrade(UpgradeBranch.Speed), "purchase can queue with below-cost wallet");
            runtime.Tick();
            Require(runtime.SpeedLevel == 1 && runtime.Coins == 0,
                "sale income is committed before the single queued purchase rechecks funds");
            runtime = NewRuntime(config, null);
            Set(runtime, "coins", 36L); Set(runtime, "dryerHasWip", true); Set(runtime, "dryerRemaining", 2);
            Require(runtime.RequestUpgrade(UpgradeBranch.Speed), "Speed purchase queues while dryer WIP is active");
            runtime.Tick();
            Require(runtime.SpeedLevel == 1 && runtime.DryerHasWip && runtime.DryerRemaining == 1,
                "purchase does not retime already active WIP");
            runtime = NewRuntime(config, null);
            Set(runtime, "sold", 15L);
            Set(runtime, "dryerHasWip", true);
            Set(runtime, "dryerRemaining", 1);
            runtime.Tick();
            Require(!runtime.DryerHasWip && runtime.RollerHasWip && runtime.RollerRemaining == 2, "new dryer output routes into roller WIP at L0");
            for (int i = 0; i < 26; i++) Set(runtime, "sourceBuffer", 8);
            Set(runtime, "rollerInput", 8);
            Set(runtime, "packagerInput", 8);
            Set(runtime, "dryerHasWip", true);
            Set(runtime, "rollerHasWip", true);
            Set(runtime, "packagerHasWip", true);
            Require(runtime.TotalInFlight == 27, "three 8-item buffers and three WIP slots total 27");
            runtime = NewRuntime(config, null);
            Set(runtime, "sold", 999999999999L); Set(runtime, "coins", 999999999999L);
            Set(runtime, "packagerHasWip", true); Set(runtime, "packagerRemaining", 1);
            runtime.Tick();
            Require(runtime.Sold == 1000000000000L && runtime.Coins == 1000000000000L,
                "wallet and lifetime sales saturate at 1e12");
            Debug.Log("M040 SIMULATION CHECKS: costs/timing/batches, exact sale ordinals, overflow handling, single-tick purchase/income order, no active-WIP retime, branch effect and automation reset, roller route, 27-item bound, and 1e12 caps passed.");
        }

        private static void ValidateTraceAndLifecycle(FactoryBalanceConfig config)
        {
            for (int branch = 0; branch < 3; branch++)
            {
                FactoryRuntime upgrades = NewRuntime(config, null);
                Set(upgrades, "coins", 1000L);
                UpgradeBranch selected = (UpgradeBranch)branch;
                for (int level = 0; level < 3; level++)
                {
                    Require(upgrades.RequestUpgrade(selected), selected + " L" + (level + 1) + " can be queued");
                    upgrades.Tick();
                    Require(upgrades.Level(selected) == level + 1, selected + " reaches L" + (level + 1));
                }
                Require(!upgrades.CanAffordNext(selected) && !upgrades.RequestUpgrade(selected), selected + " L3 displays/behaves as MAX");
            }

            FactoryRuntime trace = NewRuntime(config, null);
            for (int second = 1; second <= 300; second++)
            {
                trace.Tick();
                Require(trace.Coins >= 0 && trace.Sold >= 0 && trace.TotalInFlight >= 0 && trace.TotalInFlight <= 27,
                    "300-second trace preserves nonnegative wallet and 27-item line bound at second " + second);
                Require(trace.SourceBuffer <= 8 && trace.RollerInput <= 8 && trace.PackagerInput <= 8,
                    "three station buffers remain within capacity at second " + second);
            }
            long accepted = 25 - trace.RejectedHarvests;
            Require(trace.Sold + trace.TotalInFlight == accepted,
                "300-second trace accounts for every accepted auto item exactly once");
            long expectedCoins = Math.Min(trace.Sold, 15) * 4 + Math.Min(Math.Max(trace.Sold - 15, 0), 25) * 5 + Math.Max(trace.Sold - 40, 0) * 6;
            Require(trace.Coins == expectedCoins, "300-second trace wallet matches exact lifetime sale price tiers");
            Require(trace.Sold == 24 && trace.Coins == 105, "passive 300-second baseline checkpoint is sold 24 / coins 105");
            Debug.Log("M040 PASSIVE 300S TRACE PASS: sold=" + trace.Sold + ", coins=" + trace.Coins + ", rejectedAuto=" + trace.RejectedHarvests +
                ", inFlight=" + trace.TotalInFlight + ", buffers=" + trace.SourceBuffer + "/" + trace.RollerInput + "/" + trace.PackagerInput +
                ", no loss/duplication and bounds held each tick.");

            ValidateActiveGoldenTrace(config);

            FactoryRuntime lifecycle = NewRuntime(config, null);
            Require(lifecycle.RequestHarvest(), "lifecycle fixture accepts one manual item");
            lifecycle.Tick();
            int beforePauseTicks = lifecycle.Ticks;
            int beforePauseItems = lifecycle.TotalInFlight;
            int beforePauseDryer = lifecycle.DryerRemaining;
            int beforePauseAuto = lifecycle.SecondsUntilAuto;
            lifecycle.SetForeground(false);
            for (int i = 0; i < 120; i++) lifecycle.Tick();
            Require(lifecycle.Ticks == beforePauseTicks && lifecycle.TotalInFlight == beforePauseItems &&
                lifecycle.DryerRemaining == beforePauseDryer && lifecycle.SecondsUntilAuto == beforePauseAuto,
                "background ticks preserve live WIP and do not award offline progression");
            lifecycle.SetForeground(true);
            lifecycle.Tick();
            Require(lifecycle.Ticks == beforePauseTicks + 1, "foreground resume continues ordinary simulation ticks");

            FactoryRuntime blocked = NewRuntime(config, null);
            Set(blocked, "sold", 15L); Set(blocked, "secondsUntilAuto", 6);
            Set(blocked, "sourceBuffer", 8); Set(blocked, "rollerInput", 8); Set(blocked, "packagerInput", 8);
            Set(blocked, "dryerHasWip", true); Set(blocked, "dryerRemaining", 1);
            Set(blocked, "rollerHasWip", true); Set(blocked, "rollerRemaining", 1);
            Set(blocked, "packagerHasWip", true); Set(blocked, "packagerRemaining", 1);
            blocked.Tick();
            Require(blocked.TotalInFlight == 26 && blocked.RollerHasWip && blocked.DryerHasWip &&
                blocked.RollerInput == 8 && blocked.PackagerInput == 7,
                "downstream-first backpressure holds completed WIP when the next input is full without item loss");
            blocked.Tick();
            Require(blocked.RollerHasWip && blocked.RollerRemaining == 2 && blocked.RollerInput == 7 &&
                blocked.PackagerInput == 8 && blocked.TotalInFlight == 26,
                "completed roller WIP advances once when downstream space opens and the next queued item starts");
        }

        private static void ValidateActiveGoldenTrace(FactoryBalanceConfig config)
        {
            FactoryRuntime active = NewRuntime(config, null);
            UpgradeBranch[] cycle = { UpgradeBranch.Speed, UpgradeBranch.Productivity, UpgradeBranch.Automation };
            int nextBranch = 0;
            Require(active.RequestHarvest(), "active golden trace tap at t0 accepted");
            for (int second = 1; second <= 300; second++)
            {
                int queuedBranch = -1;
                for (int attempt = 0; attempt < cycle.Length; attempt++)
                {
                    int candidate = (nextBranch + attempt) % cycle.Length;
                    if (active.Level(cycle[candidate]) >= 3) continue;
                    queuedBranch = candidate;
                    break;
                }
                int queuedLevel = queuedBranch >= 0 ? active.Level(cycle[queuedBranch]) : 3;
                if (queuedBranch >= 0) active.RequestUpgrade(cycle[queuedBranch]);
                active.Tick(second % 6 == 0);
                if (queuedBranch >= 0 && active.Level(cycle[queuedBranch]) > queuedLevel)
                    nextBranch = (queuedBranch + 1) % cycle.Length;
                Require(active.Coins >= 0 && active.TotalInFlight <= 27, "active 300-second trace holds wallet/queue invariant at second " + second);
                if (second == 6)
                {
                    Require(active.Sold == 1 && active.Coins == 4, "active golden checkpoint t6: first sale pays 4");
                    Debug.Log("M040 ACTIVE TRACE t6: sold=" + active.Sold + ", coins=" + active.Coins + ", levels=" + active.SpeedLevel + "/" + active.ProductivityLevel + "/" + active.AutomationLevel);
                }
                if (second == 42)
                {
                    Require(active.SpeedLevel == 1 && active.Sold == 9 && active.Coins == 0, "active golden checkpoint t42: Speed L1");
                    Debug.Log("M040 ACTIVE TRACE t42: sold=" + active.Sold + ", coins=" + active.Coins + ", levels=" + active.SpeedLevel + "/" + active.ProductivityLevel + "/" + active.AutomationLevel);
                }
                if (second == 65)
                {
                    Require(active.RollerUnlocked && active.Sold == 15 && active.Coins == 24, "active golden checkpoint t65: roller unlock and sale tier");
                    Debug.Log("M040 ACTIVE TRACE t65: sold=" + active.Sold + ", coins=" + active.Coins + ", roller=" + active.RollerUnlocked);
                }
                if (second == 127)
                {
                    Require(active.SealerUnlocked && active.Sold == 40 && active.Coins == 23, "active golden checkpoint t127: sealer unlock and sale tier");
                    Debug.Log("M040 ACTIVE TRACE t127: sold=" + active.Sold + ", coins=" + active.Coins + ", sealer=" + active.SealerUnlocked);
                }
                if (second == 207)
                {
                    Require(active.SpeedLevel == 3 && active.ProductivityLevel == 3 && active.AutomationLevel == 3 && active.Coins == 5,
                        "active golden checkpoint t207: all branches at L3");
                    Debug.Log("M040 ACTIVE TRACE t207: levels=" + active.SpeedLevel + "/" + active.ProductivityLevel + "/" + active.AutomationLevel + ", coins=" + active.Coins);
                }
            }
            Require(active.Sold == 184 && active.Coins == 563 && active.SpeedLevel == 3 &&
                active.ProductivityLevel == 3 && active.AutomationLevel == 3,
                "active 300-second golden checkpoint: sold 184, coins 563, all three branches at L3");
            Debug.Log("M040 ACTIVE GOLDEN TRACE PASS: t300 sold=" + active.Sold + ", coins=" + active.Coins + ", levels=" +
                active.SpeedLevel + "/" + active.ProductivityLevel + "/" + active.AutomationLevel + ", rejectedManual=" + active.RejectedHarvests +
                ", inFlight=" + active.TotalInFlight + ".");
        }

        private static void ValidateSaves(FactoryBalanceConfig config, string directory)
        {
            string path = Path.Combine(directory, "save.json");
            FactoryRuntime source = NewRuntime(config, path);
            Set(source, "coins", 91L); Set(source, "sold", 14L);
            Set(source, "speedLevel", 2); Set(source, "productivityLevel", 1); Set(source, "automationLevel", 3);
            Set(source, "harvestOnboardingComplete", true); Set(source, "firstSaleOnboardingComplete", true);
            Set(source, "firstPurchaseOnboardingComplete", true); Set(source, "muted", true);
            Set(source, "sourceBuffer", 3); Set(source, "dryerHasWip", true); Set(source, "dryerRemaining", 1);
            Call(source, "SaveProgress");
            Set(source, "coins", 103L);
            Call(source, "SaveProgress");
            FactoryRuntime roundTrip = NewRuntime(config, path); Call(roundTrip, "LoadProgress");
            Require(roundTrip.Coins == 103 && roundTrip.Sold == 14 && roundTrip.SpeedLevel == 2 &&
                roundTrip.ProductivityLevel == 1 && roundTrip.AutomationLevel == 3 && roundTrip.Muted &&
                roundTrip.HarvestOnboardingComplete && roundTrip.FirstSaleOnboardingComplete && roundTrip.FirstPurchaseOnboardingComplete,
                "round-trip restores all permanent progress/settings/onboarding");
            Require(roundTrip.TotalInFlight == 0 && !roundTrip.DryerHasWip, "cold load discards WIP and buffers");

            string preInterrupted = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp");
            Set(source, "coins", 777L); Call(source, "SaveProgress");
            Require(File.ReadAllText(path) == preInterrupted, "failed temporary write preserves existing primary save");
            Directory.Delete(path + ".tmp", true);

            File.Delete(path);
            FactoryRuntime missingPrimary = NewRuntime(config, path); Call(missingPrimary, "LoadProgress");
            Require(missingPrimary.Coins == 91 && missingPrimary.Sold == 14, "missing primary restores valid backup");

            File.WriteAllText(path, "{\"schemaVersion\":1,\"coins\":0}");
            FactoryRuntime partialPrimary = NewRuntime(config, path); Call(partialPrimary, "LoadProgress");
            Require(partialPrimary.Coins == 91 && partialPrimary.Sold == 14 &&
                partialPrimary.SaveStatus.Contains("резервной копии"), "schema 1 partial DTO is rejected and valid backup restored");

            File.WriteAllText(path, "{broken-json");
            FactoryRuntime corruptPrimary = NewRuntime(config, path); Call(corruptPrimary, "LoadProgress");
            Require(corruptPrimary.Coins == 91 && corruptPrimary.Sold == 14 &&
                corruptPrimary.SaveStatus.Contains("резервной копии"), "corrupt primary restores valid backup and notice survives save");

            File.WriteAllText(path, "{broken-json");
            File.WriteAllText(path + ".bak", "{}");
            FactoryRuntime bothCorrupt = NewRuntime(config, path); Call(bothCorrupt, "LoadProgress");
            Require(bothCorrupt.Coins == 0 && bothCorrupt.SaveStatus.Contains("повреждено"), "both corrupt files create safe fresh state with notice");

            string invalidPath = Path.Combine(directory, "invalid-range.json");
            File.WriteAllText(invalidPath, "{\"schemaVersion\":1,\"coins\":-1,\"lifetimeSold\":0,\"speedLevel\":0,\"productivityLevel\":0,\"automationLevel\":0,\"rollerUnlocked\":false,\"sealerUnlocked\":false,\"harvestOnboardingComplete\":false,\"firstSaleOnboardingComplete\":false,\"firstPurchaseOnboardingComplete\":false,\"muted\":false}");
            FactoryRuntime invalidRange = NewRuntime(config, invalidPath); Call(invalidRange, "LoadProgress");
            Require(invalidRange.Coins == 0 && invalidRange.SaveStatus.Contains("повреждено"), "negative wallet numeric range is rejected as corrupt");

            string nestedPath = Path.Combine(directory, "nested-fields.json");
            File.WriteAllText(nestedPath, "{\"schemaVersion\":1,\"nested\":{\"coins\":7,\"lifetimeSold\":0,\"speedLevel\":0,\"productivityLevel\":0,\"automationLevel\":0,\"rollerUnlocked\":false,\"sealerUnlocked\":false,\"harvestOnboardingComplete\":false,\"firstSaleOnboardingComplete\":false,\"firstPurchaseOnboardingComplete\":false,\"muted\":false}}");
            FactoryRuntime nestedFields = NewRuntime(config, nestedPath); Call(nestedFields, "LoadProgress");
            Require(nestedFields.Coins == 0 && nestedFields.SaveStatus.Contains("повреждено"), "nested lookalike DTO fields do not satisfy root schema validation");

            string newerPath = Path.Combine(directory, "newer.json");
            const string future = "{\"schemaVersion\":2,\"coins\":123456,\"lifetimeSold\":50}";
            File.WriteAllText(newerPath, future);
            FactoryRuntime newer = NewRuntime(config, newerPath); Call(newer, "LoadProgress");
            Require(File.ReadAllText(newerPath) == future && newer.SaveStatus.Contains("новой версии"), "newer primary remains untouched and is classified separately");
            Call(newer, "SaveProgress");
            Require(File.Exists(Path.Combine(directory, "tiny-factory-save-fresh-v1.json")), "newer schema redirects writes to fresh slot");

            string futureBackup = Path.Combine(directory, "future-backup.json");
            File.WriteAllText(futureBackup, "{broken-json");
            File.WriteAllText(futureBackup + ".bak", future);
            FactoryRuntime backupNewer = NewRuntime(config, futureBackup); Call(backupNewer, "LoadProgress");
            Require(File.ReadAllText(futureBackup + ".bak") == future && backupNewer.SaveStatus.Contains("новой версии"), "newer backup is preserved and classified separately");

            string freshBase = Path.Combine(directory, "fresh-slot", "tiny-factory-save-fresh-v1.json");
            Directory.CreateDirectory(Path.GetDirectoryName(freshBase));
            FactoryRuntime freshSource = NewRuntime(config, freshBase);
            Set(freshSource, "coins", 41L); Call(freshSource, "SaveProgress");
            Set(freshSource, "coins", 51L); Call(freshSource, "SaveProgress");
            File.Delete(freshBase);
            string newerOriginal = Path.Combine(Path.GetDirectoryName(freshBase), "original.json");
            File.WriteAllText(newerOriginal, future);
            FactoryRuntime futureWithBackup = NewRuntime(config, newerOriginal); Call(futureWithBackup, "LoadProgress");
            Require(futureWithBackup.Coins == 41 && futureWithBackup.SaveStatus.Contains("новой версии"), "fresh slot missing primary restores its backup behind newer schema");
            Debug.Log("M040 SAVE CHECKS: full DTO round-trip, WIP discard, temp-write failure, missing-primary/partial/corrupt primary recovery, both-corrupt fallback, invalid ranges, root-key validation, future primary/backup preservation and fresh-slot backup recovery passed.");
        }

        private static void ValidateSceneAuthoring()
        {
            FactoryHudView hud = UnityEngine.Object.FindFirstObjectByType<FactoryHudView>();
            FactoryProductionLineView line = UnityEngine.Object.FindFirstObjectByType<FactoryProductionLineView>();
            Require(hud != null && line != null, "authored HUD and production view exist");
            SerializedObject hudObject = new SerializedObject(hud);
            Require(hudObject.FindProperty("productivityButton").objectReferenceValue != null &&
                hudObject.FindProperty("automationButton").objectReferenceValue != null &&
                hudObject.FindProperty("rollerStation").objectReferenceValue != null &&
                hudObject.FindProperty("originalFinalStation").objectReferenceValue != null &&
                hudObject.FindProperty("sealerStation").objectReferenceValue != null &&
                hudObject.FindProperty("rollerStatusLabel").objectReferenceValue != null &&
                hudObject.FindProperty("harvestButton").objectReferenceValue != null &&
                hudObject.FindProperty("harvestButtonLabel").objectReferenceValue != null, "new scene targets are serialized Inspector references");
            Button harvestButton = hudObject.FindProperty("harvestButton").objectReferenceValue as Button;
            TMPro.TMP_Text harvestLabel = hudObject.FindProperty("harvestButtonLabel").objectReferenceValue as TMPro.TMP_Text;
            Require(harvestButton != null && harvestLabel != null &&
                (harvestLabel.transform == harvestButton.transform || harvestLabel.transform.IsChildOf(harvestButton.transform)),
                "serialized batch label belongs to the authored harvest button");
            Button[] buttons = { GameObject.Find("Speed Upgrade Card").GetComponent<Button>(),
                GameObject.Find("Productivity Upgrade Card").GetComponent<Button>(),
                GameObject.Find("Automation Upgrade Card").GetComponent<Button>() };
            string[] handlers = { "HandleSpeedUpgradeButton", "HandleProductivityUpgradeButton", "HandleAutomationUpgradeButton" };
            for (int i = 0; i < buttons.Length; i++)
            {
                int matches = 0;
                for (int j = 0; j < buttons[i].onClick.GetPersistentEventCount(); j++)
                    if (buttons[i].onClick.GetPersistentTarget(j) == hud && buttons[i].onClick.GetPersistentMethodName(j) == handlers[i]) matches++;
                Require(matches == 1 && buttons[i].onClick.GetPersistentEventCount() == 1, handlers[i] + " persistent listener count exactly one");
            }
            SerializedObject lineObject = new SerializedObject(line);
            Require(lineObject.FindProperty("rollerWipAnchor").objectReferenceValue != null &&
                lineObject.FindProperty("rollerQueueSlots").arraySize == 8 &&
                lineObject.FindProperty("rollerProgressFill").objectReferenceValue != null &&
                lineObject.FindProperty("rollerProgressImage").objectReferenceValue != null,
                "roller buffer, WIP, and progress UI references serialized");

            FactoryRuntime runtime = UnityEngine.Object.FindFirstObjectByType<FactoryRuntime>();
            TMPro.TMP_Text actionHint = hudObject.FindProperty("firstActionLabel").objectReferenceValue as TMPro.TMP_Text;
            Require(runtime != null && actionHint != null, "runtime and route hint are serialized");
            long originalSold = runtime.Sold;
            string originalHint = actionHint.text;
            try
            {
                Set(runtime, "sold", 0L);
                Call(hud, "RefreshActionHint");
                Require(actionHint.text.Contains("Сушка → упаковка") && actionHint.text.EndsWith("+4"), "locked route hint reports the next sale tier +4");
                Set(runtime, "sold", 15L);
                Call(hud, "RefreshActionHint");
                Require(actionHint.text.Contains("Сушка → скрутка → упаковка") && actionHint.text.EndsWith("+5"), "roller route hint reports the next sale tier +5");
                Set(runtime, "sold", 40L);
                Call(hud, "RefreshActionHint");
                Require(actionHint.text.Contains("Сушка → скрутка → запайка") && actionHint.text.EndsWith("+6"), "sealer route hint reports the next sale tier +6");
                Debug.Log("M040 ACTION HINT CHECKS PASS: sold=0 uses dryer/packager +4; sold=15 uses dryer/roller/packager +5; sold=40 uses dryer/roller/sealer +6.");
            }
            finally
            {
                Set(runtime, "sold", originalSold);
                actionHint.text = originalHint;
            }
        }

        private static FactoryRuntime NewRuntime(FactoryBalanceConfig config, string savePath)
        {
            GameObject host = new GameObject("M040 Validation Host");
            host.SetActive(false);
            host.hideFlags = HideFlags.DontSave;
            Hosts.Add(host);
            FactoryRuntime runtime = host.AddComponent<FactoryRuntime>();
            Set(runtime, "config", config);
            Set(runtime, "savePath", savePath);
            return runtime;
        }

        private static void Set(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, PrivateInstance);
            Require(field != null, "private field exists " + name);
            field.SetValue(target, value);
        }

        private static void Call(object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, PrivateInstance);
            Require(info != null, "private method exists " + method);
            info.Invoke(target, null);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Cleanup()
        {
            for (int i = Hosts.Count - 1; i >= 0; i--)
            {
                if (Hosts[i] == null) continue;
                FieldInfo path = Hosts[i].GetComponent<FactoryRuntime>().GetType().GetField("savePath", PrivateInstance);
                if (path != null) path.SetValue(Hosts[i].GetComponent<FactoryRuntime>(), null);
                UnityEngine.Object.DestroyImmediate(Hosts[i]);
            }
            Hosts.Clear();
        }
    }
}
#endif
