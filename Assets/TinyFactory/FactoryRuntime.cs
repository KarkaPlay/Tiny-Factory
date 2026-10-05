using System;
using System.IO;
using UnityEngine;

namespace TinyFactory
{
    public enum UpgradeBranch { Speed, Productivity, Automation }

    [DefaultExecutionOrder(-100)]
    public sealed class FactoryRuntime : MonoBehaviour
    {
        private const int SaveSchemaVersion = 1;
        private const string SaveFileName = "tiny-factory-save.json";
        private const string FreshFileName = "tiny-factory-save-fresh-v1.json";
        private const long ValueCap = 1000000000000L;

        [Serializable]
        private sealed class SaveData
        {
            public int schemaVersion = SaveSchemaVersion;
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

        [SerializeField] private FactoryBalanceConfig config;
        [SerializeField] private long coins;
        [SerializeField] private long sold;
        [SerializeField] private int speedLevel;
        [SerializeField] private int productivityLevel;
        [SerializeField] private int automationLevel;
        [SerializeField] private int sourceBuffer;
        [SerializeField] private int rollerInput;
        [SerializeField] private int packagerInput;
        [SerializeField] private int dryerRemaining;
        [SerializeField] private int rollerRemaining;
        [SerializeField] private int packagerRemaining;
        [SerializeField] private bool dryerHasWip;
        [SerializeField] private bool rollerHasWip;
        [SerializeField] private bool packagerHasWip;
        [SerializeField] private bool purchaseQueued;
        [SerializeField] private UpgradeBranch queuedBranch;
        [SerializeField] private int secondsUntilAuto = 12;
        [SerializeField] private bool harvestOnboardingComplete;
        [SerializeField] private bool firstSaleOnboardingComplete;
        [SerializeField] private bool firstPurchaseOnboardingComplete;
        [SerializeField] private bool muted;
        private float tickAccumulator;
        private bool foreground = true;
        private bool paused;
        private bool focused = true;
        private int ticks;
        private int rejectedHarvests;
        private string savePath;

        public event Action Changed;
        public event Action<bool> ItemAccepted;
        public event Action<long> SaleCommitted;
        public event Action<string> SaveNotice;
        public long Coins => coins;
        public long Sold => sold;
        public int SpeedLevel => speedLevel;
        public int ProductivityLevel => productivityLevel;
        public int AutomationLevel => automationLevel;
        public int SourceBuffer => sourceBuffer;
        public int RollerInput => rollerInput;
        public int PackagerInput => packagerInput;
        public int DryerRemaining => dryerRemaining;
        public int RollerRemaining => rollerRemaining;
        public int PackagerRemaining => packagerRemaining;
        public bool DryerHasWip => dryerHasWip;
        public bool RollerHasWip => rollerHasWip;
        public bool PackagerHasWip => packagerHasWip;
        public int SecondsUntilAuto => secondsUntilAuto;
        public int Ticks => ticks;
        public int RejectedHarvests => rejectedHarvests;
        public int Capacity => config != null ? config.bufferCapacity : 8;
        public bool IsForeground => foreground;
        public FactoryBalanceConfig Config => config;
        public bool RollerUnlocked => sold >= config.rollerUnlockSales;
        public bool SealerUnlocked => sold >= config.sealerUnlockSales;
        public bool HarvestOnboardingComplete => harvestOnboardingComplete;
        public bool FirstSaleOnboardingComplete => firstSaleOnboardingComplete;
        public bool FirstPurchaseOnboardingComplete => firstPurchaseOnboardingComplete;
        public bool Muted => muted;
        public string SaveStatus { get; private set; } = string.Empty;
        public int TotalInFlight => sourceBuffer + rollerInput + packagerInput +
                                    (dryerHasWip ? 1 : 0) + (rollerHasWip ? 1 : 0) + (packagerHasWip ? 1 : 0);

        private void Awake()
        {
            if (config == null || !config.IsValid())
            {
                Debug.LogError("FactoryRuntime requires a valid serialized FactoryBalanceConfig.", this);
                enabled = false;
                return;
            }
            savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            LoadProgress();
            secondsUntilAuto = config.AutomationInterval(automationLevel);
        }

