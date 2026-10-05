#if UNITY_EDITOR
using TinyFactory;
using TMPro;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TinyFactory.Editor
{
    internal static class M040SceneAuthoring
    {
        [MenuItem("Tiny Factory/Author M04 Upgrade Cards")]
        private static void AuthorCards()
        {
            FactoryHudView hud = Object.FindFirstObjectByType<FactoryHudView>();
            Button speed = GameObject.Find("Speed Upgrade Card")?.GetComponent<Button>();
            Button productivity = GameObject.Find("Productivity Upgrade Card")?.GetComponent<Button>();
            Button automation = GameObject.Find("Automation Upgrade Card")?.GetComponent<Button>();
            if (hud == null || speed == null || productivity == null || automation == null)
            {
                Debug.LogError("M04 card authoring requires HUD and all three authored upgrade card instances.");
                return;
            }

            Configure(speed, -172f, "Скорость L0/3\nСтанки 3→2с\nЦена 36 · ещё 36");
            Configure(productivity, 0f, "Партия L0/3\nСбор ×1→×2\nЦена 36 · ещё 36");
            Configure(automation, 172f, "Авто L0/3\nАвто 12→10с\nЦена 36 · ещё 36");

            FactoryProductionLineView line = Object.FindFirstObjectByType<FactoryProductionLineView>();
            Transform world = GameObject.Find("Factory World")?.transform;
            Transform anchors = GameObject.Find("Line Anchors")?.transform;
            GameObject originalFinal = GameObject.Find("Packager Sale Station");
            GameObject rollerStation = EnsureStation("Roller Station", "Assets/TinyFactory/Prefabs/DryerStation.prefab", world, new Vector3(0.55f, 0.12f, 0f));
            GameObject sealerStation = EnsureStation("Sealer Sale Station", "Assets/TinyFactory/Prefabs/PackagerSaleStation.prefab", world, new Vector3(3.05f, 0.12f, 0f));
            if (line == null || world == null || anchors == null || rollerStation == null || sealerStation == null || originalFinal == null)
            {
                Debug.LogError("M04 scene authoring could not resolve factory anchors or station prefabs.");
                return;
            }
            rollerStation.SetActive(false);
            sealerStation.SetActive(false);
            Transform rollerWip = EnsureAnchor(anchors, "Roller WIP", new Vector3(0.55f, 1.67f, -0.75f));
            var rollerQueue = new Transform[8];
            for (int i = 0; i < rollerQueue.Length; i++)
            {
                float x = -0.28f + 0.11f * i;
                float z = i % 2 == 0 ? -0.86f : -0.64f;
                rollerQueue[i] = EnsureAnchor(anchors, "Roller Queue " + (i + 1), new Vector3(x, 0.9f, z));
            }
            if (line != null)
            {
                SerializedObject lineSerialized = new SerializedObject(line);
                SerializedProperty slots = lineSerialized.FindProperty("rollerQueueSlots");
                slots.arraySize = rollerQueue.Length;
                for (int i = 0; i < rollerQueue.Length; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = rollerQueue[i];
                lineSerialized.FindProperty("rollerWipAnchor").objectReferenceValue = rollerWip;
                lineSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(line);
            }

            SerializedObject serialized = new SerializedObject(hud);
            Button harvest = serialized.FindProperty("harvestButton").objectReferenceValue as Button;
            serialized.FindProperty("harvestButtonLabel").objectReferenceValue = harvest != null
                ? harvest.GetComponentInChildren<TMP_Text>(true)
                : null;
            TMP_Text rollerStatus = EnsureRollerStatusLabel(serialized.FindProperty("safeAreaRoot").objectReferenceValue as RectTransform,
                serialized.FindProperty("packagerStatusLabel").objectReferenceValue as TMP_Text);
            serialized.FindProperty("productivityButton").objectReferenceValue = productivity;
            serialized.FindProperty("automationButton").objectReferenceValue = automation;
            serialized.FindProperty("productivityLabel").objectReferenceValue = productivity.GetComponentInChildren<TMP_Text>(true);
            serialized.FindProperty("automationLabel").objectReferenceValue = automation.GetComponentInChildren<TMP_Text>(true);
            serialized.FindProperty("rollerStation").objectReferenceValue = rollerStation;
            serialized.FindProperty("originalFinalStation").objectReferenceValue = originalFinal;
            serialized.FindProperty("sealerStation").objectReferenceValue = sealerStation;
            serialized.FindProperty("rollerStatusLabel").objectReferenceValue = rollerStatus;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddListenerIfMissing(speed, hud.HandleSpeedUpgradeButton);
            AddListenerIfMissing(productivity, hud.HandleProductivityUpgradeButton);
            AddListenerIfMissing(automation, hud.HandleAutomationUpgradeButton);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Debug.Log("M04 AUTHORED SCENE configured: three prefab-backed 3-column cards with persistent listeners, roller queue/WIP anchors, locked/unlocked roller and final sealer station references.");
        }

        [MenuItem("Tiny Factory/Preview M04/Affordable locked state")]
        private static void PreviewAffordableState() => SetPreviewState(36, 0, 0, 0, 0);

        [MenuItem("Tiny Factory/Preview M04/MAX and unlocked state")]
        private static void PreviewMaxState() => SetPreviewState(500, 40, 3, 3, 3);

        [MenuItem("Tiny Factory/Validate M04/Productivity L3 harvest click")]
        private static void ValidateProductivityL3HarvestClick()
        {
            FactoryRuntime runtime = Object.FindFirstObjectByType<FactoryRuntime>();
            FactoryHudView hud = Object.FindFirstObjectByType<FactoryHudView>();
            if (runtime == null || hud == null || !EditorApplication.isPlaying)
            {
                Debug.LogError("L3 harvest validation requires the active SampleScene in Play mode.");
                return;
            }

            SetPreviewState(500, 40, 3, 3, 3);
            runtime.SetForeground(true);
            typeof(FactoryHudView).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(hud, null);
            FieldInfo buttonField = typeof(FactoryHudView).GetField("harvestButton", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo labelField = typeof(FactoryHudView).GetField("harvestButtonLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo feedbackField = typeof(FactoryHudView).GetField("feedbackLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            Button button = buttonField?.GetValue(hud) as Button;
            TMP_Text label = labelField?.GetValue(hud) as TMP_Text;
            TMP_Text feedback = feedbackField?.GetValue(hud) as TMP_Text;
            if (button == null || label == null || feedback == null)
            {
                Debug.LogError("L3 harvest validation requires serialized harvest button/label and feedback references.");
                return;
            }

            button.onClick.Invoke();
            bool passed = runtime.SourceBuffer == 4 && label.text.Contains("+4") && feedback.text.Contains("+4") &&
                button.onClick.GetPersistentEventCount() == 1;
            if (!passed)
            {
                Debug.LogError("M040 HARVEST L3 CLICK FAIL: queue=" + runtime.SourceBuffer + ", button='" + label.text + "', feedback='" + feedback.text + "', persistentListeners=" + button.onClick.GetPersistentEventCount());
                return;
            }
            Debug.Log("M040 HARVEST L3 CLICK PASS: synthetic authored button click accepted queue=4; button='" + label.text + "'; feedback='" + feedback.text + "'; persistentListeners=1.");
        }

        [MenuItem("Tiny Factory/Preview M05/Save recovery notice")]
        private static void PreviewSaveNotice()
        {
            FactoryRuntime runtime = Object.FindFirstObjectByType<FactoryRuntime>();
            FactoryHudView hud = Object.FindFirstObjectByType<FactoryHudView>();
            if (runtime == null || hud == null || !EditorApplication.isPlaying)
            {
                Debug.LogError("Save notice preview requires the active SampleScene in Play mode.");
                return;
            }
            SetPreviewState(0, 0, 0, 0, 0);
            PropertyInfo status = typeof(FactoryRuntime).GetProperty("SaveStatus", BindingFlags.Instance | BindingFlags.Public);
            status?.GetSetMethod(true)?.Invoke(runtime, new object[] { "Сохранение повреждено; начата новая игра" });
            typeof(FactoryHudView).GetMethod("ShowSaveNotice", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(hud, new object[] { runtime.SaveStatus });
        }

        private static void SetPreviewState(long coins, long sold, int speed, int productivity, int automation)
        {
            FactoryRuntime runtime = Object.FindFirstObjectByType<FactoryRuntime>();
            if (runtime == null || !EditorApplication.isPlaying)
            {
                Debug.LogError("M04 state preview requires the active SampleScene in Play mode.");
                return;
            }
            SetRuntimeField(runtime, "savePath", null);
            SetRuntimeField(runtime, "coins", coins);
            SetRuntimeField(runtime, "sold", sold);
            SetRuntimeField(runtime, "speedLevel", speed);
            SetRuntimeField(runtime, "productivityLevel", productivity);
            SetRuntimeField(runtime, "automationLevel", automation);
            SetRuntimeField(runtime, "sourceBuffer", 0);
            SetRuntimeField(runtime, "rollerInput", 0);
            SetRuntimeField(runtime, "packagerInput", 0);
            SetRuntimeField(runtime, "dryerHasWip", false);
            SetRuntimeField(runtime, "rollerHasWip", false);
            SetRuntimeField(runtime, "packagerHasWip", false);
            SetRuntimeField(runtime, "dryerRemaining", 0);
            SetRuntimeField(runtime, "rollerRemaining", 0);
            SetRuntimeField(runtime, "packagerRemaining", 0);
            SetRuntimeField(runtime, "secondsUntilAuto", runtime.Config.AutomationInterval(automation));
            Action changed = typeof(FactoryRuntime).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(runtime) as Action;
            changed?.Invoke();
            Debug.Log("M04 preview state applied in memory only; local save path disabled for this Play session.");
        }

        private static void SetRuntimeField(FactoryRuntime runtime, string field, object value) =>
            typeof(FactoryRuntime).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(runtime, value);

        private static GameObject EnsureStation(string name, string prefabPath, Transform parent, Vector3 localPosition)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            GameObject value = existing != null ? existing.gameObject : null;
            if (value == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null || parent == null) return null;
                value = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                value.name = name;
            }
            value.transform.SetParent(parent, false);
            value.transform.localPosition = localPosition;
            EditorUtility.SetDirty(value);
            return value;
        }

        private static Transform EnsureAnchor(Transform parent, string name, Vector3 localPosition)
        {
            Transform value = parent != null ? parent.Find(name) : null;
            if (value == null)
            {
                GameObject anchor = new GameObject(name);
                value = anchor.transform;
                value.SetParent(parent, false);
            }
            value.localPosition = localPosition;
            EditorUtility.SetDirty(value.gameObject);
            return value;
        }

        private static void Configure(Button button, float x, string preview)
        {
            RectTransform rect = button.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .055f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(164f, 82f);
            rect.localScale = Vector3.one;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = preview;
                text.enableAutoSizing = true;
                text.fontSizeMin = 16f;
                text.fontSizeMax = 18f;
                text.fontSize = 14f;
                text.alignment = TextAlignmentOptions.Center;
            }
        }

        private static TMP_Text EnsureRollerStatusLabel(RectTransform safeAreaRoot, TMP_Text styleSource)
        {
            if (safeAreaRoot == null || styleSource == null) return null;
            Transform existing = safeAreaRoot.Find("Roller Status Label");
            GameObject labelObject;
            if (existing != null)
            {
                labelObject = existing.gameObject;
            }
            else
            {
                labelObject = Object.Instantiate(styleSource.gameObject, safeAreaRoot, false);
                labelObject.name = "Roller Status Label";
            }
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            RectTransform rect = labelObject.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .283f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(310f, 22f);
            rect.localScale = Vector3.one;
            label.text = "Скрутчик · 0/15";
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            EditorUtility.SetDirty(labelObject);
            return label;
        }

        private static void AddListenerIfMissing(Button button, UnityEngine.Events.UnityAction callback)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == callback.Target as UnityEngine.Object &&
                    button.onClick.GetPersistentMethodName(i) == callback.Method.Name) return;
            UnityEventTools.AddPersistentListener(button.onClick, callback);
            EditorUtility.SetDirty(button);
        }
    }
}
#endif
