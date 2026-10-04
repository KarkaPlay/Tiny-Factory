using UnityEngine;

namespace TinyFactory
{
    [CreateAssetMenu(menuName = "Tiny Factory/Factory Balance Config", fileName = "FactoryBalanceConfig")]
    public sealed class FactoryBalanceConfig : ScriptableObject
    {
        [Min(1)] public int bufferCapacity = 8;
        [Min(1)] public int manualBatch = 1;
        [Min(1)] public int automationIntervalSeconds = 12;
        [Min(1)] public int dryerSeconds = 3;
        [Min(1)] public int dryerSecondsAtSpeedLevelOne = 2;
        [Min(1)] public int packagerSeconds = 2;
        [Min(0)] public int speedLevelOneCost = 36;
        [Min(1)] public long saleValue = 4;
        [Min(1)] public long valueCap = 1000000000000L;

        public int DryerDuration(int speedLevel) => speedLevel >= 1 ? dryerSecondsAtSpeedLevelOne : dryerSeconds;
        public int PackagerDuration(int speedLevel) => packagerSeconds;
    }
}
