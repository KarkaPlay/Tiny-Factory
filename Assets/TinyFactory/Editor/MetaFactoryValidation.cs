#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TinyFactory;
using UnityEditor;
using UnityEngine;

namespace TinyFactory.Editor
{
    internal static class MetaFactoryValidation
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static GameObject host;

        [MenuItem("Tiny Factory/Run M050 Validation")]
        private static void Run()
        {
            string root = Path.Combine(Path.GetTempPath(), "tinyfactory-m050-validation");
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
                Directory.CreateDirectory(root);
                MetaFactoryConfig config = Resources.Load<MetaFactoryConfig>("MetaFactoryConfig");
                Require(config != null && config.IsValid(), "authored M050 config validates");
                ValidateFreshTransactions(config, Path.Combine(root, "fresh"));
                ValidateProcessingTiming(config, Path.Combine(root, "timing"));
                ValidateForegroundTiming(config, Path.Combine(root, "foreground"));
                ValidatePersistenceBlock(config, Path.Combine(root, "persistence-block"));
                ValidateMigration(config, Path.Combine(root, "migration"));
                ValidateMigrationWalletCap(config, Path.Combine(root, "migration-wallet-cap"));
                ValidateMissingSchemaField(config, Path.Combine(root, "missing-field"));
                ValidateFutureSchema(config, Path.Combine(root, "future-schema"));
                Debug.Log("M050 VALIDATION PASS: config, fresh state, exactly-once turn-in, cancellation, UI state persistence, immediate 8-second WIP timing, foreground resume/hitch guards, persistence-block recovery notice, in-flight upgrade, v1 migration receipt and wallet-cap notice, missing-field recovery and future-schema preservation.");
            }
            catch (Exception exception) { Debug.LogError("M050 VALIDATION FAIL: " + exception); }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void ValidateFreshTransactions(MetaFactoryConfig config, string directory)
        {
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            Require(runtime.Coins == config.freshSaveCoins && runtime.Warehouse(MetaProduct.FreshLeaf) == config.freshStartQuantity, "fresh state uses configured wallet and stock");
            Require(runtime.OfferCount == 2 && runtime.Offers[0].kind == "onboarding" && runtime.Offers[0].lines[0].count == config.onboardingFreshLeafQuantity, "two initial offers and configured tutorial quantity");
            Require(!runtime.SetUiState(MetaStation.Dryer, MetaFactoryRuntime.PanelTab.Orders, config.localCapacity + 1, 0f), "invalid transfer UI state is rejected");
            Require(runtime.SetUiState(MetaStation.Garden, MetaFactoryRuntime.PanelTab.Orders, 2, -6f), "valid UI state commits");
            Require(runtime.SelectedTab == MetaFactoryRuntime.PanelTab.Orders && Mathf.Approximately(runtime.CameraVerticalOffset, -6f), "tab and world camera persist");
            Require(!runtime.CancelActiveOrder(false), "unconfirmed active-order cancellation is rejected");
            Require(runtime.AcceptOffer(0) && !runtime.AcceptOffer(0), "only one active order can be accepted");
            Require(runtime.CanTurnIn(out _) && runtime.TurnInActiveOrder(), "complete tutorial order turns in atomically");
            long paid = runtime.Coins;
            Require(!runtime.TurnInActiveOrder() && runtime.Coins == paid, "duplicate turn-in cannot pay twice");
            Require(runtime.Build(MetaStation.Dryer) && runtime.Coins == paid - config.dryerBuildCost, "dryer build uses configured price");
            Require(!runtime.Collect(MetaStation.Garden, int.MaxValue) && !runtime.Load(MetaStation.Dryer, int.MaxValue), "invalid transfers leave the state unchanged");
            long savedCoins = runtime.Coins;
            CleanupHost();
            MetaFactoryRuntime reloaded = NewRuntime(config, directory);
            Require(reloaded.Coins == savedCoins && reloaded.SelectedTab == MetaFactoryRuntime.PanelTab.Orders && Mathf.Approximately(reloaded.CameraVerticalOffset, -6f), "transaction and UI state survive reload");
        }

