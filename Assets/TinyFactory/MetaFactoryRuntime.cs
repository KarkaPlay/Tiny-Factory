using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyFactory
{
    /// <summary>Owns the active-only meta pilot state and commits each player action as one save transaction.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MetaFactoryRuntime : MonoBehaviour
    {
        public enum PanelTab { Factory, Warehouse, Orders }
        private const int Schema = 2;
        private const string MetaFile = "tiny-factory-meta-save-v2.json";
        private const string RecoveryFile = "tiny-factory-meta-recovery-v2.json";
        private const string MigrationGuardFile = "tiny-factory-meta-migration.guard";
        private const string LegacyFile = "tiny-factory-save.json";
        private const long LegacyCap = 1000000000000L;
        private const int MaxCompletedIds = 2048;
        // Foreground simulation advances by at most a quarter-second per frame. Time spent in a hitch is dropped.
        private const float MaxForegroundDeltaSeconds = 0.25f;

        [Serializable] public sealed class ProductCount { public MetaProduct product; public int count; }
        [Serializable] public sealed class OrderLine { public MetaProduct product; public int count; }
        [Serializable] public sealed class OrderOffer
        {
            public string id;
            public string kind;
            public long reward;
            public List<OrderLine> lines = new List<OrderLine>();
            public bool IsPilotGoal => kind == "pilot";
        }
        [Serializable] public sealed class StationState
        {
            public MetaStation station;
            public bool built;
            public int level;
            public int input;
            public int output;
            public int remainingSeconds;
            public int gardenRemainingSeconds;
            public int reservedInput;
            public bool outputBlocked;
        }
        [Serializable] private sealed class SaveState
        {
            public int schemaVersion = Schema;
            public long coins;
            public bool muted;
            public bool tutorialComplete;
            public int tutorialStep;
            public int lifetimeDryTeaProduced;
            public bool pilotGoalComplete;
            public List<ProductCount> warehouse = new List<ProductCount>();
            public List<StationState> stations = new List<StationState>();
            public List<OrderOffer> offers = new List<OrderOffer>();
            public OrderOffer activeOrder;
            public List<string> completedOrderIds = new List<string>();
            public int nextOfferId = 1;
            public uint rngState = 32;
            public bool migrationReceipt;
            public long migrationLegacyCoins;
            public int migrationRefund;
            public long lastCheckpointUtcTicks;
            public MetaStation selectedStation = MetaStation.Garden;
            public PanelTab selectedTab = PanelTab.Orders;
            public int transferQuantity;
            public float cameraVerticalOffset;
        }
        [Serializable] private sealed class LegacyV1
        {
            public int schemaVersion;
            public long coins;
            public long lifetimeSold;
            public int speedLevel;
            public int productivityLevel;
            public int automationLevel;
            public bool rollerUnlocked;
            public bool sealerUnlocked;
            public bool harvestOnboardingComplete;
            public bool firstSaleOnboardingComplete;
            public bool firstPurchaseOnboardingComplete;
            public bool muted;
        }

        [SerializeField] private MetaFactoryConfig config;
        [SerializeField] private string statusText;
        [SerializeField] private bool mute;
#if UNITY_EDITOR
        [SerializeField] private string editorSaveDirectoryOverride;
#endif
        private SaveState state;
        private string savePath;
        private float tickAccumulator;
        private bool foreground = true;
        private bool skipNextForegroundFrame = true;
        private bool paused;
        private bool focused = true;
        private bool persistenceBlocked;
        private bool migrationPending;
        private bool legacyMigrationDisabled;

        public event Action Changed;
        public event Action TimerChanged;
        public event Action<string> Notice;
        public event Action<MetaStation, bool, int> TransferCommitted;
        public long Coins => state?.coins ?? 0;
        public bool Muted => state != null && state.muted;
        public bool TutorialComplete => state != null && state.tutorialComplete;
        public int TutorialStep => state?.tutorialStep ?? 0;
        public int LifetimeDryTeaProduced => state?.lifetimeDryTeaProduced ?? 0;
        public bool PilotGoalComplete => state != null && state.pilotGoalComplete;
        public bool MigrationReceipt => state != null && state.migrationReceipt;
        public bool MigrationPending => migrationPending;
        public long MigrationLegacyCoins => state?.migrationLegacyCoins ?? 0;
        public int MigrationRefund => state?.migrationRefund ?? 0;
        public long MigrationResultCoins => state?.coins ?? 0;
        public int OfferCount => state?.offers?.Count ?? 0;
        public OrderOffer ActiveOrder => state?.activeOrder;
        public IReadOnlyList<OrderOffer> Offers => state?.offers;
        public IReadOnlyList<StationState> Stations => state?.stations;
        public string StatusText => statusText ?? string.Empty;
        public bool IsForeground => foreground;
        public bool PersistenceBlocked => persistenceBlocked;
        public MetaFactoryConfig Config => config;

        private void Awake()
        {
            if (config == null) config = Resources.Load<MetaFactoryConfig>("MetaFactoryConfig");
            if (config == null || !config.IsValid())
            {
                Debug.LogError("MetaFactoryRuntime requires a valid MetaFactoryConfig asset.", this);
                enabled = false;
                return;
            }
#if UNITY_EDITOR
            string saveDirectory = string.IsNullOrWhiteSpace(editorSaveDirectoryOverride) ? Application.persistentDataPath : editorSaveDirectoryOverride;
#else
            string saveDirectory = Application.persistentDataPath;
#endif
            savePath = Path.Combine(saveDirectory, MetaFile);
            LoadOrMigrate();
            mute = state != null && state.muted;
        }

        private void Update()
        {
            AdvanceForegroundFrame(Time.unscaledDeltaTime);
        }

        private void AdvanceForegroundFrame(float unscaledDelta)
        {
            if (!foreground || state == null || persistenceBlocked || migrationPending)
            {
                tickAccumulator = 0f;
                return;
            }
            if (skipNextForegroundFrame)
            {
                skipNextForegroundFrame = false;
                tickAccumulator = 0f;
                return;
            }
            if (float.IsNaN(unscaledDelta) || float.IsInfinity(unscaledDelta) || unscaledDelta <= 0f) return;
            tickAccumulator += Mathf.Min(unscaledDelta, MaxForegroundDeltaSeconds);
            while (tickAccumulator >= 1f)
            {
                tickAccumulator -= 1f;
                AdvanceOneSecond();
            }
        }

        public int Warehouse(MetaProduct product) => FindCount(state?.warehouse, product);
        public StationState Station(MetaStation station) => FindStation(state?.stations, station);
        public int WarehouseTotal => Sum(state?.warehouse);

        public bool Collect(MetaStation station, int quantity)
        {
            bool committed = Transact(candidate =>
            {
                StationState local = FindStation(candidate.stations, station);
                if (local == null || !local.built || quantity <= 0 || quantity > local.output) return false;
                MetaProduct product = OutputProduct(station);
                if (!CanAddWarehouse(candidate, product, quantity)) return false;
                local.output -= quantity;
                AddCount(candidate.warehouse, product, quantity);
                if (station == MetaStation.Dryer && product == MetaProduct.DryTea && candidate.tutorialStep == 4)
                    AdvanceTutorial(candidate, 4);
                return true;
            });
            if (committed) NotifyTransferCommitted(station, false, quantity);
            return committed;
        }

        public bool Load(MetaStation station, int quantity)
        {
            bool committed = Transact(candidate =>
            {
                StationState local = FindStation(candidate.stations, station);
                if (local == null || !local.built || station == MetaStation.Garden || quantity <= 0 ||
                    quantity > Warehouse(candidate.warehouse, InputProduct(station)) || local.input + quantity > config.localCapacity) return false;
                AddCount(candidate.warehouse, InputProduct(station), -quantity);
                local.input += quantity;
                TryStartProcessing(local, station);
                if (station == MetaStation.Dryer && candidate.tutorialStep == 3 && (local.reservedInput > 0 || local.output > 0))
                {
                    AdvanceTutorial(candidate, 3);
                    if (local.output > 0) AdvanceTutorial(candidate, 4);
                }
                return true;
            });
            if (committed) NotifyTransferCommitted(station, true, quantity);
            return committed;
        }

        private void NotifyTransferCommitted(MetaStation station, bool loading, int quantity)
        {
            try { TransferCommitted?.Invoke(station, loading, quantity); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        public int MaxLoad(MetaStation station)
        {
            if (station == MetaStation.Garden) return 0;
            StationState local = Station(station);
            if (local == null || !local.built) return 0;
            int limit = Math.Min(config.localCapacity - local.input, Warehouse(InputProduct(station)));
            return limit;
        }

        public int MaxCollect(MetaStation station)
        {
            StationState local = Station(station);
            if (local == null || !local.built) return 0;
            MetaProduct product = OutputProduct(station);
            return Math.Min(local.output, Math.Min(config.warehouseProductCapacity - Warehouse(product), config.warehouseTotalCapacity - WarehouseTotal));
        }

        public bool Build(MetaStation station)
        {
            return Transact(candidate =>
            {
                StationState local = FindStation(candidate.stations, station);
                if (local == null || local.built || station == MetaStation.Garden) return false;
                long cost;
                if (station == MetaStation.Dryer) cost = config.dryerBuildCost;
                else
                {
                    if (candidate.lifetimeDryTeaProduced < config.dryerUnlockPackerAtProduced) return false;
                    cost = config.packerBuildCost;
                }
                if (candidate.coins < cost) return false;
                candidate.coins -= cost;
                local.built = true;
                if (station == MetaStation.Dryer && candidate.tutorialStep == 2) AdvanceTutorial(candidate, 2);
                EnsureOffers(candidate);
                return true;
            });
        }

        public long NextUpgradeCost(MetaStation station)
        {
            StationState local = Station(station);
            if (local == null || !local.built || local.level < 0 || local.level >= 2) return -1;
            long[] costs = UpgradeCosts(station);
            return costs[local.level];
        }

        public bool Upgrade(MetaStation station)
        {
            return Transact(candidate =>
            {
                StationState local = FindStation(candidate.stations, station);
                if (local == null || !local.built || local.level < 0 || local.level >= 2) return false;
                long cost = UpgradeCosts(station)[local.level];
                if (candidate.coins < cost) return false;
                candidate.coins -= cost;
                local.level++;
                return true;
            });
        }

        public bool AcceptOffer(int index)
        {
            return Transact(candidate =>
            {
                if (candidate.activeOrder != null || index < 0 || index >= candidate.offers.Count) return false;
                OrderOffer selected = candidate.offers[index];
                candidate.offers.RemoveAt(index);
                candidate.activeOrder = CloneOffer(selected);
                if (selected.kind == "onboarding" && candidate.tutorialStep == 0) AdvanceTutorial(candidate, 0);
                EnsureOffers(candidate);
                return true;
            });
        }

        public bool CancelActiveOrder(bool confirmed)
        {
            if (!confirmed) return false;
            return Transact(candidate =>
            {
                if (candidate.activeOrder == null) return false;
                bool isPilot = candidate.activeOrder.IsPilotGoal;
                candidate.activeOrder = null;
                if (isPilot && !candidate.pilotGoalComplete) PutPilotOfferInSlot2(candidate);
                EnsureOffers(candidate);
                return true;
            });
        }

        public bool ReplaceOffer(int index)
        {
            return Transact(candidate =>
            {
                if (index != 1 || index >= candidate.offers.Count ||
                    candidate.offers[index].IsPilotGoal) return false;
                candidate.offers[index] = GenerateOrdinaryOffer(candidate, index);
                return true;
            });
        }

        public bool CanTurnIn(out string shortfall)
        {
            shortfall = string.Empty;
            if (state?.activeOrder == null) return false;
            for (int i = 0; i < state.activeOrder.lines.Count; i++)
            {
                OrderLine line = state.activeOrder.lines[i];
                int missing = line.count - Warehouse(line.product);
                if (missing > 0) shortfall += (shortfall.Length == 0 ? "" : " · ") + ProductDisplayName(line.product) + " ×" + missing;
            }
            return shortfall.Length == 0;
        }

        public bool TurnInActiveOrder()
        {
            return Transact(candidate =>
            {
                OrderOffer order = candidate.activeOrder;
                if (order == null || candidate.completedOrderIds.Contains(order.id)) return false;
                for (int i = 0; i < order.lines.Count; i++)
                    if (Warehouse(candidate.warehouse, order.lines[i].product) < order.lines[i].count) return false;
                for (int i = 0; i < order.lines.Count; i++) AddCount(candidate.warehouse, order.lines[i].product, -order.lines[i].count);
                candidate.coins = Math.Min(config.walletCapacity, SaturatingAdd(candidate.coins, order.reward, config.walletCapacity));
                candidate.completedOrderIds.Add(order.id);
                if (candidate.completedOrderIds.Count > MaxCompletedIds) candidate.completedOrderIds.RemoveAt(0);
                if (order.IsPilotGoal) candidate.pilotGoalComplete = true;
                candidate.activeOrder = null;
                if (order.kind == "onboarding" && candidate.tutorialStep == 1)
                {
                    AdvanceTutorial(candidate, 1);
                    StationState dryer = FindStation(candidate.stations, MetaStation.Dryer);
                    if (dryer.built) AdvanceTutorial(candidate, 2);
                    if (dryer.reservedInput > 0) AdvanceTutorial(candidate, 3);
                    if (dryer.output > 0) { AdvanceTutorial(candidate, 3); AdvanceTutorial(candidate, 4); }
                }
                EnsureOffers(candidate);
                return true;
            });
        }

        private static void AdvanceTutorial(SaveState candidate, int expectedStep)
        {
            if (candidate.tutorialComplete || candidate.tutorialStep != expectedStep) return;
            candidate.tutorialStep++;
            if (candidate.tutorialStep >= 5) candidate.tutorialComplete = true;
        }

        public bool SetMuted(bool value)
        {
            bool saved = Transact(candidate => { if (candidate.muted == value) return false; candidate.muted = value; return true; });
            mute = state != null && state.muted;
            return saved;
        }

        public bool SetUiState(MetaStation selectedStation, PanelTab selectedTab, int transferQuantity, float cameraVerticalOffset)
        {
            if (!Enum.IsDefined(typeof(MetaStation), selectedStation) || !Enum.IsDefined(typeof(PanelTab), selectedTab) ||
                transferQuantity < 0 || transferQuantity > config.localCapacity || float.IsNaN(cameraVerticalOffset) ||
                float.IsInfinity(cameraVerticalOffset) || cameraVerticalOffset < -12f || cameraVerticalOffset > 0f) return false;
            return Transact(candidate =>
            {
                if (candidate.selectedStation == selectedStation && candidate.selectedTab == selectedTab &&
                    candidate.transferQuantity == transferQuantity && Mathf.Approximately(candidate.cameraVerticalOffset, cameraVerticalOffset)) return false;
                candidate.selectedStation = selectedStation;
                candidate.selectedTab = selectedTab;
                candidate.transferQuantity = transferQuantity;
                candidate.cameraVerticalOffset = cameraVerticalOffset;
                return true;
            });
        }

        public MetaStation SelectedStation => state == null ? MetaStation.Garden : state.selectedStation;
        public PanelTab SelectedTab => state == null ? PanelTab.Factory : state.selectedTab;
        public int TransferQuantity => state == null ? 0 : state.transferQuantity;
        public float CameraVerticalOffset => state == null ? 0f : state.cameraVerticalOffset;

        public bool ConfirmMigration()
        {
            if (!migrationPending || state == null || persistenceBlocked) return false;
            SaveState candidate = Clone(state);
            candidate.lastCheckpointUtcTicks = DateTime.UtcNow.Ticks;
            if (!WriteState(candidate))
            {
                BlockPersistence("Миграция не сохранена; старое сохранение 0.4 осталось нетронутым");
                return false;
            }
            state = candidate;
            migrationPending = false;
            statusText = string.Empty;
            Changed?.Invoke();
            return true;
        }

        public void AdvanceForHarness(int seconds)
        {
            if (seconds < 0 || seconds > 86400) throw new ArgumentOutOfRangeException(nameof(seconds));
            for (int i = 0; i < seconds; i++) AdvanceOneSecond();
        }

        private void AdvanceOneSecond()
        {
            if (state == null || persistenceBlocked || migrationPending) return;
            SaveState next = Clone(state);
            bool changed = false;
            StationState garden = FindStation(next.stations, MetaStation.Garden);
            if (garden.built)
            {
                if (garden.gardenRemainingSeconds > 0) garden.gardenRemainingSeconds--;
                if (garden.gardenRemainingSeconds <= 0)
                {
                    if (garden.output < config.localCapacity) { garden.output++; changed = true; }
                    garden.gardenRemainingSeconds = config.gardenSecondsByLevel[garden.level];
                }
            }
            changed |= AdvanceProcessing(next, MetaStation.Dryer);
            changed |= AdvanceProcessing(next, MetaStation.Packer);
            if (changed)
            {
                Commit(next);
            }
            else
            {
                // Timer state is persistent for cold restart. Persisting a one-second checkpoint is intentional for this small save.
                SaveState timerOnly = Clone(next);
                timerOnly.lastCheckpointUtcTicks = DateTime.UtcNow.Ticks;
                if (!WriteState(timerOnly)) BlockPersistence("Не удалось сохранить таймер производства");
                else
                {
                    state = timerOnly;
                    TimerChanged?.Invoke();
                }
            }
        }

        private bool AdvanceProcessing(SaveState candidate, MetaStation station)
        {
            StationState local = FindStation(candidate.stations, station);
            if (local == null || !local.built) return false;
            if (local.remainingSeconds > 0) local.remainingSeconds--;
            bool changed = false;
            if (local.remainingSeconds == 0 && local.reservedInput > 0)
            {
                if (local.output < config.localCapacity)
                {
                    local.output++;
                    local.reservedInput = 0;
                    local.outputBlocked = false;
                    if (station == MetaStation.Dryer) candidate.lifetimeDryTeaProduced++;
                    changed = true;
                }
                else local.outputBlocked = true;
            }
            changed |= TryStartProcessing(local, station);
            return changed;
        }

        private bool TryStartProcessing(StationState local, MetaStation station)
        {
            if (local.reservedInput != 0 || local.remainingSeconds != 0 || local.input < RecipeSize(station) || local.output >= config.localCapacity) return false;
            int consumed = RecipeSize(station);
            local.input -= consumed;
            local.reservedInput = consumed;
            local.remainingSeconds = ProcessingSeconds(station, local.level);
            local.outputBlocked = false;
            return true;
        }

        private int RecipeSize(MetaStation station) => station == MetaStation.Dryer ? config.freshLeafPerBatch : 1;
        private static string ProductDisplayName(MetaProduct product) => product == MetaProduct.FreshLeaf ? "Свежий лист" :
            product == MetaProduct.DryTea ? "Сухой чай" : product == MetaProduct.TeaPacket ? "Пакетики" : "Товар";
        private int ProcessingSeconds(MetaStation station, int level) => station == MetaStation.Dryer ? config.dryerSecondsByLevel[level] : config.packerSecondsByLevel[level];
        private long[] UpgradeCosts(MetaStation station) => station == MetaStation.Garden ? config.gardenUpgradeCosts : station == MetaStation.Dryer ? config.dryerUpgradeCosts : config.packerUpgradeCosts;

        private bool Transact(Func<SaveState, bool> operation)
        {
            if (state == null || persistenceBlocked || migrationPending || !foreground) return false;
            SaveState candidate = Clone(state);
            if (!operation(candidate) || !Validate(candidate)) return false;
            return Commit(candidate);
        }

        private bool Commit(SaveState candidate)
        {
            candidate.lastCheckpointUtcTicks = DateTime.UtcNow.Ticks;
            if (!WriteState(candidate))
            {
                BlockPersistence("Не удалось записать сохранение; действие отменено");
                return false;
            }
            state = candidate;
            statusText = string.Empty;
            Changed?.Invoke();
            return true;
        }

        private void BlockPersistence(string reason)
        {
            persistenceBlocked = true;
            tickAccumulator = 0f;
            statusText = reason + ". Игровое состояние остановлено; перезапустите игру после проверки свободного места и доступа к файлам.";
            Notice?.Invoke(statusText);
            Changed?.Invoke();
        }

        private bool WriteState(SaveState candidate)
        {
            string temp = savePath + ".tmp";
            string backup = savePath + ".bak";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                File.WriteAllText(temp, JsonUtility.ToJson(candidate));
                SaveState check = ReadAndValidate(temp, allowNewer: false);
                if (!Validate(check)) throw new InvalidDataException("Temporary save failed state validation.");
                if (File.Exists(savePath))
                {
                    // Preserve a future backup before installing the current verified primary as backup.
                    if (File.Exists(backup))
                    {
                        try { ReadAndValidate(backup, allowNewer: true); }
                        catch (NewerSchemaException)
                        {
                            string archive = UniqueArchivePath(backup, ".schema-unknown-", ".json");
                            File.Copy(backup, archive, false);
                        }
                        catch { /* Invalid backup can be replaced by the verified primary. */ }
                    }
                    SaveState current = ReadAndValidate(savePath, allowNewer: false);
                    if (Validate(current)) File.Copy(savePath, backup, true);
                }
                else if (File.Exists(backup))
                {
                    try { ReadAndValidate(backup, allowNewer: true); }
                    catch (NewerSchemaException)
                    {
                        string archive = UniqueArchivePath(backup, ".schema-unknown-", ".json");
                        File.Copy(backup, archive, false);
                        throw new InvalidDataException("Future backup preserved; there is no verified primary to replace it.");
                    }
                    catch { /* Invalid backup can be replaced by the current verified primary. */ }
                }
                if (File.Exists(savePath))
                {
                    // Preserve a primary with an unknown schema; callers must never overwrite it.
                    try { ReadAndValidate(savePath, allowNewer: true); }
                    catch (NewerSchemaException) { throw; }
                }
                if (File.Exists(savePath)) File.Delete(savePath);
                File.Move(temp, savePath);
                if (candidate.migrationReceipt && !WriteMigrationGuard()) throw new IOException("Could not persist migration guard.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Meta save commit failed; current in-memory action was not applied: " + exception.Message, this);
                return false;
            }
        }

        private void LoadOrMigrate()
        {
            string saveDirectory = Path.GetDirectoryName(savePath);
            string guardPath = Path.Combine(saveDirectory, MigrationGuardFile);
            legacyMigrationDisabled = File.Exists(guardPath);
            if (legacyMigrationDisabled)
            {
                string recoveryPath = Path.Combine(saveDirectory, RecoveryFile);
                if (File.Exists(recoveryPath)) savePath = recoveryPath;
            }
            string backup = savePath + ".bak";
            if (File.Exists(savePath) || File.Exists(backup))
            {
                try { state = ReadAndValidate(savePath, allowNewer: true); return; }
                catch (NewerSchemaException e) { PreserveUnsupported(e.Path); return; }
                catch (Exception primaryError)
                {
                    try
                    {
                        state = ReadAndValidate(backup, allowNewer: true);
                        statusText = "Прогресс восстановлен из резервной копии";
                        Notice?.Invoke(statusText);
                        if (!RestorePrimaryFromBackup())
                        {
                            BlockPersistence("Резервная копия прочитана, но восстановить основной файл не удалось");
                        }
                        return;
                    }
                    catch (NewerSchemaException e) { PreserveUnsupported(e.Path); return; }
                    catch (Exception backupError)
                    {
                        if (!WriteMigrationGuard())
                        {
                            BlockPersistence("Сохранения повреждены; старый слот оставлен без изменений для защиты миграции");
                            return;
                        }
                        legacyMigrationDisabled = true;
                        ArchiveIfPresent(savePath, ".corrupt-"); ArchiveIfPresent(backup, ".corrupt-");
                        statusText = "Сохранение повреждено; восстановлена новая игра";
                        Debug.LogWarning("Meta save and backup invalid: " + primaryError.Message + " / " + backupError.Message, this);
                        Notice?.Invoke(statusText);
                        state = FreshState();
                        if (!WriteState(state)) BlockPersistence("Не удалось сохранить восстановленное состояние");
                        return;
                    }
                }
            }
            if (legacyMigrationDisabled)
            {
                state = FreshState();
                if (!WriteState(state)) BlockPersistence("Не удалось создать сохранение новой кампании");
                return;
            }
            SaveState migrated = TryMigrateV1();
            state = migrated ?? FreshState();
            if (migrated != null)
            {
                migrationPending = true;
                long creditedLegacy = Math.Min(migrated.migrationLegacyCoins, config.walletCapacity);
                long creditedRefund = Math.Max(0L, migrated.coins - creditedLegacy);
                statusText = "0.4: баланс " + migrated.migrationLegacyCoins + " монет; расчётная компенсация старых уровней " + migrated.migrationRefund +
                    ", зачислено из неё " + creditedRefund + "; итог " + migrated.coins + " при лимите кошелька " + config.walletCapacity +
                    ". Запасы и заказы новой кампании начинаются сначала.";
                Notice?.Invoke(statusText);
            }
            else if (!WriteState(state)) BlockPersistence("Не удалось создать сохранение новой кампании");
        }

        private SaveState TryMigrateV1()
        {
            string legacy = Path.Combine(Path.GetDirectoryName(savePath), LegacyFile);
            string[] candidates = { legacy, legacy + ".bak" };
            LegacyV1 old = null;
            string[] required = { "schemaVersion", "coins", "lifetimeSold", "speedLevel", "productivityLevel", "automationLevel", "rollerUnlocked", "sealerUnlocked", "harvestOnboardingComplete", "firstSaleOnboardingComplete", "firstPurchaseOnboardingComplete", "muted" };
            foreach (string file in candidates)
            {
                if (!File.Exists(file)) continue;
                try
                {
                    string json = File.ReadAllText(file);
                    for (int i = 0; i < required.Length; i++) if (!HasRootField(json, required[i])) throw new InvalidDataException("Legacy field missing: " + required[i]);
                    LegacyV1 read = JsonUtility.FromJson<LegacyV1>(json);
                    if (read == null || read.schemaVersion != 1 || read.coins < 0 || read.coins > LegacyCap || read.lifetimeSold < 0 || read.lifetimeSold > LegacyCap ||
                        read.speedLevel < 0 || read.speedLevel > 3 || read.productivityLevel < 0 || read.productivityLevel > 3 || read.automationLevel < 0 || read.automationLevel > 3)
                        throw new InvalidDataException("Legacy save values invalid.");
                    if (read.rollerUnlocked != (read.lifetimeSold >= 15) || read.sealerUnlocked != (read.lifetimeSold >= 40))
                        throw new InvalidDataException("Legacy unlock flags do not match lifetime sales.");
                    old = read; break;
                }
                catch (Exception error) { Debug.LogWarning("Legacy candidate invalid: " + file + " / " + error.Message, this); }
            }
            if (old == null) return null;
            int refund = (old.speedLevel + old.productivityLevel + old.automationLevel) * (36 + 54 + 72) / 3;
            // Exact old price table: each level's cost was 36, 54, then 72.
            refund = RefundForLevel(old.speedLevel) + RefundForLevel(old.productivityLevel) + RefundForLevel(old.automationLevel);
            SaveState migrated = FreshState();
            migrated.coins = Math.Min(config.walletCapacity, SaturatingAdd(old.coins, refund, config.walletCapacity));
            migrated.muted = old.muted;
            migrated.migrationReceipt = true;
            migrated.migrationLegacyCoins = old.coins;
            migrated.migrationRefund = refund;
            return migrated;
        }

        private static int RefundForLevel(int level)
        {
            int result = 0;
            int[] costs = { 36, 54, 72 };
            for (int i = 0; i < level && i < costs.Length; i++) result += costs[i];
            return result;
        }

        private SaveState FreshState()
        {
            SaveState fresh = new SaveState { coins = config.freshSaveCoins };
            fresh.warehouse.Add(new ProductCount { product = MetaProduct.FreshLeaf, count = config.freshStartQuantity });
            fresh.stations.Add(new StationState { station = MetaStation.Garden, built = true, level = 0, gardenRemainingSeconds = config.gardenSecondsByLevel[0] });
            fresh.stations.Add(new StationState { station = MetaStation.Dryer, built = false, level = 0 });
            fresh.stations.Add(new StationState { station = MetaStation.Packer, built = false, level = 0 });
            fresh.offers.Add(GenerateOffer(fresh, 0, onboarding: true));
            fresh.offers.Add(GenerateOrdinaryOffer(fresh, 1));
            return fresh;
        }

        private void EnsureOffers(SaveState candidate)
        {
            if (candidate.offers == null) candidate.offers = new List<OrderOffer>();
            StationState packer = FindStation(candidate.stations, MetaStation.Packer);
            bool pilotNeeded = packer != null && packer.built && !candidate.pilotGoalComplete &&
                (candidate.activeOrder == null || !candidate.activeOrder.IsPilotGoal);
            if (pilotNeeded)
            {
                OrderOffer pilot = null;
                for (int i = candidate.offers.Count - 1; i >= 0; i--)
                {
                    if (candidate.offers[i] != null && candidate.offers[i].IsPilotGoal)
                    {
                        pilot = candidate.offers[i];
                        candidate.offers.RemoveAt(i);
                    }
                }
                if (candidate.offers.Count == 0 || candidate.offers[0] == null || candidate.offers[0].IsPilotGoal)
                    candidate.offers.Insert(0, GenerateOffer(candidate, 0, false));
                while (candidate.offers.Count > 1) candidate.offers.RemoveAt(candidate.offers.Count - 1);
                candidate.offers.Add(pilot ?? PilotOffer(candidate));
            }
            else
            {
                for (int i = candidate.offers.Count - 1; i >= 0; i--)
                    if (candidate.offers[i] != null && candidate.offers[i].IsPilotGoal) candidate.offers.RemoveAt(i);
            }
            while (candidate.offers.Count < 2) candidate.offers.Add(GenerateOrdinaryOffer(candidate, candidate.offers.Count));
            while (candidate.offers.Count > 2) candidate.offers.RemoveAt(candidate.offers.Count - 1);
            if (candidate.offers.Count == 0) candidate.offers.Add(GenerateOffer(candidate, 0, onboarding: false));
        }

        private void PutPilotOfferInSlot2(SaveState candidate)
        {
            if (candidate.offers.Count > 1) candidate.offers.RemoveAt(1);
            while (candidate.offers.Count == 0) candidate.offers.Add(GenerateOffer(candidate, 0, false));
            if (candidate.offers.Count == 1) candidate.offers.Add(PilotOffer(candidate));
        }

        private OrderOffer PilotOffer(SaveState candidate)
        {
            OrderOffer offer = new OrderOffer
            {
                id = NewId(candidate), kind = "pilot", reward = 0,
                lines = new List<OrderLine> { new OrderLine { product = MetaProduct.DryTea, count = config.pilotDryTeaQuantity }, new OrderLine { product = MetaProduct.TeaPacket, count = config.pilotTeaPacketQuantity } }
            };
            offer.reward = CalculateReward(offer);
            return offer;
        }

        private OrderOffer GenerateOrdinaryOffer(SaveState candidate, int slot) => GenerateOffer(candidate, slot, false);

        private OrderOffer GenerateOffer(SaveState candidate, int slot, bool onboarding)
        {
            int count = onboarding ? config.onboardingFreshLeafQuantity : 1 + (int)(NextRandom(candidate) % 5);
            OrderOffer offer = new OrderOffer { id = NewId(candidate), kind = onboarding ? "onboarding" : "ordinary", reward = 0 };
            if (slot == 0 || !FindStation(candidate.stations, MetaStation.Dryer).built)
                offer.lines.Add(new OrderLine { product = MetaProduct.FreshLeaf, count = count });
            else
            {
                bool packerBuilt = FindStation(candidate.stations, MetaStation.Packer).built;
                int variants = packerBuilt ? 3 : 2;
                MetaProduct product = (MetaProduct)(NextRandom(candidate) % (uint)variants);
                int units = 1 + (int)(NextRandom(candidate) % 5);
                offer.lines.Add(new OrderLine { product = product, count = units });
                if (NextRandom(candidate) % 3 == 0)
                {
                    MetaProduct second = product == MetaProduct.FreshLeaf ? MetaProduct.DryTea : MetaProduct.FreshLeaf;
                    int secondUnits = 1 + (int)(NextRandom(candidate) % 5);
                    offer.lines.Add(new OrderLine { product = second, count = secondUnits });
                }
            }
            offer.reward = CalculateReward(offer);
            return offer;
        }

        private long CalculateReward(OrderOffer offer)
        {
            long total = 0;
            foreach (OrderLine line in offer.lines)
            {
                long unit = config.rewardPerUnitByProduct[(int)line.product];
                total += (long)unit * line.count;
            }
            return total;
        }

        private static string NewId(SaveState candidate) => "O" + (candidate.nextOfferId++).ToString("D8");
        private static uint NextRandom(SaveState candidate)
        {
            candidate.rngState = candidate.rngState * 1664525u + 1013904223u;
            return candidate.rngState;
        }

        private static MetaProduct InputProduct(MetaStation station) => station == MetaStation.Dryer ? MetaProduct.FreshLeaf : MetaProduct.DryTea;
        private static MetaProduct OutputProduct(MetaStation station) => station == MetaStation.Garden ? MetaProduct.FreshLeaf : station == MetaStation.Dryer ? MetaProduct.DryTea : MetaProduct.TeaPacket;
        private static int FindCount(List<ProductCount> list, MetaProduct product) => Warehouse(list, product);
        private static int Warehouse(List<ProductCount> list, MetaProduct product)
        {
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++) if (list[i].product == product) return list[i].count;
            return 0;
        }
        private static void AddCount(List<ProductCount> list, MetaProduct product, int delta)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].product == product) { list[i].count += delta; return; }
            list.Add(new ProductCount { product = product, count = delta });
        }
        private static StationState FindStation(List<StationState> list, MetaStation station)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++) if (list[i].station == station) return list[i];
            return null;
        }
        private static int Sum(List<ProductCount> list)
        {
            int sum = 0;
            if (list != null) for (int i = 0; i < list.Count; i++) sum += list[i].count;
            return sum;
        }
        private bool CanAddWarehouse(SaveState candidate, MetaProduct product, int quantity) =>
            Warehouse(candidate.warehouse, product) + quantity <= config.warehouseProductCapacity && Sum(candidate.warehouse) + quantity <= config.warehouseTotalCapacity;

        private bool Validate(SaveState candidate)
        {
            if (candidate == null || candidate.schemaVersion != Schema || candidate.coins < 0 || candidate.coins > config.walletCapacity ||
                !Enum.IsDefined(typeof(MetaStation), candidate.selectedStation) || !Enum.IsDefined(typeof(PanelTab), candidate.selectedTab) ||
                candidate.warehouse == null || candidate.stations == null || candidate.stations.Count != 3 || candidate.offers == null || candidate.offers.Count != 2 ||
                candidate.completedOrderIds == null || candidate.completedOrderIds.Count > MaxCompletedIds || candidate.nextOfferId < 1 || candidate.tutorialStep < 0 || candidate.tutorialStep > 5 ||
                candidate.lifetimeDryTeaProduced < 0 || candidate.migrationRefund < 0 || candidate.transferQuantity > config.localCapacity || candidate.transferQuantity < 0 ||
                float.IsNaN(candidate.cameraVerticalOffset) || float.IsInfinity(candidate.cameraVerticalOffset) || candidate.cameraVerticalOffset < -12f || candidate.cameraVerticalOffset > 0f) return false;
            HashSet<MetaProduct> products = new HashSet<MetaProduct>();
            int totalWarehouse = 0;
            foreach (ProductCount item in candidate.warehouse)
            {
                if (item == null || !Enum.IsDefined(typeof(MetaProduct), item.product) || !products.Add(item.product) || item.count < 0 || item.count > config.warehouseProductCapacity) return false;
                totalWarehouse += item.count;
            }
            if (totalWarehouse > config.warehouseTotalCapacity) return false;
            HashSet<MetaStation> stations = new HashSet<MetaStation>();
            foreach (StationState stationState in candidate.stations)
                if (stationState == null || !Enum.IsDefined(typeof(MetaStation), stationState.station) || !stations.Add(stationState.station)) return false;
            foreach (MetaStation id in new[] { MetaStation.Garden, MetaStation.Dryer, MetaStation.Packer })
            {
                StationState s = FindStation(candidate.stations, id);
                if (s == null || s.level < 0 || s.level > 2 || s.input < 0 || s.input > config.localCapacity || s.output < 0 || s.output > config.localCapacity ||
                    s.remainingSeconds < 0 || s.remainingSeconds > MaxProcessingSeconds(id) || s.gardenRemainingSeconds < 0 || s.gardenRemainingSeconds > MaxGardenSeconds() || s.reservedInput < 0 || s.reservedInput > RecipeSize(id) ||
                    (s.remainingSeconds > 0 && s.reservedInput == 0) ||
                    (s.reservedInput > 0 && s.reservedInput != RecipeSize(id)) || (id == MetaStation.Garden && s.reservedInput > 0) ||
                    (s.reservedInput > 0 && s.input > config.localCapacity) ||
                    (s.remainingSeconds == 0 && s.reservedInput > 0 && !s.outputBlocked && s.output < config.localCapacity)) return false;
                if (!s.built && (s.level != 0 || s.input != 0 || s.output != 0 || s.reservedInput != 0 || s.remainingSeconds != 0)) return false;
            }
            if (!FindStation(candidate.stations, MetaStation.Garden).built ||
                (FindStation(candidate.stations, MetaStation.Packer).built && (!FindStation(candidate.stations, MetaStation.Dryer).built || candidate.lifetimeDryTeaProduced < config.dryerUnlockPackerAtProduced)) ||
                (candidate.pilotGoalComplete && !FindStation(candidate.stations, MetaStation.Packer).built)) return false;
            if (candidate.offers.Count != 2 || candidate.offers[0] == null || candidate.offers[1] == null) return false;
            if (candidate.offers[0].IsPilotGoal || candidate.offers[1].IsPilotGoal && !FindStation(candidate.stations, MetaStation.Packer).built) return false;
            if (candidate.offers[0].id == candidate.offers[1].id) return false;
            foreach (OrderOffer offer in candidate.offers) if (!ValidateOffer(candidate, offer)) return false;
            bool pilotRequired = FindStation(candidate.stations, MetaStation.Packer).built && !candidate.pilotGoalComplete &&
                                 (candidate.activeOrder == null || !candidate.activeOrder.IsPilotGoal);
            if (candidate.offers[1].IsPilotGoal != pilotRequired) return false;
            if (candidate.pilotGoalComplete && (candidate.offers[0].IsPilotGoal || candidate.offers[1].IsPilotGoal ||
                (candidate.activeOrder != null && candidate.activeOrder.IsPilotGoal))) return false;
            if (candidate.offers[0].kind != "onboarding" &&
                (candidate.offers[0].lines.Count != 1 || candidate.offers[0].lines[0].product != MetaProduct.FreshLeaf)) return false;
            if (candidate.offers[0].kind == "onboarding" && (candidate.offers[0].lines.Count != 1 ||
                candidate.offers[0].lines[0].product != MetaProduct.FreshLeaf || candidate.offers[0].lines[0].count != config.onboardingFreshLeafQuantity)) return false;
            if (candidate.activeOrder != null && (candidate.activeOrder.id == candidate.offers[0].id || candidate.activeOrder.id == candidate.offers[1].id)) return false;
            HashSet<string> completedIds = new HashSet<string>();
            int largestOfferId = 0;
            if (!ReadOfferId(candidate.offers[0].id, out int firstOfferId) || !ReadOfferId(candidate.offers[1].id, out int secondOfferId)) return false;
            largestOfferId = Mathf.Max(firstOfferId, secondOfferId);
            if (candidate.activeOrder != null)
            {
                if (!completedIds.Add(candidate.activeOrder.id) || !ReadOfferId(candidate.activeOrder.id, out int activeId)) return false;
                largestOfferId = Mathf.Max(largestOfferId, activeId);
            }
            foreach (string id in candidate.completedOrderIds)
                if (string.IsNullOrEmpty(id) || id == candidate.offers[0].id || id == candidate.offers[1].id || !completedIds.Add(id) || !ReadOfferId(id, out int completedId)) return false;
                else largestOfferId = Mathf.Max(largestOfferId, completedId);
            if (candidate.nextOfferId <= largestOfferId) return false;
            return candidate.activeOrder == null || ValidateOffer(candidate, candidate.activeOrder);
        }

        private static bool ReadOfferId(string id, out int value)
        {
            value = 0;
            return !string.IsNullOrEmpty(id) && id.Length == 9 && id[0] == 'O' && int.TryParse(id.Substring(1), out value) && value > 0;
        }

        private bool ValidateOffer(SaveState candidate, OrderOffer offer)
        {
            if (offer == null || string.IsNullOrEmpty(offer.id) || offer.reward < 0 || offer.lines == null || offer.lines.Count < 1 || offer.lines.Count > 2) return false;
            if (offer.kind != "onboarding" && offer.kind != "ordinary" && offer.kind != "pilot") return false;
            int total = 0;
            HashSet<MetaProduct> products = new HashSet<MetaProduct>();
            foreach (OrderLine line in offer.lines)
            {
                if (line == null || !Enum.IsDefined(typeof(MetaProduct), line.product) || !products.Add(line.product) || line.count <= 0 || line.count > config.warehouseProductCapacity) return false;
                if (offer.kind == "ordinary" && line.count > 5) return false;
                if (line.product == MetaProduct.DryTea && !FindStation(candidate.stations, MetaStation.Dryer).built) return false;
                if (line.product == MetaProduct.TeaPacket && !FindStation(candidate.stations, MetaStation.Packer).built) return false;
                total += line.count;
            }
            bool exactPilot = offer.lines.Count == 2 &&
                GetOfferProductCount(offer, MetaProduct.DryTea) == config.pilotDryTeaQuantity && GetOfferProductCount(offer, MetaProduct.TeaPacket) == config.pilotTeaPacketQuantity;
            return total <= config.warehouseTotalCapacity && (offer.IsPilotGoal ? offer.reward == CalculateReward(offer) && exactPilot : offer.reward == CalculateReward(offer));
        }

        private static int GetOfferProductCount(OrderOffer offer, MetaProduct product)
        {
            for (int i = 0; i < offer.lines.Count; i++) if (offer.lines[i].product == product) return offer.lines[i].count;
            return 0;
        }

        private SaveState ReadAndValidate(string path, bool allowNewer)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Save does not exist", path);
            string json = File.ReadAllText(path);
            if (!HasRootField(json, "schemaVersion")) throw new InvalidDataException("Save schema field missing.");
            SaveState parsed = DeserializeState(json);
            if (parsed == null || parsed.schemaVersion < 1) throw new InvalidDataException("Save schema invalid.");
            if (parsed.schemaVersion > Schema)
            {
                if (allowNewer) throw new NewerSchemaException(path);
                throw new InvalidDataException("Unsupported future schema in transaction temp file.");
            }
            string[] required = { "schemaVersion", "coins", "muted", "tutorialComplete", "tutorialStep", "lifetimeDryTeaProduced", "pilotGoalComplete", "warehouse", "stations", "offers", "activeOrder", "completedOrderIds", "nextOfferId", "rngState", "migrationReceipt", "migrationLegacyCoins", "migrationRefund", "lastCheckpointUtcTicks", "selectedStation", "selectedTab", "transferQuantity", "cameraVerticalOffset" };
            for (int i = 0; i < required.Length; i++) if (!HasRootField(json, required[i])) throw new InvalidDataException("Meta save field missing: " + required[i]);
            if (parsed.schemaVersion != Schema || !Validate(parsed)) throw new InvalidDataException("Meta save failed validation.");
            return parsed;
        }

        private void PreserveUnsupported(string path)
        {
            if (File.Exists(path))
            {
                try { File.Copy(path, UniqueArchivePath(path, ".schema-unknown-", ".json"), false); }
                catch (Exception error) { Debug.LogWarning("Could not preserve copy of newer save; source remains untouched: " + error.Message, this); }
            }
            if (!legacyMigrationDisabled && WriteMigrationGuard()) legacyMigrationDisabled = true;
            if (!legacyMigrationDisabled)
            {
                state = FreshState();
                BlockPersistence("Не удалось защитить слот от миграции; сохранение оставлено без изменений");
                return;
            }
            string recoveryPath = Path.Combine(Path.GetDirectoryName(savePath), RecoveryFile);
            if (legacyMigrationDisabled)
            {
                savePath = recoveryPath;
                state = FreshState();
                persistenceBlocked = false;
                if (!WriteState(state)) BlockPersistence("Не удалось создать отдельный слот для новой кампании");
            }
            if (!persistenceBlocked)
            {
                statusText = "Сохранение более новой версии сохранено отдельно; открыта новая кампания в отдельном слоте";
                Notice?.Invoke(statusText);
            }
        }

        private int MaxGardenSeconds() => Mathf.Max(config.gardenSecondsByLevel[0], Mathf.Max(config.gardenSecondsByLevel[1], config.gardenSecondsByLevel[2]));
        private int MaxProcessingSeconds(MetaStation station)
        {
            int[] values = station == MetaStation.Dryer ? config.dryerSecondsByLevel : config.packerSecondsByLevel;
            return Mathf.Max(values[0], Mathf.Max(values[1], values[2]));
        }

        private bool RestorePrimaryFromBackup()
        {
            try
            {
                SaveState verified = ReadAndValidate(savePath + ".bak", allowNewer: false);
                if (!Validate(verified)) return false;
                if (File.Exists(savePath)) ArchiveIfPresent(savePath, ".corrupt-");
                File.Copy(savePath + ".bak", savePath, false);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("Could not restore verified backup to primary: " + error.Message, this);
                return false;
            }
        }

        private bool WriteMigrationGuard()
        {
            string path = Path.Combine(Path.GetDirectoryName(savePath), MigrationGuardFile);
            if (File.Exists(path)) return true;
            string temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, "schema2 migration handled");
                File.Move(temp, path);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("Could not persist migration guard: " + error.Message, this);
                return false;
            }
        }

        private static bool HasRootField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return false;
            int cursor = 0;
            SkipJsonWhitespace(json, ref cursor);
            if (cursor >= json.Length || json[cursor++] != '{') return false;
            while (cursor < json.Length)
            {
                SkipJsonWhitespace(json, ref cursor);
                if (cursor >= json.Length || json[cursor] == '}') return false;
                if (json[cursor] == ',') { cursor++; continue; }
                if (json[cursor++] != '"') return false;
                int start = cursor;
                while (cursor < json.Length && json[cursor] != '"')
                {
                    if (json[cursor] == '\\' && cursor + 1 < json.Length) cursor++;
                    cursor++;
                }
                if (cursor >= json.Length) return false;
                string key = json.Substring(start, cursor - start);
                cursor++;
                SkipJsonWhitespace(json, ref cursor);
                if (cursor >= json.Length || json[cursor++] != ':') return false;
                if (string.Equals(key, field, StringComparison.Ordinal)) return true;
                SkipJsonValue(json, ref cursor);
            }
            return false;
        }

        private static SaveState DeserializeState(string json)
        {
            SaveState parsed = JsonUtility.FromJson<SaveState>(json);
            if (parsed?.activeOrder != null)
            {
                OrderOffer empty = parsed.activeOrder;
                bool emptySentinel = string.IsNullOrEmpty(empty.id) && string.IsNullOrEmpty(empty.kind) && empty.reward == 0 &&
                    (empty.lines == null || empty.lines.Count == 0);
                if (emptySentinel) parsed.activeOrder = null;
            }
            return parsed;
        }

        private static void SkipJsonWhitespace(string json, ref int cursor)
        {
            while (cursor < json.Length && char.IsWhiteSpace(json[cursor])) cursor++;
        }

        private static void SkipJsonValue(string json, ref int cursor)
        {
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            while (cursor < json.Length)
            {
                char value = json[cursor];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (value == '\\') escaped = true;
                    else if (value == '"') inString = false;
                }
                else if (value == '"') inString = true;
                else if (value == '{' || value == '[') depth++;
                else if (value == '}' || value == ']')
                {
                    if (depth == 0) return;
                    depth--;
                }
                else if (value == ',' && depth == 0) return;
                cursor++;
            }
        }

        private void ArchiveIfPresent(string path, string marker)
        {
            if (!File.Exists(path)) return;
            try { File.Move(path, path + marker + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json"); }
            catch (Exception error) { Debug.LogWarning("Could not archive save " + path + ": " + error.Message, this); }
        }

        private static string UniqueArchivePath(string sourcePath, string marker, string extension)
        {
            string basePath = sourcePath + marker + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            string candidate = basePath + extension;
            int suffix = 1;
            while (File.Exists(candidate)) candidate = basePath + "-" + suffix++ + extension;
            return candidate;
        }

        private static SaveState Clone(SaveState source) => DeserializeState(JsonUtility.ToJson(source));
        private static OrderOffer CloneOffer(OrderOffer source) => JsonUtility.FromJson<OrderOffer>(JsonUtility.ToJson(source));
        private static long SaturatingAdd(long left, long right, long cap) => right > cap - left ? cap : left + right;

        private void OnApplicationPause(bool value) { paused = value; UpdateForeground(); if (value) SaveCheckpoint(); }
        private void OnApplicationFocus(bool value) { focused = value; UpdateForeground(); if (!value) SaveCheckpoint(); }
        private void OnDestroy() => SaveCheckpoint();
        private void UpdateForeground()
        {
            bool nextForeground = !paused && focused;
            if (foreground == nextForeground) return;
            foreground = nextForeground;
            tickAccumulator = 0f;
            skipNextForegroundFrame = foreground;
        }
        private void SaveCheckpoint()
        {
            if (state == null || persistenceBlocked || migrationPending) return;
            SaveState candidate = Clone(state);
            candidate.lastCheckpointUtcTicks = DateTime.UtcNow.Ticks;
            if (!WriteState(candidate)) BlockPersistence("Не удалось сохранить контрольную точку");
            else state = candidate;
        }

        private sealed class NewerSchemaException : Exception
        {
            public string Path { get; }
            public NewerSchemaException(string path) { Path = path; }
        }
    }
}
