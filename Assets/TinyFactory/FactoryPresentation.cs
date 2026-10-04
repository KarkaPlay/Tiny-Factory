using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TinyFactory
{
    public sealed class FactoryPresentation : MonoBehaviour
    {
        private FactoryRuntime runtime;
        private Text status;
        private Text lineStatus;
        private Text speedLabel;
        private Button harvestButton;
        private Button speedButton;
        private Transform dryerLeaf;
        private Transform packagerLeaf;
        private Transform worldRoot;
        private RectTransform safeAreaRoot;
        private int lastRejectedHarvests;

        private void Start()
        {
            runtime = GetComponent<FactoryRuntime>();
            BuildFactoryVisuals();
            BuildHud();
            runtime.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (runtime != null) runtime.Changed -= Refresh;
        }

        private void Update()
        {
            if (runtime == null || status == null) return;
            Rect safe = Screen.safeArea;
            safeAreaRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero;
            string state = $"Лист {runtime.SourceBuffer}/{runtime.Capacity}   Сушилка {(runtime.DryerHasWip ? runtime.DryerRemaining + "с" : "готова")}   Упаковка {(runtime.PackagerHasWip ? runtime.PackagerRemaining + "с" : "готова")}   Авто {runtime.SecondsUntilAuto}с";
            lineStatus.text = runtime.SourceBuffer + runtime.Config.manualBatch > runtime.Capacity
                ? "ЛЕНТА ЗАПОЛНЕНА · сбор временно недоступен\n" + state
                : "Куст → сушилка → упаковка\n" + state;
            if (runtime.RejectedHarvests > lastRejectedHarvests)
            {
                lineStatus.text = "ЛЕНТА ЗАПОЛНЕНА · сбор пропущен\n" + lineStatus.text;
                lastRejectedHarvests = runtime.RejectedHarvests;
            }
            dryerLeaf.gameObject.SetActive(runtime.DryerHasWip);
            packagerLeaf.gameObject.SetActive(runtime.PackagerHasWip);
            if (runtime.DryerHasWip)
            {
                float duration = runtime.Config.DryerDuration(runtime.SpeedLevel);
                float progress = 1f - runtime.DryerRemaining / duration;
                dryerLeaf.localPosition = Vector3.Lerp(new Vector3(-3.2f, 0.72f, -0.3f), new Vector3(-0.7f, 1.55f, -0.62f), progress);
            }
            if (runtime.PackagerHasWip)
            {
                float duration = runtime.Config.PackagerDuration(runtime.SpeedLevel);
                float progress = 1f - runtime.PackagerRemaining / duration;
                packagerLeaf.localPosition = Vector3.Lerp(new Vector3(0.2f, 0.7f, -0.4f), new Vector3(3f, 1.28f, -0.58f), progress);
            }
        }

        private void Refresh()
        {
            if (runtime == null || status == null) return;
            status.text = $"● {runtime.Coins} монет                         Продано {runtime.Sold}";
            speedLabel.text = runtime.SpeedLevel == 0
                ? $"СКОРОСТЬ  0/1   Сушилка 3с → 2с   Цена {runtime.Config.speedLevelOneCost}"
                : "СКОРОСТЬ  1/1   Сушилка 2с   MAX";
            speedButton.interactable = runtime.SpeedLevel == 0 && runtime.Coins >= runtime.Config.speedLevelOneCost;
            harvestButton.interactable = runtime.IsForeground && runtime.SourceBuffer + runtime.Config.manualBatch <= runtime.Capacity;
            speedButton.interactable = runtime.IsForeground && runtime.SpeedLevel == 0 && runtime.Coins >= runtime.Config.speedLevelOneCost;
        }

        private void BuildFactoryVisuals()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 6.4f;
                camera.transform.position = new Vector3(0f, 7f, -12f);
                camera.transform.LookAt(new Vector3(0f, 0.8f, 0f));
            }

            var world = new GameObject("Factory line visuals");
            world.transform.SetParent(transform, false);
            world.transform.localScale = new Vector3(0.65f, 1f, 1f);
            worldRoot = world.transform;

            MakePrimitive("Чайный куст", PrimitiveType.Sphere, new Vector3(-4f, 1.0f, 0f), new Vector3(1.45f, 1.7f, 1.1f), new Color(0.28f, 0.55f, 0.25f));
            MakePrimitive("Ствол", PrimitiveType.Cylinder, new Vector3(-4f, 0.25f, 0f), new Vector3(0.28f, 0.65f, 0.28f), new Color(0.38f, 0.22f, 0.12f));
            MakePrimitive("Лента", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(7.6f, 0.12f, 0.8f), new Color(0.36f, 0.32f, 0.25f));
            MakePrimitive("Сушилка", PrimitiveType.Cube, new Vector3(-0.7f, 0.9f, 0f), new Vector3(1.8f, 1.2f, 1.15f), new Color(0.82f, 0.48f, 0.2f));
            MakePrimitive("Касса и упаковка", PrimitiveType.Cube, new Vector3(3f, 0.75f, 0f), new Vector3(1.6f, 0.9f, 1.1f), new Color(0.2f, 0.55f, 0.62f));
            MakePrimitive("Стол", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(10f, 0.25f, 2f), new Color(0.72f, 0.65f, 0.47f));

            dryerLeaf = MakePrimitive("Лист в сушилке", PrimitiveType.Sphere, new Vector3(-0.7f, 1.65f, -0.62f), Vector3.one * 0.32f, new Color(0.2f, 0.8f, 0.25f)).transform;
            packagerLeaf = MakePrimitive("Пакетик на кассе", PrimitiveType.Cube, new Vector3(3f, 1.28f, -0.58f), Vector3.one * 0.36f, new Color(0.9f, 0.83f, 0.56f)).transform;
            dryerLeaf.gameObject.SetActive(false);
            packagerLeaf.gameObject.SetActive(false);
            MakePrimitive("Земля", PrimitiveType.Cube, new Vector3(0f, -0.58f, 0f), new Vector3(200f, 0.3f, 200f), new Color(0.25f, 0.37f, 0.24f));
        }

        private GameObject MakePrimitive(string label, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = label;
            go.transform.SetParent(worldRoot, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = go.GetComponent<Renderer>();
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
            return go;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("M03 HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = new GameObject("Safe Area Root", typeof(RectTransform));
            safe.transform.SetParent(canvasObject.transform, false);
            safeAreaRoot = safe.GetComponent<RectTransform>();
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                eventSystem.transform.SetParent(transform, false);
            }

            status = CreateText(safeAreaRoot, "Wallet", new Vector2(0.5f, 0.92f), new Vector2(0.9f, 0.07f), 34, TextAnchor.MiddleCenter, Color.white);
            lineStatus = CreateText(safeAreaRoot, "Queue status", new Vector2(0.5f, 0.72f), new Vector2(0.95f, 0.14f), 24, TextAnchor.MiddleCenter, Color.white);

            harvestButton = CreateButton(safeAreaRoot, "СОБРАТЬ ЛИСТ", new Vector2(0.5f, 0.23f), new Vector2(0.82f, 0.09f), 36);
            harvestButton.onClick.AddListener(() => runtime.RequestHarvest());
            speedButton = CreateButton(safeAreaRoot, "СКОРОСТЬ", new Vector2(0.5f, 0.12f), new Vector2(0.82f, 0.08f), 25);
            speedButton.onClick.AddListener(() => runtime.RequestSpeedUpgrade());
            speedLabel = speedButton.GetComponentInChildren<Text>();
        }

        private Text CreateText(Transform parent, string label, Vector2 anchor, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1080f * size.x, 1920f * size.y);
            rect.anchoredPosition = Vector2.zero;
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchor, Vector2 size, int fontSize)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1080f * size.x, 1920f * size.y);
            rect.anchoredPosition = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.44f, 0.25f, 0.96f);
            Button button = go.GetComponent<Button>();
            var text = CreateText(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.one, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
