using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory
{
    /// <summary>Shows committed item transfers and live station work without owning gameplay state.</summary>
    public sealed class MetaFactoryWorldMotionView : MonoBehaviour
    {
        [SerializeField] private MetaFactoryRuntime runtime;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform canvasRoot;
        [SerializeField] private Transform plotsRoot;
        [SerializeField] private MetaFactoryQuickActionView[] quickActions = new MetaFactoryQuickActionView[3];
        [SerializeField, Min(.05f)] private float transferDuration = .62f;
        [SerializeField, Min(.005f)] private float workBobHeight = .045f;
        [SerializeField, Min(.1f)] private float workBobDuration = .75f;

        private MetaFactoryPlot[] plots;
        private Tween[] workTweens = new Tween[3];
        private Tween[] outputPulses = new Tween[3];
        private Vector3[] basePositions = new Vector3[3];
        private Vector3[] baseScales = new Vector3[3];
        private int[] previousOutputs = new int[3];
        private bool[] working = new bool[3];
        private RectTransform activeToken;
        private Tween activeFlight;
        private Tween activeFade;
        private bool tweeningEnabled;

        private void OnEnable()
        {
            if (runtime == null || worldCamera == null || canvasRoot == null || plotsRoot == null) return;
            plots = plotsRoot.GetComponentsInChildren<MetaFactoryPlot>(true);
            for (int i = 0; i < plots.Length && i < 3; i++)
            {
                Transform visual = plots[i].StationVisual;
                basePositions[i] = visual.localPosition;
                baseScales[i] = visual.localScale;
                MetaFactoryRuntime.StationState state = runtime.Station(plots[i].Station);
                previousOutputs[i] = state != null ? state.output : 0;
            }
            runtime.Changed += OnStateChanged;
            runtime.TimerChanged += OnTimerChanged;
            runtime.TransferCommitted += OnTransferCommitted;
            tweeningEnabled = runtime.IsForeground;
            UpdateWorkAnimations();
        }

        private void OnDisable()
        {
            if (runtime != null)
            {
                runtime.Changed -= OnStateChanged;
                runtime.TimerChanged -= OnTimerChanged;
                runtime.TransferCommitted -= OnTransferCommitted;
            }
            StopAllVisuals();
        }

        private void OnDestroy() { StopAllVisuals(); }

        private void OnApplicationPause(bool paused) { if (paused) KillFlight(); }
        private void OnApplicationFocus(bool focused) { if (!focused) KillFlight(); }

        private void Update()
        {
            if (runtime == null) return;
            if (tweeningEnabled != runtime.IsForeground) SetTweeningEnabled(runtime.IsForeground);
        }

        private void OnStateChanged()
        {
            for (int i = 0; i < plots.Length && i < 3; i++)
            {
                MetaFactoryRuntime.StationState state = runtime.Station(plots[i].Station);
                if (state == null) continue;
                if (state.output > previousOutputs[i]) PulseStation(i);
                previousOutputs[i] = state.output;
            }
            UpdateWorkAnimations();
        }

        private void OnTimerChanged() => UpdateWorkAnimations();

        private void UpdateWorkAnimations()
        {
            if (plots == null) return;
            for (int i = 0; i < plots.Length && i < 3; i++)
            {
                MetaFactoryRuntime.StationState state = runtime.Station(plots[i].Station);
                Transform visual = plots[i].StationVisual;
                bool isWorking = state != null && state.built &&
                    (plots[i].Station == MetaStation.Garden ? state.gardenRemainingSeconds > 0 : state.remainingSeconds > 0);
                if (working[i] == isWorking) continue;
                working[i] = isWorking;
                Kill(ref workTweens[i]);
                visual.localPosition = basePositions[i];
                if (!isWorking) continue;
                workTweens[i] = visual.DOLocalMoveY(basePositions[i].y + workBobHeight, workBobDuration)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                if (!tweeningEnabled) workTweens[i].Pause();
            }
        }

        private void PulseStation(int index)
        {
            Transform visual = plots[index].StationVisual;
            Kill(ref outputPulses[index]);
            visual.localScale = baseScales[index];
            outputPulses[index] = visual.DOPunchScale(Vector3.one * .07f, .28f, 3, .5f).SetUpdate(true);
            if (!tweeningEnabled) outputPulses[index].Pause();
        }

        private void OnTransferCommitted(MetaStation station, bool loading, int quantity)
        {
            MetaFactoryQuickActionView quick = FindQuickAction(station);
            MetaFactoryPlot plot = FindPlot(station);
            if (quick == null || plot == null || quick.WarehouseAnchor == null) return;
            Vector2 from;
            Vector2 to;
            if (loading)
            {
                from = RectScreenPoint(quick.WarehouseAnchor);
                to = WorldScreenPoint(plot.InputAnchor);
            }
            else
            {
                from = WorldScreenPoint(plot.OutputAnchor);
                to = RectScreenPoint(quick.WarehouseAnchor);
            }
            if (float.IsNaN(from.x) || float.IsNaN(to.x)) return;
            quick.PulseDepot();
            int plotIndex = System.Array.IndexOf(plots, plot);
            if (plotIndex >= 0 && plotIndex < plots.Length && plotIndex < 3) PulseStation(plotIndex);
            StartTransferToken(from, to, station, loading, quantity);
        }

        private void StartTransferToken(Vector2 from, Vector2 to, MetaStation station, bool loading, int quantity)
        {
            KillFlight();
            activeToken = new GameObject("Committed Transfer Token", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            activeToken.SetParent(canvasRoot, false);
            activeToken.sizeDelta = new Vector2(218f, 56f);
            Image background = activeToken.GetComponent<Image>();
            background.color = new Color32(24, 47, 35, 245);
            background.raycastTarget = false;
            TMP_Text label = new GameObject("Product Count", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(activeToken, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            label.text = MetaFactoryHudView.ProductName(loading ? InputProduct(station) : OutputProduct(station)) + " ×" + quantity;
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 19f; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, from, null, out Vector2 fromLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, to, null, out Vector2 toLocal);
            activeToken.anchoredPosition = fromLocal;
            CanvasGroup group = activeToken.gameObject.AddComponent<CanvasGroup>();
            activeFlight = activeToken.DOAnchorPos(toLocal, transferDuration).SetEase(Ease.InOutCubic).SetUpdate(true);
            activeFlight.OnComplete(() =>
            {
                if (activeToken != null)
                {
                    activeFade = group.DOFade(0f, .12f).SetUpdate(true);
                    activeFade.OnComplete(KillFlight);
                }
                else KillFlight();
            });
            if (!tweeningEnabled) activeFlight.Pause();
        }

        private MetaFactoryQuickActionView FindQuickAction(MetaStation station)
        {
            if (quickActions == null) return null;
            for (int i = 0; i < quickActions.Length; i++)
                if (quickActions[i] != null && quickActions[i].Station == station && quickActions[i].gameObject.activeInHierarchy) return quickActions[i];
            return null;
        }

        private MetaFactoryPlot FindPlot(MetaStation station)
        {
            if (plots == null) return null;
            for (int i = 0; i < plots.Length; i++) if (plots[i] != null && plots[i].Station == station) return plots[i];
            return null;
        }

        private Vector2 WorldScreenPoint(Vector3 position)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(position);
            return screen.z > 0f ? new Vector2(screen.x, screen.y) : new Vector2(float.NaN, float.NaN);
        }

        private static Vector2 RectScreenPoint(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.position);
        private static MetaProduct InputProduct(MetaStation station) => station == MetaStation.Dryer ? MetaProduct.FreshLeaf : MetaProduct.DryTea;
        private static MetaProduct OutputProduct(MetaStation station) => station == MetaStation.Garden ? MetaProduct.FreshLeaf : station == MetaStation.Dryer ? MetaProduct.DryTea : MetaProduct.TeaPacket;

        private void SetTweeningEnabled(bool enabledState)
        {
            tweeningEnabled = enabledState;
            for (int i = 0; i < 3; i++)
            {
                SetTween(workTweens[i], enabledState);
                SetTween(outputPulses[i], enabledState);
            }
            SetTween(activeFlight, enabledState);
        }

        private void StopAllVisuals()
        {
            for (int i = 0; i < 3; i++)
            {
                Kill(ref workTweens[i]);
                Kill(ref outputPulses[i]);
                if (plots != null && i < plots.Length && plots[i] != null)
                {
                    plots[i].StationVisual.localPosition = basePositions[i];
                    plots[i].StationVisual.localScale = baseScales[i];
                }
            }
            KillFlight();
        }

        private void KillFlight()
        {
            Kill(ref activeFlight);
            Kill(ref activeFade);
            if (activeToken != null)
            {
                if (Application.isPlaying) Destroy(activeToken.gameObject);
                else DestroyImmediate(activeToken.gameObject);
            }
            activeToken = null;
        }

        private static void SetTween(Tween tween, bool enabled)
        {
            if (tween == null || !tween.IsActive()) return;
            if (enabled) tween.Play(); else tween.Pause();
        }

        private static void Kill(ref Tween tween)
        {
            if (tween == null) return;
            if (tween.IsActive()) tween.Kill();
            tween = null;
        }
    }
}
