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
            if (config == null)
            {
                Debug.LogError("FactoryRuntime requires its serialized FactoryBalanceConfig reference.", this);
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
            harvestOnboardingComplete = true;
            ItemAccepted?.Invoke(false);
            SaveProgress();
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
            Level(branch) < 3 && coins >= config.UpgradeCost(Level(branch));

        public void SetMuted(bool value)
        {
            if (muted == value) return;
            muted = value;
            SaveProgress();
            Changed?.Invoke();
        }

        public void SetForeground(bool active) { foreground = active; }

        public void Tick()
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
                if (level < 3 && coins >= cost)
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
            if (!File.Exists(primary)) { SaveStatus = string.Empty; return; }
            try
            {
                SaveData data = ReadAndValidate(primary, allowFuture: true);
                if (data == null) return;
                Apply(data);
                return;
            }
            catch (NewerSchemaException)
            {
                string archive = primary + ".schema-unknown-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json";
                try { File.Copy(primary, archive, false); }
                catch (Exception e) { Debug.LogError("Could not archive newer save; original remains untouched: " + e.Message, this); }
                savePath = Path.Combine(Application.persistentDataPath, FreshFileName);
                if (File.Exists(savePath))
                {
                    try { Apply(ReadAndValidate(savePath, allowFuture: false)); }
                    catch (Exception e) { Debug.LogWarning("Fresh save slot is invalid; starting fresh: " + e.Message, this); }
                }
                SaveStatus = "Сохранение новой версии сохранено отдельно; начата новая игра";
                SaveNotice?.Invoke(SaveStatus);
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Primary save is invalid; trying backup: " + e.Message, this);
            }
            try
            {
                SaveData backupData = ReadAndValidate(backup, allowFuture: false);
                Apply(backupData);
                string corruptArchive = primary + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.Move(primary, corruptArchive);
                SaveStatus = "Прогресс восстановлен из резервной копии";
                SaveNotice?.Invoke(SaveStatus);
                SaveProgress();
                return;
            }
            catch (Exception e) { Debug.LogWarning("Save backup is invalid; starting fresh: " + e.Message, this); }
            try { if (File.Exists(primary)) File.Move(primary, primary + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")); } catch { }
            try { if (File.Exists(backup)) File.Move(backup, backup + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")); } catch { }
            SaveStatus = "Сохранение повреждено; начата новая игра";
            SaveNotice?.Invoke(SaveStatus);
        }

        private SaveData ReadAndValidate(string path, bool allowFuture)
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null || data.schemaVersion <= 0) throw new InvalidDataException("Missing or invalid save schema.");
            if (data.schemaVersion > SaveSchemaVersion)
            {
                if (allowFuture) throw new NewerSchemaException();
                throw new InvalidDataException("Backup uses a newer unsupported schema.");
            }
            if (data.schemaVersion != SaveSchemaVersion || data.coins < 0 || data.coins > ValueCap ||
                data.lifetimeSold < 0 || data.lifetimeSold > ValueCap ||
                data.speedLevel < 0 || data.speedLevel > 3 || data.productivityLevel < 0 || data.productivityLevel > 3 ||
                data.automationLevel < 0 || data.automationLevel > 3 ||
                data.rollerUnlocked != (data.lifetimeSold >= config.rollerUnlockSales) || data.sealerUnlocked != (data.lifetimeSold >= config.sealerUnlockSales))
                throw new InvalidDataException("Save values failed validation.");
            return data;
        }

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
                SaveData verified = ReadAndValidate(temp, allowFuture: false);
                if (verified.coins != data.coins || verified.lifetimeSold != data.lifetimeSold)
                    throw new InvalidDataException("Temporary save readback differs from source.");
                if (File.Exists(savePath))
                {
                    SaveData old = ReadAndValidate(savePath, allowFuture: false);
                    File.Copy(savePath, backup, true);
                    SaveData backupCheck = ReadAndValidate(backup, allowFuture: false);
                    if (backupCheck.coins != old.coins || backupCheck.lifetimeSold != old.lifetimeSold)
                        throw new InvalidDataException("Backup verification failed.");
                    File.Replace(temp, savePath, null);
                }
                else File.Move(temp, savePath);
                SaveStatus = string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError("Local save failed; existing save was preserved where possible: " + e.Message, this);
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private static long SaturatingAdd(long current, long amount, long cap)
        {
            if (amount <= 0 || current >= cap) return Math.Min(current, cap);
            return amount >= cap - current ? cap : current + amount;
        }

        private sealed class NewerSchemaException : Exception { }
    }
}
