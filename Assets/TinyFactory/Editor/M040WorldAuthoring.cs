#if UNITY_EDITOR
using TinyFactory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory.Editor
{
    internal static class M040WorldAuthoring
    {
        private const int QueueSlotCount = 8;
        private const string WorldName = "Factory World";
        private const string AnchorsName = "Line Anchors";

        [MenuItem("Tiny Factory/Author M04 World")]
        private static void AuthorWorld()
        {
            FactoryProductionLineView line = Object.FindFirstObjectByType<FactoryProductionLineView>();
            FactoryHudView hud = Object.FindFirstObjectByType<FactoryHudView>();
            Transform world = GameObject.Find(WorldName)?.transform;
            Transform anchors = GameObject.Find(AnchorsName)?.transform;
            GameObject originalFinal = GameObject.Find("Packager Sale Station");
            if (line == null || hud == null || world == null || anchors == null || originalFinal == null)
            {
                Debug.LogError("M04 world authoring requires the baseline factory world, line anchors, final station, line view, and HUD.");
                return;
            }

            Vector3 finalStationPosition = originalFinal.transform.localPosition;
            Quaternion finalStationRotation = originalFinal.transform.localRotation;
            GameObject rollerStation = EnsureStation("Roller Station", "Assets/TinyFactory/Prefabs/DryerStation.prefab",
                world, new Vector3(0.55f, 0.12f, 0f));
            GameObject sealerStation = EnsureStation("Sealer Sale Station", "Assets/TinyFactory/Prefabs/PackagerSaleStation.prefab",
                world, finalStationPosition);
            if (rollerStation == null || sealerStation == null)
            {
                Debug.LogError("M04 world authoring could not load the existing station prefabs.");
                return;
            }
            sealerStation.transform.localRotation = finalStationRotation;

            GameObject lockedPlaceholder = EnsureLockedPlaceholder(world);
            EnsureRollerDetails(rollerStation.transform);
            EnsureSealerDetails(sealerStation.transform);
            Transform[] sourceSlots = EnsureQueue(anchors, "Source Queue ", QueueSlotCount,
                i => new Vector3(-4.25f + 0.32f * i, 1f, -0.75f));
            Transform[] rollerSlots = EnsureQueue(anchors, "Roller Queue ", QueueSlotCount,
                i => new Vector3(-0.28f + 0.14f * i, 0.9f, i % 2 == 0 ? -0.86f : -0.64f));
            Transform[] finalSlots = EnsureQueue(anchors, "Packager Queue ", QueueSlotCount,
                i => new Vector3(0.15f + 0.32f * i, 0.9f, -0.75f));
            Transform dryerWip = EnsureAnchor(anchors, "Dryer WIP", new Vector3(-0.75f, 1.67f, -0.75f));
            Transform rollerWip = EnsureAnchor(anchors, "Roller WIP", new Vector3(0.55f, 1.67f, -0.75f));
            Transform finalWip = EnsureAnchor(anchors, "Packager WIP",
                finalStationPosition + new Vector3(0f, 1.33f, -0.75f));

            SerializedObject lineSerialized = new SerializedObject(line);
            SetArray(lineSerialized, "sourceQueueSlots", sourceSlots);
            lineSerialized.FindProperty("dryerWipAnchor").objectReferenceValue = dryerWip;
            SetArray(lineSerialized, "rollerQueueSlots", rollerSlots);
            lineSerialized.FindProperty("rollerWipAnchor").objectReferenceValue = rollerWip;
            SetArray(lineSerialized, "packagerQueueSlots", finalSlots);
            lineSerialized.FindProperty("packagerWipAnchor").objectReferenceValue = finalWip;
            lineSerialized.FindProperty("rollerLockedPlaceholder").objectReferenceValue = lockedPlaceholder;
            lineSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(line);

            SerializedObject hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("rollerStation").objectReferenceValue = rollerStation;
            hudSerialized.FindProperty("originalFinalStation").objectReferenceValue = originalFinal;
            hudSerialized.FindProperty("sealerStation").objectReferenceValue = sealerStation;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);

            RectTransform safeAreaRoot = hudSerialized.FindProperty("safeAreaRoot").objectReferenceValue as RectTransform;
            RectTransform rollerProgressFill = EnsureRollerProgressBar(safeAreaRoot);
            if (rollerProgressFill != null)
            {
                lineSerialized = new SerializedObject(line);
                lineSerialized.FindProperty("rollerProgressFill").objectReferenceValue = rollerProgressFill;
                lineSerialized.FindProperty("rollerProgressImage").objectReferenceValue = rollerProgressFill.GetComponent<Image>();
                lineSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(line);
            }

            rollerStation.SetActive(false);
            sealerStation.SetActive(false);
            lockedPlaceholder.SetActive(true);
            EditorSceneManager.MarkSceneDirty(line.gameObject.scene);
            Debug.Log("M04 world authored: three runtime queue/WIP stages, locked roller placeholder, roller and in-place sealer references.");
        }

        private static GameObject EnsureStation(string name, string prefabPath, Transform parent, Vector3 localPosition)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            GameObject value = existing != null ? existing.gameObject : null;
            if (value == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) return null;
                value = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (value == null) return null;
                value.name = name;
            }
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            EditorUtility.SetDirty(value);
            return value;
        }

        private static void EnsureRollerDetails(Transform station)
        {
            Material rollerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/Packager.mat");
            Material accentMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/TeaPackage.mat");
            EnsurePrimitive(station, "Roller drum upper", PrimitiveType.Cylinder,
                new Vector3(0f, 1.18f, -0.56f), new Vector3(0f, 0f, 90f), new Vector3(0.2f, 0.22f, 0.2f), rollerMaterial);
            EnsurePrimitive(station, "Roller drum lower", PrimitiveType.Cylinder,
                new Vector3(0f, 0.91f, -0.56f), new Vector3(0f, 0f, 90f), new Vector3(0.2f, 0.22f, 0.2f), rollerMaterial);
            EnsurePrimitive(station, "Roller drive cap left", PrimitiveType.Cylinder,
                new Vector3(-0.37f, 1.04f, -0.56f), new Vector3(0f, 0f, 90f), new Vector3(0.1f, 0.1f, 0.1f), accentMaterial);
            EnsurePrimitive(station, "Roller drive cap right", PrimitiveType.Cylinder,
                new Vector3(0.37f, 1.04f, -0.56f), new Vector3(0f, 0f, 90f), new Vector3(0.1f, 0.1f, 0.1f), accentMaterial);
        }

        private static void EnsureSealerDetails(Transform station)
        {
            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/Packager.mat");
            Material hotMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/Dryer.mat");
            Material trimMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/TeaPackage.mat");
            EnsureBlock(station, "Sealer press left", new Vector3(-0.38f, 1.38f, -0.58f), new Vector3(0.1f, 0.76f, 0.16f), bodyMaterial);
            EnsureBlock(station, "Sealer press right", new Vector3(0.38f, 1.38f, -0.58f), new Vector3(0.1f, 0.76f, 0.16f), bodyMaterial);
            EnsureBlock(station, "Sealer press bridge", new Vector3(0f, 1.73f, -0.58f), new Vector3(0.86f, 0.12f, 0.16f), bodyMaterial);
            EnsureBlock(station, "Sealer heated jaw", new Vector3(0f, 1.49f, -0.68f), new Vector3(0.56f, 0.12f, 0.08f), hotMaterial);
            EnsureBlock(station, "Sealer gold trim", new Vector3(0f, 1.35f, -0.68f), new Vector3(0.62f, 0.055f, 0.085f), trimMaterial);
        }

        private static RectTransform EnsureRollerProgressBar(RectTransform safeAreaRoot)
        {
            if (safeAreaRoot == null) return null;
            Transform backgroundTransform = safeAreaRoot.Find("Roller progress background");
            GameObject background = backgroundTransform != null ? backgroundTransform.gameObject : new GameObject("Roller progress background", typeof(RectTransform), typeof(Image));
            if (backgroundTransform == null)
            {
                Undo.RegisterCreatedObjectUndo(background, "Create roller progress bar");
                background.transform.SetParent(safeAreaRoot, false);
            }
            RectTransform backgroundRect = background.transform as RectTransform;
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.265f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(302.4f, 11.52f);
            backgroundRect.localScale = Vector3.one;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.color = new Color(0.08f, 0.12f, 0.13f, 0.75f);

            Transform fillTransform = background.transform.Find("Roller progress fill");
            GameObject fill = fillTransform != null ? fillTransform.gameObject : new GameObject("Roller progress fill", typeof(RectTransform), typeof(Image));
            if (fillTransform == null)
            {
                Undo.RegisterCreatedObjectUndo(fill, "Create roller progress fill");
                fill.transform.SetParent(background.transform, false);
            }
            RectTransform fillRect = fill.transform as RectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.raycastTarget = false;
            fillImage.color = new Color(0.88f, 0.67f, 0.25f, 1f);
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(fill);
            return fillRect;
        }

        private static void EnsurePrimitive(Transform parent, string name, PrimitiveType type,
            Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject value = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
            value.name = name;
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(value, "Create station detail");
                value.transform.SetParent(parent, false);
                Collider collider = value.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }
            value.transform.localPosition = localPosition;
            value.transform.localEulerAngles = localEulerAngles;
            value.transform.localScale = localScale;
            Renderer renderer = value.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            EditorUtility.SetDirty(value);
        }

        private static GameObject EnsureLockedPlaceholder(Transform world)
        {
            Transform existing = world.Find("Roller Locked Placeholder");
            GameObject root;
            if (existing == null)
            {
                root = new GameObject("Roller Locked Placeholder");
                Undo.RegisterCreatedObjectUndo(root, "Create roller locked placeholder");
                root.transform.SetParent(world, false);
            }
            else root = existing.gameObject;

            root.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/Table.mat");
            Material trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/TinyFactory/Materials/ConveyorEdge.mat");
            EnsureBlock(root.transform, "Foundation", Vector3.zero, new Vector3(1.18f, 0.13f, 0.76f), wood);
            EnsureBlock(root.transform, "Left scaffold", new Vector3(-0.42f, 0.48f, 0f), new Vector3(0.12f, 0.78f, 0.16f), trim);
            EnsureBlock(root.transform, "Right scaffold", new Vector3(0.42f, 0.48f, 0f), new Vector3(0.12f, 0.78f, 0.16f), trim);
            EnsureBlock(root.transform, "Top scaffold", new Vector3(0f, 0.83f, 0f), new Vector3(0.96f, 0.12f, 0.16f), wood);
            EnsureBlock(root.transform, "Locked marker", new Vector3(0f, 0.47f, -0.12f), new Vector3(0.3f, 0.3f, 0.08f), trim);
            EditorUtility.SetDirty(root);
            return root;
        }

        private static void EnsureBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject block = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(block, "Create roller placeholder block");
                block.transform.SetParent(parent, false);
                Collider collider = block.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            EditorUtility.SetDirty(block);
        }

        private static Transform[] EnsureQueue(Transform parent, string prefix, int count, System.Func<int, Vector3> position)
        {
            Transform[] slots = new Transform[count];
            for (int i = 0; i < count; i++)
                slots[i] = EnsureAnchor(parent, prefix + (i + 1), position(i));
            return slots;
        }

        private static Transform EnsureAnchor(Transform parent, string name, Vector3 localPosition)
        {
            Transform value = parent.Find(name);
            if (value == null)
            {
                GameObject anchor = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(anchor, "Create production line anchor");
                value = anchor.transform;
                value.SetParent(parent, false);
            }
            value.localPosition = localPosition;
            EditorUtility.SetDirty(value.gameObject);
            return value;
        }

        private static void SetArray(SerializedObject serialized, string propertyName, Transform[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
