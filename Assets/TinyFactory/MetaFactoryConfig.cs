using UnityEngine;

namespace TinyFactory
{
    public enum MetaProduct { FreshLeaf, DryTea, TeaPacket }
    public enum MetaStation { Garden, Dryer, Packer }

    [CreateAssetMenu(menuName = "Tiny Factory/Meta Factory Config", fileName = "MetaFactoryConfig")]
    public sealed class MetaFactoryConfig : ScriptableObject
    {
        [Header("Caps")]
        [Min(1)] public int warehouseTotalCapacity = 40;
        [Min(1)] public int warehouseProductCapacity = 20;
        [Min(1)] public int localCapacity = 8;
        [Min(1)] public long walletCapacity = 1000000000000L;
        [Header("Garden")]
        [Min(1)] public int gardenSeconds = 15;
        public int[] gardenSecondsByLevel = { 15, 12, 9 };
        [Header("Dryer")]
        [Min(1)] public int freshLeafPerBatch = 2;
        public int[] dryerSecondsByLevel = { 8, 6, 4 };
        [Header("Packer")]
        [Min(1)] public int[] packerSecondsByLevel = { 6, 5, 4 };
        [Header("Economy")]
        public long freshSaveCoins = 80;
        [Min(1)] public int freshStartQuantity = 4;
        [Min(1)] public int onboardingFreshLeafQuantity = 4;
        [Min(1)] public long[] rewardPerUnitByProduct = { 2, 5, 6 };
        [Min(1)] public int pilotDryTeaQuantity = 2;
        [Min(1)] public int pilotTeaPacketQuantity = 2;
        public long dryerBuildCost = 40;
        public long packerBuildCost = 70;
        public long[] gardenUpgradeCosts = { 30, 65 };
        public long[] dryerUpgradeCosts = { 45, 90 };
        public long[] packerUpgradeCosts = { 55, 110 };
        public int dryerUnlockPackerAtProduced = 5;

        public bool IsValid() => warehouseTotalCapacity > 0 && warehouseProductCapacity > 0 &&
            warehouseProductCapacity <= warehouseTotalCapacity && localCapacity > 0 && walletCapacity > 0 &&
            gardenSeconds > 0 && HasThree(gardenSecondsByLevel) && freshLeafPerBatch > 0 &&
            HasThree(dryerSecondsByLevel) && HasThree(packerSecondsByLevel) && HasThree(rewardPerUnitByProduct) &&
            HasTwo(gardenUpgradeCosts) && HasTwo(dryerUpgradeCosts) && HasTwo(packerUpgradeCosts) &&
            freshSaveCoins >= 0 && freshSaveCoins <= walletCapacity && freshStartQuantity > 0 && freshStartQuantity <= warehouseProductCapacity && freshStartQuantity <= warehouseTotalCapacity &&
            onboardingFreshLeafQuantity > 0 && onboardingFreshLeafQuantity <= warehouseProductCapacity && onboardingFreshLeafQuantity <= warehouseTotalCapacity &&
            freshLeafPerBatch > 0 && freshLeafPerBatch <= localCapacity &&
            pilotDryTeaQuantity > 0 && pilotTeaPacketQuantity > 0 && pilotDryTeaQuantity <= warehouseProductCapacity && pilotTeaPacketQuantity <= warehouseProductCapacity &&
            pilotDryTeaQuantity + pilotTeaPacketQuantity <= warehouseTotalCapacity &&
            dryerBuildCost >= 0 && packerBuildCost >= 0 && dryerUnlockPackerAtProduced > 0;

        private static bool HasThree(int[] values) => values != null && values.Length == 3 && values[0] > 0 && values[1] > 0 && values[2] > 0;
        private static bool HasThree(long[] values) => values != null && values.Length == 3 && values[0] > 0 && values[1] > 0 && values[2] > 0;
        private static bool HasTwo(long[] values) => values != null && values.Length == 2 && values[0] >= 0 && values[1] >= 0;
    }
}