        private void Update()
        {
            if (!foreground || config == null) return;
            tickAccumulator += Time.unscaledDeltaTime;
            if (tickAccumulator < 1f) return;
            tickAccumulator -= 1f;
            if (tickAccumulator >= 1f) tickAccumulator %= 1f;
            Tick();
        }

        public bool RequestHarvest()
        {
            if (config == null || !foreground) return false;
            int batch = config.ManualBatch(productivityLevel);
            if (sourceBuffer + batch > Capacity)
            {
                rejectedHarvests++;
                Changed?.Invoke();
                return false;
            }
            sourceBuffer += batch;
            bool onboardingChanged = !harvestOnboardingComplete;
            harvestOnboardingComplete = true;
            ItemAccepted?.Invoke(false);
            if (onboardingChanged) SaveProgress();
            Changed?.Invoke();
            return true;
        }

        public bool RequestUpgrade(UpgradeBranch branch)
        {
            if (config == null || !foreground || purchaseQueued || Level(branch) >= 3) return false;
            purchaseQueued = true;
            queuedBranch = branch;
            return true;
        }

        // Kept as a compatibility entry point for the authored M03 UnityEvent.
        public bool RequestSpeedUpgrade() => RequestUpgrade(UpgradeBranch.Speed);

        public int Level(UpgradeBranch branch) => branch switch
        {
            UpgradeBranch.Speed => speedLevel,
            UpgradeBranch.Productivity => productivityLevel,
            _ => automationLevel
        };

        public bool CanAffordNext(UpgradeBranch branch) =>
            config != null && Level(branch) < 3 && config.UpgradeCost(Level(branch)) > 0 &&
            coins >= config.UpgradeCost(Level(branch));

        public void SetMuted(bool value)
        {
            if (muted == value) return;
            muted = value;
            SaveProgress();
            Changed?.Invoke();
        }

        public void SetForeground(bool active) { foreground = active; }

        public void Tick() => Tick(false);

        public void Tick(bool manualHarvestAtBoundary)
        {
            if (config == null || !foreground) return;
            ticks++;
            if (dryerRemaining > 0) dryerRemaining--;
            if (rollerRemaining > 0) rollerRemaining--;
            if (packagerRemaining > 0) packagerRemaining--;

            if (packagerHasWip && packagerRemaining == 0)
            {
                packagerHasWip = false;
                sold = SaturatingAdd(sold, 1L, config.valueCap);
                long price = sold <= config.rollerUnlockSales ? config.saleValue :
                    sold <= config.sealerUnlockSales ? config.rollerTierSaleValue : config.sealerTierSaleValue;
                coins = SaturatingAdd(coins, price, config.valueCap);
                if (sold == 1) firstSaleOnboardingComplete = true;
                SaleCommitted?.Invoke(price);
                SaveProgress();
            }
            if (rollerHasWip && rollerRemaining == 0 && packagerInput < Capacity)
            {
                rollerHasWip = false;
                packagerInput++;
            }
            if (dryerHasWip && dryerRemaining == 0)
            {
                if (RollerUnlocked)
                {
                    if (!rollerHasWip && rollerInput == 0)
                    {
                        rollerHasWip = true;
                        rollerRemaining = config.RollerDuration(speedLevel);
                        dryerHasWip = false;
                    }
                    else if (rollerInput < Capacity)
                    {
                        rollerInput++;
                        dryerHasWip = false;
                    }
                }
                else if (packagerInput < Capacity)
                {
                    packagerInput++;
                    dryerHasWip = false;
                }
            }
            if (!packagerHasWip && packagerInput > 0)
            {
                packagerInput--;
                packagerHasWip = true;
                packagerRemaining = config.FinalDuration(speedLevel);
            }
            if (!rollerHasWip && rollerInput > 0)
            {
                rollerInput--;
                rollerHasWip = true;
                rollerRemaining = config.RollerDuration(speedLevel);
            }
            if (!dryerHasWip && sourceBuffer > 0)
            {
                sourceBuffer--;
                dryerHasWip = true;
                dryerRemaining = config.DryerDuration(speedLevel);
            }

            if (manualHarvestAtBoundary) RequestHarvest();

            secondsUntilAuto--;
            if (secondsUntilAuto <= 0)
            {
                int batch = config.ManualBatch(productivityLevel);
                if (sourceBuffer + batch <= Capacity)
                {
                    sourceBuffer += batch;
                    ItemAccepted?.Invoke(true);
                }
                else rejectedHarvests++;
                secondsUntilAuto = config.AutomationInterval(automationLevel);
            }

            if (purchaseQueued)
            {
                purchaseQueued = false;
                int level = Level(queuedBranch);
                int cost = config.UpgradeCost(level);
                if (level < 3 && cost > 0 && coins >= cost)
                {
                    coins -= cost;
                    SetLevel(queuedBranch, level + 1);
                    firstPurchaseOnboardingComplete = true;
                    if (queuedBranch == UpgradeBranch.Automation)
                        secondsUntilAuto = config.AutomationInterval(automationLevel);
                    SaveProgress();
                }
            }
            Changed?.Invoke();
        }

