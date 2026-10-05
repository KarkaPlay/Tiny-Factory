#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TinyFactory;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyFactory.Editor
{
    internal static class MetaFactorySceneAuthoring
    {
        private const string CanvasName = "M051 Authored HUD";
        private const string WorldName = "M050 Meta Factory World";
        private const string RowPrefabPath = "Assets/TinyFactory/Prefabs/MetaOrderRow.prefab";

        [MenuItem("Tiny Factory/Author M05 Scene")]
        private static void AuthorScene()
        {
            MetaFactoryConfig config = AssetDatabase.LoadAssetAtPath<MetaFactoryConfig>("Assets/TinyFactory/Resources/MetaFactoryConfig.asset");
            if (config == null || !config.IsValid())
            {
                Debug.LogError("M05 authoring requires the valid MetaFactoryConfig asset.");
                return;
            }
            // The legacy 0.4 scene contains multiple UI roots; disabling only its controller
            // can leave static legacy labels/buttons rendered over the new HUD.
            Canvas[] existingCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas existingCanvas in existingCanvases)
            {
                if (existingCanvas == null) continue;
                if (existingCanvas.gameObject.name == CanvasName) UnityEngine.Object.DestroyImmediate(existingCanvas.gameObject);
                else existingCanvas.gameObject.SetActive(false);
            }
            FactoryHudView oldHud = UnityEngine.Object.FindFirstObjectByType<FactoryHudView>();
            if (oldHud != null)
            {
                Canvas oldCanvas = oldHud.GetComponentInParent<Canvas>();
                if (oldCanvas != null) oldCanvas.gameObject.SetActive(false);
                else oldHud.enabled = false;
            }
            FactoryRuntime oldRuntime = UnityEngine.Object.FindFirstObjectByType<FactoryRuntime>();
            if (oldRuntime != null) oldRuntime.enabled = false;
            FactoryProductionLineView oldLine = UnityEngine.Object.FindFirstObjectByType<FactoryProductionLineView>();
            if (oldLine != null) oldLine.enabled = false;
            GameObject oldWorld = GameObject.Find("Factory World");
            if (oldWorld != null) oldWorld.SetActive(false);
            GameObject oldAnchors = GameObject.Find("Line Anchors");
            if (oldAnchors != null) oldAnchors.SetActive(false);

            MetaOrderRowView rowPrefab = EnsureOrderRowPrefab();
            Camera camera = EnsureCamera();
            Transform worldRoot = EnsureRoot(WorldName).transform;
            MetaFactoryRuntime runtime = EnsureRoot("M050 Meta Factory Runtime").GetComponent<MetaFactoryRuntime>();
            if (runtime == null) runtime = Undo.AddComponent<MetaFactoryRuntime>(EnsureRoot("M050 Meta Factory Runtime"));
            SerializedObject runtimeSo = new SerializedObject(runtime);
            runtimeSo.FindProperty("config").objectReferenceValue = config;
#if UNITY_EDITOR
            SerializedProperty editorSaveDirectory = runtimeSo.FindProperty("editorSaveDirectoryOverride");
            if (editorSaveDirectory != null) editorSaveDirectory.stringValue = Path.Combine(Path.GetTempPath(), "tinyfactory-m050-play-save");
#endif
            runtimeSo.ApplyModifiedPropertiesWithoutUndo();
            AuthorPlots(worldRoot, runtime);
            MetaFactoryHudView hud = AuthorHud(runtime, rowPrefab);
            RectTransform safeArea = (RectTransform)hud.transform.Find("Safe Area Root");
            AuthorNearActions(safeArea, worldRoot, runtime, hud, camera, TMP_Settings.defaultFontAsset);
            AuthorMotionView(hud.gameObject, safeArea, worldRoot, runtime, camera);
            MetaFactoryWorldView worldView = EnsureRoot("M050 World Input").GetComponent<MetaFactoryWorldView>();
            if (worldView == null) worldView = Undo.AddComponent<MetaFactoryWorldView>(EnsureRoot("M050 World Input"));
            SerializedObject worldSo = new SerializedObject(worldView);
            worldSo.FindProperty("worldCamera").objectReferenceValue = camera;
            worldSo.FindProperty("hud").objectReferenceValue = hud;
            worldSo.FindProperty("plotsRoot").objectReferenceValue = worldRoot;
            worldSo.FindProperty("minimumCameraY").floatValue = -12f;
            worldSo.FindProperty("maximumCameraY").floatValue = 0f;
            worldSo.FindProperty("gestureThresholdDp").floatValue = 12f;
            worldSo.FindProperty("selectionHysteresisDp").floatValue = 24f;
            worldSo.FindProperty("viewportBottomFraction").floatValue = .49f;
            worldSo.FindProperty("viewportTopFraction").floatValue = .83f;
            worldSo.FindProperty("snapDuration").floatValue = .24f;
            worldSo.ApplyModifiedPropertiesWithoutUndo();
            EnsureInputSystemEventSystem();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("M05 scene authored: active-only runtime, one vertical column, pinned safe-area HUD and Input System UI surface.");
        }

        private static Camera EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                Undo.RegisterCreatedObjectUndo(go, "Create M05 camera");
                go.tag = "MainCamera";
                camera = go.GetComponent<Camera>();
            }
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 7f, -12f);
            // The steeper top-down angle keeps the complete selected plot above the tutorial strip.
            camera.transform.rotation = Quaternion.Euler(34.5f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(227, 221, 198, 255);
            EditorUtility.SetDirty(camera);
            return camera;
        }

        private static void AuthorPlots(Transform root, MetaFactoryRuntime runtime)
        {
            string[] names = { "Garden Plot", "Dryer Plot", "Packer Plot" };
            string[] prefabs = { "Assets/TinyFactory/Prefabs/TeaBush.prefab", "Assets/TinyFactory/Prefabs/DryerStation.prefab", "Assets/TinyFactory/Prefabs/PackagerSaleStation.prefab" };
            MetaStation[] stations = { MetaStation.Garden, MetaStation.Dryer, MetaStation.Packer };
            Vector3[] positions = { new Vector3(0f, 0f, 0f), new Vector3(0f, -6f, 0f), new Vector3(0f, -12f, 0f) };
            for (int i = 0; i < names.Length; i++)
            {
                Transform existing = root.Find(names[i]);
                GameObject plotRoot;
                if (existing != null) plotRoot = existing.gameObject;
                else
                {
                    plotRoot = new GameObject(names[i]);
                    Undo.RegisterCreatedObjectUndo(plotRoot, "Create M05 plot");
                    plotRoot.transform.SetParent(root, false);
                    Transform visual = new GameObject("Station Visual").transform;
                    Undo.RegisterCreatedObjectUndo(visual.gameObject, "Create station visual root");
                    visual.SetParent(plotRoot.transform, false);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
                    if (prefab == null) { Debug.LogError("Missing M05 plot prefab: " + prefabs[i]); return; }
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, visual);
                    instance.name = "Building Visual";
                }
                Transform stationVisual = plotRoot.transform.Find("Station Visual");
                if (stationVisual == null)
                {
                    stationVisual = new GameObject("Station Visual").transform;
                    Undo.RegisterCreatedObjectUndo(stationVisual.gameObject, "Create station visual root");
                    stationVisual.SetParent(plotRoot.transform, false);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[i]);
                    if (prefab == null) { Debug.LogError("Missing M05 plot prefab: " + prefabs[i]); return; }
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stationVisual);
                    instance.name = "Building Visual";
                }
                plotRoot.transform.SetParent(root, false);
                plotRoot.transform.localPosition = positions[i];
                plotRoot.transform.localRotation = Quaternion.identity;
                plotRoot.transform.localScale = Vector3.one;
                MetaFactoryPlot plot = plotRoot.GetComponent<MetaFactoryPlot>();
                if (plot == null) plot = Undo.AddComponent<MetaFactoryPlot>(plotRoot);
                if (plotRoot.GetComponent<Collider>() == null)
                {
                    BoxCollider collider = Undo.AddComponent<BoxCollider>(plotRoot);
                    collider.center = new Vector3(0f, 1.1f, 0f);
                    collider.size = new Vector3(4f, 3.2f, 1.4f);
                }
                SerializedObject plotSo = new SerializedObject(plot);
                plotSo.FindProperty("station").enumValueIndex = (int)stations[i];
                plotSo.FindProperty("runtime").objectReferenceValue = runtime;
                plotSo.FindProperty("stationVisual").objectReferenceValue = stationVisual.gameObject;
                plotSo.FindProperty("lockedVisual").objectReferenceValue = plotRoot.transform.Find("Locked Visual")?.gameObject;
                plotSo.ApplyModifiedPropertiesWithoutUndo();
                EnsureLockedVisual(plotRoot.transform, names[i]);
            }
        }

        private static void EnsureLockedVisual(Transform plot, string name)
        {
            if (plot.Find("Locked Visual") != null) return;
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(placeholder, "Create locked plot marker");
            placeholder.name = "Locked Visual";
            placeholder.transform.SetParent(plot, false);
            placeholder.transform.localPosition = new Vector3(0f, 0.2f, 0.2f);
            placeholder.transform.localScale = new Vector3(3.8f, 0.28f, 1.35f);
            UnityEngine.Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            MetaFactoryPlot component = plot.GetComponent<MetaFactoryPlot>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("lockedVisual").objectReferenceValue = placeholder;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MetaFactoryHudView AuthorHud(MetaFactoryRuntime runtime, MetaOrderRowView rowPrefab)
        {
            GameObject canvasGo = GameObject.Find(CanvasName);
            if (canvasGo != null) UnityEngine.Object.DestroyImmediate(canvasGo);
            canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create M05 HUD");
            Canvas canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .45f;
            RectTransform safe = Rect("Safe Area Root", canvas.transform); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null) Debug.LogWarning("TMP default font asset missing during M05 authoring; check after import.");

            RectTransform header = Panel("Pinned Header", safe, new Vector2(0, .83f), new Vector2(1, 1), new Color32(239, 231, 204, 245));
            TMP_Text wallet = Text(header, "Wallet", "Монеты · 80", 39, font, TextAlignmentOptions.Left, new Vector2(.05f, .66f), new Vector2(.95f, .98f));
            TMP_Text active = Text(header, "Active Order", "Активный заказ · выберите предложение", 28, font, TextAlignmentOptions.Left, new Vector2(.05f, .34f), new Vector2(.95f, .66f));
            TMP_Text status = Text(header, "Foreground Notice", "Прототип 0.5 · производство только в открытой игре", 21, font, TextAlignmentOptions.Left, new Vector2(.05f, .03f), new Vector2(.95f, .34f));

            RectTransform lower = Panel("Pinned Lower Card", safe, new Vector2(0, 0), new Vector2(1, .41f), new Color32(248, 244, 229, 250));
            RectTransform tabs = Rect("Tabs", lower); tabs.anchorMin = new Vector2(.02f, .87f); tabs.anchorMax = new Vector2(.98f, .99f); tabs.offsetMin = Vector2.zero; tabs.offsetMax = Vector2.zero;
            HorizontalLayoutGroup tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); tabLayout.spacing = 12; tabLayout.childControlWidth = true; tabLayout.childControlHeight = true; tabLayout.childForceExpandHeight = true; tabLayout.childForceExpandWidth = true;
            Button factoryTab = Button(tabs, "Завод", font, 28);
            Button warehouseTab = Button(tabs, "Склад", font, 28);
            Button ordersTab = Button(tabs, "Заказы", font, 28);
            GameObject factory = Panel("Factory Panel", lower, new Vector2(.02f, .03f), new Vector2(.98f, .85f), Color.clear).gameObject;
            GameObject warehouse = Panel("Warehouse Panel", lower, new Vector2(.02f, .03f), new Vector2(.98f, .85f), Color.clear).gameObject;
            GameObject orders = Panel("Orders Panel", lower, new Vector2(.02f, .03f), new Vector2(.98f, .85f), Color.clear).gameObject;
            warehouse.SetActive(false); orders.SetActive(false);

            TMP_Text selectedName = Text(factory.transform, "Selected Plot", "Чайный сад", 30, font, TextAlignmentOptions.Left, new Vector2(.02f, .72f), new Vector2(.98f, .98f));
            TMP_Text selectedState = Text(factory.transform, "Station State", "Уровень 0", 25, font, TextAlignmentOptions.Left, new Vector2(.02f, .45f), new Vector2(.98f, .73f));
            TMP_Text buildLabel = Text(factory.transform, "Build Status", "", 23, font, TextAlignmentOptions.Left, new Vector2(.02f, .36f), new Vector2(.98f, .47f));
            Button build = Button(factory.transform, "Построить", font, 25); Position(build.GetComponent<RectTransform>(), new Vector2(.02f, .22f), new Vector2(.48f, .36f));
            TMP_Text upgradeLabel = Text(factory.transform, "Upgrade Status", "", 22, font, TextAlignmentOptions.Left, new Vector2(.52f, .32f), new Vector2(.98f, .43f));
            Button upgrade = Button(factory.transform, "Улучшить", font, 24); Position(upgrade.GetComponent<RectTransform>(), new Vector2(.52f, .19f), new Vector2(.98f, .32f));
            TMP_Text transferPreview = Text(factory.transform, "Station Inventory Detail", "", 20, font, TextAlignmentOptions.Left, new Vector2(.02f, .015f), new Vector2(.98f, .17f));
            TMP_Text warehouseText = Text(warehouse.transform, "Warehouse Inventory", "Общий склад", 30, font, TextAlignmentOptions.Left, new Vector2(.04f, .12f), new Vector2(.96f, .93f));

            TMP_Text activeDetails = Text(orders.transform, "Active Order Details", "Нет активного заказа", 23, font, TextAlignmentOptions.Left, new Vector2(.02f, .70f), new Vector2(.98f, .98f));
            Button turnIn = Button(orders.transform, "Сдать заказ", font, 22); Position(turnIn.GetComponent<RectTransform>(), new Vector2(.02f, .60f), new Vector2(.48f, .72f));
            Button cancel = Button(orders.transform, "Отказаться…", font, 22); Position(cancel.GetComponent<RectTransform>(), new Vector2(.52f, .60f), new Vector2(.98f, .72f));
            RectTransform cancelPanel = Panel("Cancel Confirmation", orders.transform, new Vector2(.02f, .42f), new Vector2(.98f, .59f), new Color32(247, 228, 209, 255)); cancelPanel.gameObject.SetActive(false);
            Text(cancelPanel, "Cancel Text", "Отказ не расходует товары и не выдаёт награду. Подтвердить?", 20, font, TextAlignmentOptions.Left, new Vector2(.02f, .45f), new Vector2(.98f, .98f));
            Button confirmCancel = Button(cancelPanel, "Подтвердить", font, 19); Position(confirmCancel.GetComponent<RectTransform>(), new Vector2(.02f, .02f), new Vector2(.48f, .43f));
            Button dismissCancel = Button(cancelPanel, "Вернуться", font, 19); Position(dismissCancel.GetComponent<RectTransform>(), new Vector2(.52f, .02f), new Vector2(.98f, .43f));
            RectTransform offers = Rect("Offer Rows", orders.transform); offers.anchorMin = new Vector2(.02f, .01f); offers.anchorMax = new Vector2(.98f, .58f); offers.offsetMin = Vector2.zero; offers.offsetMax = Vector2.zero;
            VerticalLayoutGroup offerLayout = offers.gameObject.AddComponent<VerticalLayoutGroup>(); offerLayout.spacing = 8; offerLayout.childControlWidth = true; offerLayout.childControlHeight = true; offerLayout.childForceExpandWidth = true; offerLayout.childForceExpandHeight = false;
            offers.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform tutorial = Panel("Tutorial Panel", safe, new Vector2(.02f, .415f), new Vector2(.98f, .485f), new Color32(236, 241, 218, 245));
            TMP_Text tutorialLabel = Text(tutorial, "Tutorial Step", "", 21, font, TextAlignmentOptions.Left, Vector2.zero, Vector2.one);
            RectTransform migration = Panel("Migration Notice", safe, new Vector2(.06f, .36f), new Vector2(.94f, .68f), new Color32(248, 244, 229, 255)); migration.gameObject.SetActive(false);
            TMP_Text migrationText = Text(migration, "Migration Summary", "", 24, font, TextAlignmentOptions.Left, new Vector2(.04f, .35f), new Vector2(.96f, .96f));
            Button migrationConfirm = Button(migration, "Принять и начать кампанию", font, 23); Position(migrationConfirm.GetComponent<RectTransform>(), new Vector2(.06f, .06f), new Vector2(.94f, .30f));

            MetaFactoryHudView hud = canvasGo.AddComponent<MetaFactoryHudView>();
            SerializedObject so = new SerializedObject(hud);
            Assign(so, "runtime", runtime); Assign(so, "safeAreaRoot", safe); Assign(so, "walletLabel", wallet); Assign(so, "activeOrderLabel", active); Assign(so, "statusLabel", status);
            Assign(so, "factoryPanel", factory); Assign(so, "warehousePanel", warehouse); Assign(so, "ordersPanel", orders);
            Assign(so, "factoryTabButton", factoryTab); Assign(so, "warehouseTabButton", warehouseTab); Assign(so, "ordersTabButton", ordersTab);
            Assign(so, "selectedStationTitle", selectedName); Assign(so, "selectedStationState", selectedState); Assign(so, "stationBuildLabel", buildLabel); Assign(so, "stationUpgradeLabel", upgradeLabel); Assign(so, "transferPreviewLabel", transferPreview);
            Assign(so, "buildButton", build); Assign(so, "upgradeButton", upgrade);
            Assign(so, "warehouseLabel", warehouseText); Assign(so, "offerRowsRoot", offers); Assign(so, "orderRowPrefab", rowPrefab); Assign(so, "activeOrderDetails", activeDetails); Assign(so, "turnInButton", turnIn); Assign(so, "cancelOrderButton", cancel); Assign(so, "cancelConfirmationPanel", cancelPanel.gameObject); Assign(so, "confirmCancelButton", confirmCancel); Assign(so, "dismissCancelButton", dismissCancel);
            Assign(so, "tutorialPanel", tutorial.gameObject); Assign(so, "tutorialLabel", tutorialLabel); Assign(so, "migrationPanel", migration.gameObject); Assign(so, "migrationLabel", migrationText); Assign(so, "migrationConfirmButton", migrationConfirm);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
            return hud;
        }

        private static void AuthorNearActions(RectTransform safeArea, Transform plotsRoot,
            MetaFactoryRuntime runtime, MetaFactoryHudView hud, Camera camera, TMP_FontAsset font)
        {
            MetaFactoryPlot[] plots = plotsRoot.GetComponentsInChildren<MetaFactoryPlot>(true);
            foreach (MetaFactoryPlot plot in plots)
            {
                string name = "M051 Near Actions · " + plot.Station;
                Transform existing = safeArea.Find(name);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                RectTransform panel = Rect(name, safeArea);
                panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
                panel.sizeDelta = new Vector2(340f, 450f);
                Image plate = panel.gameObject.AddComponent<Image>(); plate.color = new Color32(24, 47, 35, 228); plate.raycastTarget = true;

                TMP_Text warehouse = Text(panel, "Shared Warehouse Indicator", "Общий склад", 20, font, TextAlignmentOptions.Left, new Vector2(.04f, .76f), new Vector2(.96f, .98f));
                TMP_Text reason = Text(panel, "Action Disabled Reason", "", 18, font, TextAlignmentOptions.Left, new Vector2(.04f, .02f), new Vector2(.96f, .24f));
                MetaFactoryQuickActionButton load = QuickActionButton(panel, "+1", font, 22);
                Position(load.GetComponent<RectTransform>(), new Vector2(.06f, .46f), new Vector2(.94f, .74f));
                MetaFactoryQuickActionButton collect = QuickActionButton(panel, "Забрать", font, 22);
                Position(collect.GetComponent<RectTransform>(), new Vector2(.06f, .16f), new Vector2(.94f, .44f));
                MetaFactoryQuickActionView view = safeArea.gameObject.AddComponent<MetaFactoryQuickActionView>();
                SerializedObject quick = new SerializedObject(view);
                quick.FindProperty("station").enumValueIndex = (int)plot.Station;
                quick.FindProperty("runtime").objectReferenceValue = runtime;
                quick.FindProperty("hud").objectReferenceValue = hud;
                quick.FindProperty("worldCamera").objectReferenceValue = camera;
                quick.FindProperty("canvasRoot").objectReferenceValue = safeArea;
                quick.FindProperty("actionPanel").objectReferenceValue = panel;
                quick.FindProperty("warehouseAnchor").objectReferenceValue = panel;
                quick.FindProperty("buildingAnchor").objectReferenceValue = plot.transform;
                quick.FindProperty("loadButton").objectReferenceValue = load;
                quick.FindProperty("collectButton").objectReferenceValue = collect;
                quick.FindProperty("warehouseLabel").objectReferenceValue = warehouse;
                quick.FindProperty("reasonLabel").objectReferenceValue = reason;
                quick.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                warehouse.color = new Color32(245, 237, 207, 255);
                reason.color = new Color32(245, 237, 207, 255);
            }
        }

        private static void AuthorMotionView(GameObject canvas, RectTransform safeArea, Transform plotsRoot,
            MetaFactoryRuntime runtime, Camera camera)
        {
            MetaFactoryWorldMotionView motion = canvas.GetComponent<MetaFactoryWorldMotionView>();
            if (motion == null) motion = Undo.AddComponent<MetaFactoryWorldMotionView>(canvas);
            SerializedObject so = new SerializedObject(motion);
            so.FindProperty("runtime").objectReferenceValue = runtime;
            so.FindProperty("worldCamera").objectReferenceValue = camera;
            so.FindProperty("canvasRoot").objectReferenceValue = safeArea;
            so.FindProperty("plotsRoot").objectReferenceValue = plotsRoot;
            MetaFactoryQuickActionView[] quick = safeArea.GetComponents<MetaFactoryQuickActionView>();
            SerializedProperty array = so.FindProperty("quickActions");
            array.arraySize = quick.Length;
            for (int i = 0; i < quick.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = quick[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(motion);
        }

        private static MetaFactoryQuickActionButton QuickActionButton(Transform parent, string label, TMP_FontAsset font, float size)
        {
            RectTransform rect = Rect("Near Action · " + label, parent);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color32(47, 96, 37, 255); image.raycastTarget = true;
            MetaFactoryQuickActionButton button = rect.gameObject.AddComponent<MetaFactoryQuickActionButton>(); button.targetGraphic = image;
            button.colors = new ColorBlock { normalColor = image.color, highlightedColor = new Color32(76, 125, 61, 255), pressedColor = new Color32(34, 79, 32, 255), selectedColor = image.color, disabledColor = new Color32(62, 68, 54, 230), colorMultiplier = 1f, fadeDuration = .08f };
            TMP_Text text = Text(rect, "Label", label, size, font, TextAlignmentOptions.Center, new Vector2(.02f, .02f), new Vector2(.98f, .98f));
            text.color = Color.white;
            return button;
        }

        private static MetaOrderRowView EnsureOrderRowPrefab()
        {
            GameObject root = new GameObject("Order Offer Card", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            root.GetComponent<Image>().color = new Color32(255, 252, 241, 255);
            root.GetComponent<LayoutElement>().preferredHeight = 170;
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            TMP_Text title = Text(root.transform, "Offer Title", "Предложение", 22, font, TextAlignmentOptions.Left, Vector2.zero, Vector2.one);
            TMP_Text contents = Text(root.transform, "Offer Contents", "Свежий лист ×4 · выплата 8", 19, font, TextAlignmentOptions.Left, Vector2.zero, Vector2.one);
            Position((RectTransform)title.transform, new Vector2(.03f, .77f), new Vector2(.97f, .97f));
            Position((RectTransform)contents.transform, new Vector2(.03f, .39f), new Vector2(.97f, .75f));
            Button accept = Button(root.transform, "Выбрать заказ", font, 18);
            Button replace = Button(root.transform, "Бесплатно заменить", font, 18);
            Position(accept.GetComponent<RectTransform>(), new Vector2(.03f, .03f), new Vector2(.97f, .35f));
            Position(replace.GetComponent<RectTransform>(), new Vector2(.51f, .03f), new Vector2(.97f, .35f));
            MetaOrderRowView row = root.AddComponent<MetaOrderRowView>();
            SerializedObject so = new SerializedObject(row); Assign(so, "title", title); Assign(so, "contents", contents); Assign(so, "acceptButton", accept); Assign(so, "replaceButton", replace); so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<MetaOrderRowView>(RowPrefabPath);
        }

        [MenuItem("Tiny Factory/Reset M050 Editor Save Fixture")]
        private static void ResetEditorSaveFixture()
        {
            string directory = Path.Combine(Path.GetTempPath(), "tinyfactory-m050-play-save");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Debug.Log("M050 isolated Editor save fixture reset: " + directory);
        }

        private static void EnsureInputSystemEventSystem()
        {
            EventSystem system = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            GameObject go = system != null ? system.gameObject : new GameObject("EventSystem");
            if (system == null) { Undo.RegisterCreatedObjectUndo(go, "Create EventSystem"); Undo.AddComponent<EventSystem>(go); }
            Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule == null) { Debug.LogError("InputSystemUIInputModule is unavailable; verify the installed Input System package."); return; }
            if (go.GetComponent(inputModule) == null) Undo.AddComponent(go, inputModule);
            foreach (BaseInputModule module in go.GetComponents<BaseInputModule>())
                if (module.GetType() != inputModule) module.enabled = false;
        }

        private static GameObject EnsureRoot(string name)
        {
            GameObject root = GameObject.Find(name);
            if (root != null) return root;
            root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
            return root;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = Rect(name, parent); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = true; return rect;
        }
        private static TMP_Text Text(Transform parent, string name, string value, float size, TMP_FontAsset font, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            RectTransform rect = Rect(name, parent); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>(); if (font != null) text.font = font;
            text.text = value; text.fontSize = size; text.color = new Color32(46, 44, 37, 255); text.alignment = alignment; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        private static Button Button(Transform parent, string label, TMP_FontAsset font, float size)
        {
            RectTransform rect = Rect("Button · " + label, parent); Image image = rect.gameObject.AddComponent<Image>(); image.color = new Color32(87, 124, 75, 255);
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.colors = new ColorBlock { normalColor = image.color, highlightedColor = new Color32(105, 143, 91, 255), pressedColor = new Color32(65, 100, 57, 255), selectedColor = image.color, disabledColor = new Color32(150, 147, 133, 255), colorMultiplier = 1f, fadeDuration = .1f };
            TMP_Text text = Text(rect, "Label", label, size, font, TextAlignmentOptions.Center, new Vector2(.03f, .03f), new Vector2(.97f, .97f)); text.color = Color.white;
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>(); layout.minHeight = 72; layout.preferredHeight = 78; layout.flexibleWidth = 1;
            return button;
        }
        private static void Position(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private static void Assign(SerializedObject so, string name, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null) throw new MissingFieldException(so.targetObject.GetType().Name, name);
            property.objectReferenceValue = value;
        }
    }
}
#endif