        private static void ValidateMigration(MetaFactoryConfig config, string directory)
        {
            Directory.CreateDirectory(directory);
            string legacy = Path.Combine(directory, "tiny-factory-save.json");
            File.WriteAllText(legacy, "{\"schemaVersion\":1,\"coins\":17,\"lifetimeSold\":0,\"speedLevel\":1,\"productivityLevel\":2,\"automationLevel\":3,\"rollerUnlocked\":false,\"sealerUnlocked\":false,\"harvestOnboardingComplete\":false,\"firstSaleOnboardingComplete\":false,\"firstPurchaseOnboardingComplete\":false,\"muted\":false}");
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            Require(runtime.MigrationPending && runtime.MigrationLegacyCoins == 17 && runtime.MigrationRefund == 288, "v1 migration computes persisted level refund");
            Require(runtime.MigrationResultCoins == 305 && runtime.Warehouse(MetaProduct.FreshLeaf) == config.freshStartQuantity, "migration starts new configured stock without fresh-save bonus");
            Require(runtime.ConfirmMigration() && !runtime.MigrationPending, "migration receipt commits before gameplay resumes");
            long migratedCoins = runtime.Coins;
            CleanupHost();
            MetaFactoryRuntime reloaded = NewRuntime(config, directory);
            Require(!reloaded.MigrationPending && reloaded.Coins == migratedCoins, "confirmed migration is not imported or refunded again");
        }

        private static void ValidateMigrationWalletCap(MetaFactoryConfig config, string directory)
        {
            Directory.CreateDirectory(directory);
            string legacy = Path.Combine(directory, "tiny-factory-save.json");
            File.WriteAllText(legacy, "{\"schemaVersion\":1,\"coins\":" + config.walletCapacity +
                ",\"lifetimeSold\":40,\"speedLevel\":3,\"productivityLevel\":3,\"automationLevel\":3,\"rollerUnlocked\":true,\"sealerUnlocked\":true,\"harvestOnboardingComplete\":false,\"firstSaleOnboardingComplete\":false,\"firstPurchaseOnboardingComplete\":false,\"muted\":false}");
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            Require(runtime.MigrationPending && runtime.MigrationLegacyCoins == config.walletCapacity && runtime.MigrationRefund == 486 && runtime.MigrationResultCoins == config.walletCapacity,
                "max-wallet v1 migration caps a three-track L3 refund");
            string notice = MetaFactoryHudView.FormatMigrationNotice(runtime.MigrationLegacyCoins, runtime.MigrationRefund, runtime.MigrationResultCoins,
                config.walletCapacity, config.freshStartQuantity, config.freshSaveCoins);
            Require(notice.Contains("зачислено из неё 0") && notice.Contains("лимит кошелька") && notice.Contains("новый стартовый бонус " + config.freshSaveCoins + " монет не добавляется"),
                "wallet-cap notice separates calculated from credited refund and states fresh-start policy");
            Require(runtime.Warehouse(MetaProduct.FreshLeaf) == config.freshStartQuantity && runtime.ConfirmMigration(), "capped migration confirms with only configured new-campaign stock");
            long migratedCoins = runtime.Coins;
            CleanupHost();
            MetaFactoryRuntime reloaded = NewRuntime(config, directory);
            Require(!reloaded.MigrationPending && reloaded.MigrationReceipt && reloaded.Coins == migratedCoins, "capped migration cannot refund a second time after reload");
        }

