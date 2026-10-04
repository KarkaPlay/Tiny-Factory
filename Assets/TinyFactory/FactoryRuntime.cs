using System;
using UnityEngine;

namespace TinyFactory
{
    [DefaultExecutionOrder(-100)]
    public sealed class FactoryRuntime : MonoBehaviour
    {
        [SerializeField] private FactoryBalanceConfig config;
        [SerializeField] private long coins;
        [SerializeField] private long sold;
        [SerializeField] private int speedLevel;
        [SerializeField] private int sourceBuffer;
        [SerializeField] private int packagerInput;
        [SerializeField] private int dryerRemaining;
        [SerializeField] private int packagerRemaining;
        [SerializeField] private bool dryerHasWip;
        [SerializeField] private bool packagerHasWip;
        [SerializeField] private bool purchaseQueued;
        [SerializeField] private int secondsUntilAuto = 12;
        private float tickAccumulator;
        private bool foreground = true;
        private bool paused;
        private bool focused = true;
        private int ticks;
        private int rejectedHarvests;

        public event Action Changed;
        public long Coins => coins;
        public long Sold => sold;
        public int SpeedLevel => speedLevel;
        public int SourceBuffer => sourceBuffer;
        public int PackagerInput => packagerInput;
        public int DryerRemaining => dryerRemaining;
        public int PackagerRemaining => packagerRemaining;
        public bool DryerHasWip => dryerHasWip;
        public bool PackagerHasWip => packagerHasWip;
        public int SecondsUntilAuto => secondsUntilAuto;
        public int Ticks => ticks;
        public int RejectedHarvests => rejectedHarvests;
        public int Capacity => config != null ? config.bufferCapacity : 8;
        public bool IsForeground => foreground;
        public FactoryBalanceConfig Config => config;

        private void Awake()
        {
            if (config == null) config = Resources.Load<FactoryBalanceConfig>("FactoryBalanceConfig");
            if (config == null) Debug.LogError("FactoryRuntime requires FactoryBalanceConfig.", this);
            secondsUntilAuto = config != null ? config.automationIntervalSeconds : 12;
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
            if (sourceBuffer + config.manualBatch > Capacity)
            {
                rejectedHarvests++;
                Changed?.Invoke();
                return false;
            }
            sourceBuffer += config.manualBatch;
            Changed?.Invoke();
            return true;
        }

        public bool RequestSpeedUpgrade()
        {
            if (config == null || !foreground || purchaseQueued || speedLevel >= 1) return false;
            purchaseQueued = true;
            return true;
        }

        public void SetForeground(bool active)
        {
            foreground = active;
        }

        public void Tick()
        {
            if (config == null || !foreground) return;
            ticks++;

            if (dryerRemaining > 0) dryerRemaining--;
            if (packagerRemaining > 0) packagerRemaining--;

            // Complete downstream first. A full destination leaves completed WIP in place.
            if (packagerHasWip && packagerRemaining == 0)
            {
                packagerHasWip = false;
                sold = SaturatingAdd(sold, 1L, config.valueCap);
                coins = SaturatingAdd(coins, config.saleValue, config.valueCap);
            }
            if (dryerHasWip && dryerRemaining == 0 && packagerInput < Capacity)
            {
                dryerHasWip = false;
                packagerInput++;
            }

            if (!packagerHasWip && packagerInput > 0)
            {
                packagerInput--;
                packagerHasWip = true;
                packagerRemaining = config.PackagerDuration(speedLevel);
            }
            if (!dryerHasWip && sourceBuffer > 0)
            {
                sourceBuffer--;
                dryerHasWip = true;
                dryerRemaining = config.DryerDuration(speedLevel);
            }

            // Baseline automation is part of the source. Failed attempts are dropped, with no debt.
            secondsUntilAuto--;
            if (secondsUntilAuto <= 0)
            {
                if (sourceBuffer + config.manualBatch <= Capacity)
                    sourceBuffer += config.manualBatch;
                else
                    rejectedHarvests++;
                secondsUntilAuto = config.automationIntervalSeconds;
            }

            // One queued purchase at most. Funds are checked at commit time after any sale.
            if (purchaseQueued)
            {
                purchaseQueued = false;
                if (speedLevel == 0 && coins >= config.speedLevelOneCost)
                {
                    coins -= config.speedLevelOneCost;
                    speedLevel = 1;
                }
            }
            Changed?.Invoke();
        }

        public int TotalInFlight => sourceBuffer + packagerInput +
                                    (dryerHasWip ? 1 : 0) + (packagerHasWip ? 1 : 0);

        private static long SaturatingAdd(long current, long amount, long cap)
        {
            if (amount <= 0 || current >= cap) return Math.Min(current, cap);
            return amount >= cap - current ? cap : current + amount;
        }

        private void UpdateForeground() => SetForeground(!paused && focused);
        private void OnApplicationPause(bool value) { paused = value; UpdateForeground(); }
        private void OnApplicationFocus(bool value) { focused = value; UpdateForeground(); }
    }
}