        private int LevelValue(UpgradeBranch branch) => Level(branch);
        private void SetLevel(UpgradeBranch branch, int value)
        {
            if (branch == UpgradeBranch.Speed) speedLevel = value;
            else if (branch == UpgradeBranch.Productivity) productivityLevel = value;
            else automationLevel = value;
        }

        private void OnApplicationPause(bool value) { paused = value; UpdateForeground(); if (value) SaveProgress(); }
        private void OnApplicationFocus(bool value) { focused = value; UpdateForeground(); if (!value) SaveProgress(); }
        private void OnDestroy() => SaveProgress();
        private void UpdateForeground() => SetForeground(!paused && focused);

        private void LoadProgress()
        {
            string primary = savePath;
            string backup = savePath + ".bak";
            if (!File.Exists(primary) && !File.Exists(backup)) return;
            if (File.Exists(primary))
            {
                try
                {
                    SaveData data = ReadAndValidate(primary);
                    Apply(data);
                    return;
                }
                catch (NewerSchemaException)
                {
                    string archive = UniqueArchivePath(primary, ".schema-unknown-", ".json");
                    try { File.Copy(primary, archive, false); }
                    catch (Exception e) { Debug.LogError("Could not archive newer save; original remains untouched: " + e.Message, this); }
                    savePath = Path.Combine(Path.GetDirectoryName(primary), FreshFileName);
                    bool restoredFresh = LoadFreshSaveSlot();
                    SaveStatus = restoredFresh
                        ? "Сохранение новой версии сохранено отдельно; восстановлен свежий слот"
                        : "Сохранение новой версии сохранено отдельно; начата новая игра";
                    SaveNotice?.Invoke(SaveStatus);
                    return;
                }
                catch (Exception e) { Debug.LogWarning("Primary save is invalid; trying backup: " + e.Message, this); }
            }
            SaveData backupData = null;
            try { backupData = ReadAndValidate(backup); }
            catch (NewerSchemaException)
            {
                string archive = UniqueArchivePath(backup, ".schema-unknown-", ".json");
                try { File.Copy(backup, archive, false); }
                catch (Exception e) { Debug.LogError("Could not archive newer backup; original remains untouched: " + e.Message, this); }
                savePath = Path.Combine(Path.GetDirectoryName(primary), FreshFileName);
                bool restoredFresh = LoadFreshSaveSlot();
                SaveStatus = restoredFresh
                    ? "Резервная копия новой версии сохранена отдельно; восстановлен свежий слот"
                    : "Резервная копия новой версии сохранена отдельно; начата новая игра";
                SaveNotice?.Invoke(SaveStatus);
                return;
            }
            catch (Exception e) { Debug.LogWarning("Save backup is invalid; starting fresh: " + e.Message, this); }
            if (backupData != null)
            {
                bool primaryArchived = !File.Exists(primary);
                if (File.Exists(primary))
                {
                    try { File.Move(primary, UniqueArchivePath(primary, ".corrupt-", string.Empty)); primaryArchived = true; }
                    catch (Exception e) { Debug.LogError("Could not archive invalid primary; backup will be used without overwriting it: " + e.Message, this); }
                }
                Apply(backupData);
                SaveStatus = "Прогресс восстановлен из резервной копии";
                SaveNotice?.Invoke(SaveStatus);
                if (primaryArchived) SaveProgress();
                return;
            }
            try
            {
                if (File.Exists(primary)) File.Move(primary, UniqueArchivePath(primary, ".corrupt-", string.Empty));
                if (File.Exists(backup)) File.Move(backup, UniqueArchivePath(backup, ".corrupt-", string.Empty));
            }
            catch (Exception e) { Debug.LogWarning("Could not archive invalid save files: " + e.Message, this); }
            SaveStatus = "Сохранение повреждено; начата новая игра";
            SaveNotice?.Invoke(SaveStatus);
        }