        private static void ValidateProcessingTiming(MetaFactoryConfig config, string directory)
        {
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            Require(runtime.AcceptOffer(0) && runtime.TurnInActiveOrder(), "onboarding reward funds the dryer build");
            Require(runtime.Build(MetaStation.Dryer), "dryer build prerequisite passes");
            runtime.AdvanceForHarness(config.gardenSecondsByLevel[0] * 2);
            Require(runtime.Station(MetaStation.Garden).output == 2 && runtime.Collect(MetaStation.Garden, 2), "garden output can be collected for a dryer batch");
            Require(runtime.Load(MetaStation.Dryer, config.freshLeafPerBatch), "successful load starts WIP in the same transaction");
            Require(runtime.Station(MetaStation.Dryer).remainingSeconds == config.dryerSecondsByLevel[0] && runtime.Station(MetaStation.Dryer).reservedInput == config.freshLeafPerBatch, "initial WIP reserves a full recipe and full timer");
            int fullRefreshes = 0, timerRefreshes = 0;
            Action onFullRefresh = () => fullRefreshes++;
            Action onTimerRefresh = () => timerRefreshes++;
            runtime.Changed += onFullRefresh;
            runtime.TimerChanged += onTimerRefresh;
            runtime.AdvanceForHarness(1);
            runtime.Changed -= onFullRefresh;
            runtime.TimerChanged -= onTimerRefresh;
            Require(fullRefreshes == 0 && timerRefreshes == 1, "timer-only tick refreshes station labels without rebuilding the offer board");
            runtime.AdvanceForHarness(2);
            int remainingBeforeUpgrade = runtime.Station(MetaStation.Dryer).remainingSeconds;
            Require(runtime.Upgrade(MetaStation.Dryer) && runtime.Station(MetaStation.Dryer).remainingSeconds == remainingBeforeUpgrade, "upgrade does not retime the running recipe");
            runtime.AdvanceForHarness(remainingBeforeUpgrade - 1);
            Require(runtime.Station(MetaStation.Dryer).output == 0 && runtime.Station(MetaStation.Dryer).remainingSeconds == 1, "output remains unavailable one second before completion");
            runtime.AdvanceForHarness(1);
            Require(runtime.Station(MetaStation.Dryer).output == 1 && runtime.Station(MetaStation.Dryer).remainingSeconds == 0, "output becomes ready exactly at the saved recipe boundary");
        }

        private static void ValidateForegroundTiming(MetaFactoryConfig config, string directory)
        {
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            Require(runtime.AcceptOffer(0) && runtime.TurnInActiveOrder() && runtime.Build(MetaStation.Dryer), "foreground test builds dryer through valid transactions");
            runtime.AdvanceForHarness(config.gardenSecondsByLevel[0] * 2);
            Require(runtime.Collect(MetaStation.Garden, 2) && runtime.Load(MetaStation.Dryer, config.freshLeafPerBatch), "foreground test starts a dryer recipe");
            int before = runtime.Station(MetaStation.Dryer).remainingSeconds;
            FieldInfo accumulator = typeof(MetaFactoryRuntime).GetField("tickAccumulator", PrivateInstance);
            MethodInfo advance = typeof(MetaFactoryRuntime).GetMethod("AdvanceForegroundFrame", PrivateInstance);
            MethodInfo pause = typeof(MetaFactoryRuntime).GetMethod("OnApplicationPause", PrivateInstance);
            MethodInfo focus = typeof(MetaFactoryRuntime).GetMethod("OnApplicationFocus", PrivateInstance);
            accumulator.SetValue(runtime, .75f);
            pause.Invoke(runtime, new object[] { true });
            Require(Mathf.Approximately((float)accumulator.GetValue(runtime), 0f), "pause discards partial foreground accumulator");
            pause.Invoke(runtime, new object[] { false });
            advance.Invoke(runtime, new object[] { 50f });
            Require(runtime.Station(MetaStation.Dryer).remainingSeconds == before && Mathf.Approximately((float)accumulator.GetValue(runtime), 0f), "first frame after pause/resume is skipped");
            accumulator.SetValue(runtime, .75f);
            focus.Invoke(runtime, new object[] { false });
            Require(Mathf.Approximately((float)accumulator.GetValue(runtime), 0f), "focus loss discards partial foreground accumulator");
            focus.Invoke(runtime, new object[] { true });
            advance.Invoke(runtime, new object[] { 50f });
            Require(runtime.Station(MetaStation.Dryer).remainingSeconds == before && Mathf.Approximately((float)accumulator.GetValue(runtime), 0f), "first frame after focus return is skipped");
            advance.Invoke(runtime, new object[] { 50f });
            Require(runtime.Station(MetaStation.Dryer).remainingSeconds == before && Mathf.Approximately((float)accumulator.GetValue(runtime), .25f), "foreground hitch is clamped to one quarter second");
            for (int i = 0; i < 3; i++) advance.Invoke(runtime, new object[] { .25f });
            Require(runtime.Station(MetaStation.Dryer).remainingSeconds == before - 1, "only one real foreground second advances after four clamped frames");
        }

