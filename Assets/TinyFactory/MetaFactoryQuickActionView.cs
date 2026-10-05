using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory
{
    /// <summary>Projects concise, building-local transfer controls into the authored overlay canvas.</summary>
    public sealed class MetaFactoryQuickActionView : MonoBehaviour
    {
        [SerializeField] private MetaStation station;
        [SerializeField] private MetaFactoryRuntime runtime;
        [SerializeField] private MetaFactoryHudView hud;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform canvasRoot;
        [SerializeField] private RectTransform actionPanel;
        [SerializeField] private RectTransform warehouseAnchor;
        [SerializeField] private Transform buildingAnchor;
        [SerializeField] private MetaFactoryQuickActionButton loadButton;
        [SerializeField] private MetaFactoryQuickActionButton collectButton;
        [SerializeField] private TMP_Text warehouseLabel;
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField, Min(0f)] private float sideGapDp = 10f;
        [SerializeField] private float viewportBottomFraction = .49f;
        [SerializeField] private float viewportTopFraction = .83f;

        private bool bound;
        private Tween depotPulse;
        private Vector3 basePanelScale = Vector3.one;
        private Renderer[] buildingRenderers;

        public MetaStation Station => station;
        public RectTransform WarehouseAnchor => warehouseAnchor;

        private void OnEnable()
        {
            if (actionPanel != null) basePanelScale = actionPanel.localScale;
            if (buildingAnchor != null) buildingRenderers = buildingAnchor.GetComponentsInChildren<Renderer>(true);
            if (runtime != null)
            {
                runtime.Changed += Refresh;
                runtime.TimerChanged += Refresh;
                bound = true;
            }
            if (loadButton != null) loadButton.onClick.AddListener(LoadOne);
            if (collectButton != null) collectButton.onClick.AddListener(CollectAvailable);
            Refresh();
        }

        private void OnDisable()
        {
            if (bound && runtime != null)
            {
                runtime.Changed -= Refresh;
                runtime.TimerChanged -= Refresh;
                bound = false;
            }
            if (loadButton != null) loadButton.onClick.RemoveListener(LoadOne);
            if (collectButton != null) collectButton.onClick.RemoveListener(CollectAvailable);
            if (depotPulse != null && depotPulse.IsActive()) depotPulse.Kill();
            depotPulse = null;
            if (actionPanel != null) actionPanel.localScale = basePanelScale;
        }

        public void PulseDepot()
        {
            if (actionPanel == null) return;
            if (depotPulse != null && depotPulse.IsActive()) depotPulse.Kill();
            actionPanel.localScale = basePanelScale;
            depotPulse = actionPanel.DOPunchScale(Vector3.one * .035f, .22f, 2, .4f).SetUpdate(true);
        }

        private void LateUpdate()
        {
            if (actionPanel == null || worldCamera == null || canvasRoot == null || buildingAnchor == null) return;
            Vector3 screen = worldCamera.WorldToScreenPoint(buildingAnchor.position);
            bool visible = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
                           screen.y >= Screen.height * viewportBottomFraction && screen.y <= Screen.height * viewportTopFraction;
            actionPanel.gameObject.SetActive(visible);
            if (!visible) return;
            GetProjectedBounds(out float left, out float right, out float bottomEdge, out float topEdge);
            float halfWidth = actionPanel.rect.width * canvasRoot.lossyScale.x * .5f;
            float halfHeight = actionPanel.rect.height * canvasRoot.lossyScale.y * .5f;
            float gap = DpToPixels(sideGapDp);
            float margin = DpToPixels(10f);
            float rightCenter = right + gap + halfWidth;
            float leftCenter = left - gap - halfWidth;
            float minCenterX = halfWidth + margin;
            float maxCenterX = Screen.width - halfWidth - margin;
            float centerX = rightCenter <= maxCenterX ? rightCenter :
                leftCenter >= minCenterX ? leftCenter : Mathf.Clamp(rightCenter, minCenterX, maxCenterX);
            float centerY = (bottomEdge + topEdge) * .5f;
            float minY = Screen.height * viewportBottomFraction + halfHeight;
            float maxY = Screen.height * viewportTopFraction - halfHeight;
            centerY = minY > maxY ? (minY + maxY) * .5f : Mathf.Clamp(centerY, minY, maxY);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, new Vector2(centerX, centerY), null, out Vector2 point);
            actionPanel.anchoredPosition = point;
        }

        private void GetProjectedBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;
            bool found = false;
            if (buildingRenderers != null)
            {
                foreach (Renderer renderer in buildingRenderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    Bounds bounds = renderer.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 world = new Vector3(
                            (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                            (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                            (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                        Vector3 projected = worldCamera.WorldToScreenPoint(world);
                        if (projected.z <= 0f) continue;
                        minX = Mathf.Min(minX, projected.x); maxX = Mathf.Max(maxX, projected.x);
                        minY = Mathf.Min(minY, projected.y); maxY = Mathf.Max(maxY, projected.y);
                        found = true;
                    }
                }
            }
            if (found) return;
            Vector3 anchor = worldCamera.WorldToScreenPoint(buildingAnchor.position);
            float fallbackRadius = DpToPixels(40f);
            minX = anchor.x - fallbackRadius; maxX = anchor.x + fallbackRadius;
            minY = anchor.y - fallbackRadius; maxY = anchor.y + fallbackRadius;
        }

        private static float DpToPixels(float value) => value * (Screen.dpi > 0f ? Screen.dpi / 160f : Mathf.Max(.5f, Screen.height / 1920f));

        private void Refresh()
        {
            if (runtime == null || loadButton == null || collectButton == null) return;
            MetaFactoryRuntime.StationState state = runtime.Station(station);
            if (state == null)
            {
                SetButton(loadButton, "+1", false, "Данные загружаются");
                SetButton(collectButton, "Забрать", false, "Данные загружаются");
                if (warehouseLabel != null) warehouseLabel.text = "Общий склад · загрузка";
                if (reasonLabel != null) reasonLabel.text = "Данные загружаются";
                return;
            }
            MetaProduct input = station == MetaStation.Dryer ? MetaProduct.FreshLeaf : MetaProduct.DryTea;
            MetaProduct output = station == MetaStation.Garden ? MetaProduct.FreshLeaf : station == MetaStation.Dryer ? MetaProduct.DryTea : MetaProduct.TeaPacket;
            int loadMax = runtime.MaxLoad(station);
            int collectMax = runtime.MaxCollect(station);
            loadButton.gameObject.SetActive(station != MetaStation.Garden);
            if (station != MetaStation.Garden)
            {
                string loadReason = !state.built ? "Здание закрыто" : state.input >= runtime.Config.localCapacity ? "Вход заполнен" :
                    runtime.Warehouse(input) <= 0 ? "Нет " + MetaFactoryHudView.ProductName(input) + " на складе" : string.Empty;
                SetButton(loadButton, "+1", state.built && loadMax > 0, loadReason);
            }
            string collectReason = !state.built ? "Здание закрыто" : state.output <= 0 ? "Нет готового выпуска" :
                collectMax <= 0 ? "Нет места на складе" : string.Empty;
            SetButton(collectButton, collectMax > 0 ? "Забрать " + collectMax : "Забрать", state.built && collectMax > 0, collectReason);
            if (warehouseLabel != null)
                warehouseLabel.text = station == MetaStation.Garden ? "Склад · " + MetaFactoryHudView.ProductName(output) + " " + runtime.Warehouse(output) :
                    "Склад · вход " + MetaFactoryHudView.ProductName(input) + " " + runtime.Warehouse(input) +
                    " · выход " + MetaFactoryHudView.ProductName(output) + " " + runtime.Warehouse(output);
            if (warehouseLabel != null) warehouseLabel.color = new Color32(245, 237, 207, 255);
            if (reasonLabel != null)
            {
                string firstReason = station != MetaStation.Garden && state.reservedInput == 0 && station == MetaStation.Dryer &&
                    state.input > 0 && state.input % runtime.Config.freshLeafPerBatch != 0 ? "Рецепт " + state.input % runtime.Config.freshLeafPerBatch + "/" + runtime.Config.freshLeafPerBatch + " · нужен ещё 1 " + MetaFactoryHudView.ProductName(input) :
                    station != MetaStation.Garden && !loadButton.interactable ? LoadReason(state, input) : string.Empty;
                string secondReason = !collectButton.interactable ? CollectReason(state, collectMax) : string.Empty;
                reasonLabel.text = firstReason.Length > 0 ? firstReason : secondReason;
                reasonLabel.color = new Color32(245, 237, 207, 255);
            }
        }

        private void LoadOne()
        {
            if (runtime.Load(station, 1)) hud.ReportActionResult("+1 " + MetaFactoryHudView.ProductName(station == MetaStation.Dryer ? MetaProduct.FreshLeaf : MetaProduct.DryTea) + " → вход");
            else { Refresh(); hud.ReportActionResult("Загрузка не выполнена · проверьте запас и свободное место"); }
            Refresh();
        }

        private void CollectAvailable()
        {
            int quantity = runtime.MaxCollect(station);
            if (quantity > 0 && runtime.Collect(station, quantity)) hud.ReportActionResult("Забрано " + quantity + " → общий склад");
            else { Refresh(); hud.ReportActionResult("Сбор не выполнен · выход или место на складе изменились"); }
            Refresh();
        }

        private string LoadReason(MetaFactoryRuntime.StationState state, MetaProduct input) =>
            !state.built ? "Здание закрыто" : state.input >= runtime.Config.localCapacity ? "Вход заполнен" : "Нет " + MetaFactoryHudView.ProductName(input) + " на складе";

        private string CollectReason(MetaFactoryRuntime.StationState state, int collectMax) =>
            !state.built ? "Здание закрыто" : state.output <= 0 ? "Нет готового выпуска" : collectMax <= 0 ? "Нет места на складе" : string.Empty;

        private void SetButton(MetaFactoryQuickActionButton button, string label, bool enabled, string reason)
        {
            button.interactable = enabled;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
            Image image = button.targetGraphic as Image;
            if (image != null) image.color = enabled ? new Color32(42, 91, 39, 245) : new Color32(62, 68, 54, 230);
            if (reasonLabel != null && reason.Length > 0) reasonLabel.text = reason;
        }
    }

}