        private bool LoadFreshSaveSlot()
        {
            string primary = savePath;
            string backup = savePath + ".bak";
            if (File.Exists(primary))
            {
                try { Apply(ReadAndValidate(primary)); return true; }
                catch (NewerSchemaException)
                {
                    try { File.Copy(primary, UniqueArchivePath(primary, ".schema-unknown-", ".json"), false); }
                    catch (Exception e) { Debug.LogError("Could not archive unsupported fresh save: " + e.Message, this); }
                    savePath = Path.Combine(Path.GetDirectoryName(primary), FreshFileName + ".alternate.json");
                    return false;
                }
                catch (Exception e) { Debug.LogWarning("Fresh save primary could not be loaded: " + e.Message, this); }
            }
            SaveData savedBackup = null;
            if (File.Exists(backup))
            {
                try { savedBackup = ReadAndValidate(backup); }
                catch (NewerSchemaException)
                {
                    try { File.Copy(backup, UniqueArchivePath(backup, ".schema-unknown-", ".json"), false); }
                    catch (Exception e) { Debug.LogError("Could not archive unsupported fresh backup: " + e.Message, this); }
                    savePath = Path.Combine(Path.GetDirectoryName(primary), FreshFileName + ".alternate.json");
                    return false;
                }
                catch (Exception e) { Debug.LogWarning("Fresh save backup could not be loaded: " + e.Message, this); }
            }
            if (savedBackup != null)
            {
                bool primaryArchived = !File.Exists(primary);
                if (File.Exists(primary))
                {
                    try { File.Move(primary, UniqueArchivePath(primary, ".corrupt-", string.Empty)); primaryArchived = true; }
                    catch (Exception e) { Debug.LogError("Could not archive invalid fresh primary; backup will be used without overwriting it: " + e.Message, this); }
                }
                Apply(savedBackup);
                if (primaryArchived) SaveProgress();
                return true;
            }
            try
            {
                if (File.Exists(primary)) File.Move(primary, UniqueArchivePath(primary, ".corrupt-", string.Empty));
                if (File.Exists(backup)) File.Move(backup, UniqueArchivePath(backup, ".corrupt-", string.Empty));
            }
            catch (Exception e) { Debug.LogWarning("Could not archive invalid fresh slot: " + e.Message, this); }
            return false;
        }

        private SaveData ReadAndValidate(string path)
        {
            string json = File.ReadAllText(path);
            if (!HasJsonField(json, "schemaVersion"))
                throw new InvalidDataException("Save schema field is missing.");
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || data.schemaVersion <= 0) throw new InvalidDataException("Missing or invalid save schema.");
            if (data.schemaVersion > SaveSchemaVersion)
                throw new NewerSchemaException();
            if (!HasRequiredSaveFields(json))
                throw new InvalidDataException("Save is missing one or more required permanent fields.");
            if (data.schemaVersion != SaveSchemaVersion || data.coins < 0 || data.coins > ValueCap ||
                data.lifetimeSold < 0 || data.lifetimeSold > ValueCap ||
                data.speedLevel < 0 || data.speedLevel > 3 || data.productivityLevel < 0 || data.productivityLevel > 3 ||
                data.automationLevel < 0 || data.automationLevel > 3 ||
                data.rollerUnlocked != (data.lifetimeSold >= config.rollerUnlockSales) || data.sealerUnlocked != (data.lifetimeSold >= config.sealerUnlockSales))
                throw new InvalidDataException("Save values failed validation.");
            return data;
        }