        private static void ValidatePersistenceBlock(MetaFactoryConfig config, string directory)
        {
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            string savePath = Path.Combine(directory, "tiny-factory-meta-save-v2.json");
            long coinsBefore = runtime.Coins;
            string saveBefore = File.ReadAllText(savePath);
            Directory.CreateDirectory(savePath + ".tmp");
            bool committed = runtime.SetUiState(MetaStation.Garden, MetaFactoryRuntime.PanelTab.Factory, 0, 0f);
            Require(!committed && runtime.PersistenceBlocked && runtime.Coins == coinsBefore && File.ReadAllText(savePath) == saveBefore,
                "failed persistence blocks actions without changing memory or primary save");
            Require(runtime.StatusText.Contains("перезапустите игру"), "persistence block tells player to restart after fixing storage");
            Require(!runtime.SetUiState(MetaStation.Garden, MetaFactoryRuntime.PanelTab.Warehouse, 0, 0f), "blocked persistence cannot report a later transaction as successful");
        }

        private static void ValidateMissingSchemaField(MetaFactoryConfig config, string directory)
        {
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            string path = Path.Combine(directory, "tiny-factory-meta-save-v2.json");
            string json = File.ReadAllText(path).Replace("\"coins\":80,", string.Empty);
            File.WriteAllText(path, json);
            UnityEngine.Object.DestroyImmediate(host);
            host = null;
            MetaFactoryRuntime recovered = NewRuntime(config, directory);
            Require(recovered.Coins == config.freshSaveCoins && recovered.Warehouse(MetaProduct.FreshLeaf) == config.freshStartQuantity, "schema2 with missing required wallet field is rejected and recovered safely");
        }

        private static void ValidateFutureSchema(MetaFactoryConfig config, string directory)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "tiny-factory-meta-save-v2.json");
            File.WriteAllText(path, "{\"schemaVersion\":99,\"futurePayload\":true}");
            MetaFactoryRuntime runtime = NewRuntime(config, directory);
            Require(runtime.Coins == config.freshSaveCoins && File.ReadAllText(path).Contains("futurePayload"), "future schema with changed fields is preserved and isolated from gameplay");
        }

        private static MetaFactoryRuntime NewRuntime(MetaFactoryConfig config, string directory)
        {
            CleanupHost();
            Directory.CreateDirectory(directory);
            host = new GameObject("M050 Validation Runtime");
            host.SetActive(false);
            host.hideFlags = HideFlags.HideAndDontSave;
            MetaFactoryRuntime runtime = host.AddComponent<MetaFactoryRuntime>();
            typeof(MetaFactoryRuntime).GetField("config", PrivateInstance).SetValue(runtime, config);
            typeof(MetaFactoryRuntime).GetField("savePath", PrivateInstance).SetValue(runtime, Path.Combine(directory, "tiny-factory-meta-save-v2.json"));
            typeof(MetaFactoryRuntime).GetMethod("LoadOrMigrate", PrivateInstance).Invoke(runtime, null);
            return runtime;
        }

        private static void CleanupHost()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            host = null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
