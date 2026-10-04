#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TinyFactory.Editor
{
    [InitializeOnLoad]
    internal static class FactoryConfigInstaller
    {
        private const string Folder = "Assets/TinyFactory/Resources";
        private const string AssetPath = Folder + "/FactoryBalanceConfig.asset";

        static FactoryConfigInstaller()
        {
            EditorApplication.delayCall += EnsureConfig;
        }

        [MenuItem("Tiny Factory/Create Default Balance Config")]
        private static void EnsureConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<FactoryBalanceConfig>(AssetPath) != null) return;
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/TinyFactory"))
                    AssetDatabase.CreateFolder("Assets", "TinyFactory");
                AssetDatabase.CreateFolder("Assets/TinyFactory", "Resources");
            }
            var config = ScriptableObject.CreateInstance<FactoryBalanceConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