        private static bool HasRequiredSaveFields(string json)
        {
            string[] fields =
            {
                "schemaVersion", "coins", "lifetimeSold", "speedLevel", "productivityLevel", "automationLevel",
                "rollerUnlocked", "sealerUnlocked", "harvestOnboardingComplete", "firstSaleOnboardingComplete",
                "firstPurchaseOnboardingComplete", "muted"
            };
            for (int i = 0; i < fields.Length; i++)
                if (!HasJsonField(json, fields[i])) return false;
            return true;
        }

        private static bool HasJsonField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return false;
            int cursor = 0;
            SkipWhitespace(json, ref cursor);
            if (cursor >= json.Length || json[cursor++] != '{') return false;
            while (cursor < json.Length)
            {
                SkipWhitespace(json, ref cursor);
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
                SkipWhitespace(json, ref cursor);
                if (cursor >= json.Length || json[cursor++] != ':') return false;
                if (string.Equals(key, field, StringComparison.Ordinal)) return true;
                SkipJsonValue(json, ref cursor);
            }
            return false;
        }

        private static void SkipWhitespace(string json, ref int cursor)
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

        private static string UniqueArchivePath(string path, string marker, string extension) =>
            path + marker + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N") + extension;

        private void Apply(SaveData data)
        {
            coins = data.coins;
            sold = data.lifetimeSold;
            speedLevel = data.speedLevel;
            productivityLevel = data.productivityLevel;
            automationLevel = data.automationLevel;
            harvestOnboardingComplete = data.harvestOnboardingComplete;
            firstSaleOnboardingComplete = data.firstSaleOnboardingComplete;
            firstPurchaseOnboardingComplete = data.firstPurchaseOnboardingComplete;
            muted = data.muted;
            sourceBuffer = rollerInput = packagerInput = 0;
            dryerHasWip = rollerHasWip = packagerHasWip = false;
            dryerRemaining = rollerRemaining = packagerRemaining = 0;
        }

        private void SaveProgress()
        {
            if (string.IsNullOrEmpty(savePath)) return;
            SaveData data = new SaveData
            {
                coins = Math.Max(0, Math.Min(coins, ValueCap)),
                lifetimeSold = sold,
                speedLevel = speedLevel,
                productivityLevel = productivityLevel,
                automationLevel = automationLevel,
                rollerUnlocked = RollerUnlocked,
                sealerUnlocked = SealerUnlocked,
                harvestOnboardingComplete = harvestOnboardingComplete,
                firstSaleOnboardingComplete = firstSaleOnboardingComplete,
                firstPurchaseOnboardingComplete = firstPurchaseOnboardingComplete,
                muted = muted
            };
            string temp = savePath + ".tmp";
            string backup = savePath + ".bak";
            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                SaveData verified = ReadAndValidate(temp);
                if (!Equivalent(verified, data))
                    throw new InvalidDataException("Temporary save readback differs from source.");
                if (File.Exists(savePath))
                {
                    SaveData old = ReadAndValidate(savePath);
                    File.Copy(savePath, backup, true);
                    SaveData backupCheck = ReadAndValidate(backup);
                    if (!Equivalent(backupCheck, old))
                        throw new InvalidDataException("Backup verification failed.");
                    File.Replace(temp, savePath, null);
                }
                else File.Move(temp, savePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Local save failed; existing save was preserved where possible: " + e.Message, this);
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private static bool Equivalent(SaveData a, SaveData b) =>
            a.schemaVersion == b.schemaVersion && a.coins == b.coins && a.lifetimeSold == b.lifetimeSold &&
            a.speedLevel == b.speedLevel && a.productivityLevel == b.productivityLevel &&
            a.automationLevel == b.automationLevel && a.rollerUnlocked == b.rollerUnlocked &&
            a.sealerUnlocked == b.sealerUnlocked &&
            a.harvestOnboardingComplete == b.harvestOnboardingComplete &&
            a.firstSaleOnboardingComplete == b.firstSaleOnboardingComplete &&
            a.firstPurchaseOnboardingComplete == b.firstPurchaseOnboardingComplete && a.muted == b.muted;

        private static long SaturatingAdd(long current, long amount, long cap)
        {
            if (amount <= 0 || current >= cap) return Math.Min(current, cap);
            return amount >= cap - current ? cap : current + amount;
        }

        private sealed class NewerSchemaException : Exception { }
    }
}
