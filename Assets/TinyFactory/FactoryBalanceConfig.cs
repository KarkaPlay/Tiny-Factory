using UnityEngine;

namespace TinyFactory
{
    [CreateAssetMenu(menuName = "Tiny Factory/Factory Balance Config", fileName = "FactoryBalanceConfig")]
    public sealed class FactoryBalanceConfig : ScriptableObject
    {
        [Min(1)] public int bufferCapacity = 8;
        [Min(1)] public int baseManualBatch = 1;
        [Min(1)] public int baseAutomationIntervalSeconds = 12;
        [Min(1)] public int[] upgradeCosts = { 36, 54, 72 };
        [Min(1)] public int[] dryerSecondsBySpeedLevel = { 3, 2, 2, 1 };
        [Min(1)] public int[] rollerSecondsBySpeedLevel = { 2, 1, 1, 1 };
        [Min(1)] public int[] finalSecondsBySpeedLevel = { 2, 2, 1, 1 };
        [Min(1)] public int[] manualBatchByProductivityLevel = { 1, 2, 3, 4 };
        [Min(1)] public int[] automationIntervalByLevel = { 12, 10, 8, 6 };
        [Min(1)] public int rollerUnlockSales = 15;
        [Min(1)] public int sealerUnlockSales = 40;
        [Min(1)] public long saleValue = 4;
        [Min(1)] public long rollerTierSaleValue = 5;
        [Min(1)] public long sealerTierSaleValue = 6;
        [Min(1)] public long valueCap = 1000000000000L;

        public int UpgradeCost(int currentLevel) => currentLevel < 0 || currentLevel >= upgradeCosts.Length ? 0 : upgradeCosts[currentLevel];
        public int DryerDuration(int level) => At(dryerSecondsBySpeedLevel, level, 3);
        public int RollerDuration(int level) => At(rollerSecondsBySpeedLevel, level, 2);
        public int FinalDuration(int level) => At(finalSecondsBySpeedLevel, level, 2);
        public int PackagerDuration(int level) => FinalDuration(level);
        public int ManualBatch(int level) => At(manualBatchByProductivityLevel, level, 1);
        public int AutomationInterval(int level) => At(automationIntervalByLevel, level, 12);

        private static int At(int[] values, int level, int fallback) =>
            values != null && level >= 0 && level < values.Length && values[level] > 0 ? values[level] : fallback;
    }
}
