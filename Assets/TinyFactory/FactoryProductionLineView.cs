using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory
{
    public sealed class FactoryProductionLineView : MonoBehaviour
    {
        private sealed class VisualItem
        {
            public FactoryItemVisual view;
            public Tween movement;
            public int stage = -1;
            public int slot = -1;
        }

        private const int VisualItemLimit = 27;

        [Header("Runtime and item prefab")]
        [SerializeField] private FactoryRuntime runtime;
        [SerializeField] private FactoryItemVisual itemPrefab;
        
        [SerializeField] private Transform harvestPulseTarget;
        [SerializeField] private Transform worldRoot;

        [Header("Editable line anchors")]
        [SerializeField] private Transform[] sourceQueueSlots = new Transform[8];
        [SerializeField] private Transform dryerWipAnchor;
        [SerializeField] private Transform[] rollerQueueSlots = new Transform[8];
        [SerializeField] private Transform rollerWipAnchor;
        [SerializeField] private Transform[] packagerQueueSlots = new Transform[8];
        [SerializeField] private Transform packagerWipAnchor;
        [Header("World unlock presentation")]
        [SerializeField] private GameObject rollerLockedPlaceholder;

        [Header("Processing feedback")]
        [SerializeField] private RectTransform dryerProgressFill;
        [SerializeField] private Image dryerProgressImage;
        [SerializeField] private RectTransform rollerProgressFill;
        [SerializeField] private Image rollerProgressImage;
        [SerializeField] private RectTransform packagerProgressFill;
        [SerializeField] private Image packagerProgressImage;
        [SerializeField, Min(0.05f)] private float transportDuration = 0.24f;

        private readonly List<VisualItem> visualItems = new List<VisualItem>(VisualItemLimit);
        private Tween dryerBarTween;
        private Tween rollerBarTween;
        private Tween packagerBarTween;
        private Tween harvestPulse;
        private Vector3 harvestBaseScale;
        
        private bool harvestPulseCaptured;

        private int dryerDurationAtStart;
        private int packagerDurationAtStart;
        private int lastDryerRemaining = -1;
        private int lastDryerDuration = -1;
        private int lastPackagerRemaining = -1;
        private int lastPackagerDuration = -1;
        private int rollerDurationAtStart;
        private int lastRollerRemaining = -1;
        private int lastRollerDuration = -1;
        private bool lastRollerActive;
        private bool lastDryerActive;
        private bool lastPackagerActive;
        private bool tweeningEnabled;
        private bool subscribed;

        public int VisualCount { get { return visualItems.Count; } }

        private void OnEnable()
        {
            if (runtime == null || itemPrefab == null || worldRoot == null)
            {
                Debug.LogError("FactoryProductionLineView requires runtime, item prefab, and world root references.", this);
                enabled = false;
                return;
            }
            if (sourceQueueSlots == null || sourceQueueSlots.Length < runtime.Capacity ||
                rollerQueueSlots == null || rollerQueueSlots.Length < runtime.Capacity ||
                packagerQueueSlots == null || packagerQueueSlots.Length < runtime.Capacity || rollerWipAnchor == null)
            {
                Debug.LogError("Production line queue anchors must cover the configured buffer capacity.", this);
                enabled = false;
                return;
            }
            if (!subscribed)
            {
                runtime.ItemAccepted += OnItemAccepted;
                runtime.SaleCommitted += OnSaleCommitted;
                runtime.Changed += Refresh;
                subscribed = true;
            }
            if (harvestPulseTarget != null && !harvestPulseCaptured)
            {
                harvestBaseScale = harvestPulseTarget.localScale;
                harvestPulseCaptured = true;
            }
            tweeningEnabled = runtime.IsForeground;
            ReconcileVisuals();
            RefreshProgress();
            SetTweeningEnabled(tweeningEnabled);
        }

        private void OnDisable()
        {
            SetTweeningEnabled(false);
            Kill(ref harvestPulse);
            if (harvestPulseTarget != null && harvestPulseCaptured)
                harvestPulseTarget.localScale = harvestBaseScale;
            if (subscribed && runtime != null)
            {
                runtime.ItemAccepted -= OnItemAccepted;
                runtime.SaleCommitted -= OnSaleCommitted;
                runtime.Changed -= Refresh;
                subscribed = false;
            }
        }

        private void OnDestroy()
        {
            Kill(ref dryerBarTween);
            Kill(ref rollerBarTween);
            Kill(ref packagerBarTween);
            Kill(ref harvestPulse);
            for (int i = visualItems.Count - 1; i >= 0; i--)
            {
                VisualItem item = visualItems[i];
                Kill(ref item.movement);
                if (item.view != null) Destroy(item.view.gameObject);
            }
            visualItems.Clear();
        }

        private void Update()
        {
            if (runtime == null) return;
            if (tweeningEnabled != runtime.IsForeground)
                SetTweeningEnabled(runtime.IsForeground);
        }

        private void OnItemAccepted(bool automatic)
        {
            if (visualItems.Count >= VisualItemLimit)
            {
                Debug.LogError("Visual item limit exceeded despite runtime capacity.", this);
                return;
            }

            FactoryItemVisual instance = Instantiate(itemPrefab, worldRoot, false);
            instance.name = "Лист · очередь";
            instance.SetPackaged(false);
            visualItems.Add(new VisualItem { view = instance });
            if (!automatic && harvestPulseTarget != null && harvestPulseCaptured)
            {
                Kill(ref harvestPulse);
                harvestPulseTarget.localScale = harvestBaseScale;
                harvestPulse = harvestPulseTarget.DOScale(harvestBaseScale * 1.16f, 0.18f)
                    .SetLoops(2, LoopType.Yoyo).SetUpdate(true);
                if (!tweeningEnabled) harvestPulse.Pause();
            }
            ReconcileVisuals();
        }

        private void OnSaleCommitted(long amount)
        {
            if (visualItems.Count == 0)
            {
                Debug.LogError("Runtime committed a sale with no corresponding visual item.", this);
                return;
            }

            VisualItem soldItem = visualItems[0];
            visualItems.RemoveAt(0);
            Kill(ref soldItem.movement);
            if (soldItem.view != null) Destroy(soldItem.view.gameObject);
            ReconcileVisuals();
        }

        private void Refresh()
        {
            ReconcileVisuals();
            RefreshProgress();
        }

        private void ReconcileVisuals()
        {
            if (runtime == null) return;
            int expected = runtime.TotalInFlight;
            if (expected > VisualItemLimit)
            {
                Debug.LogError("Runtime exceeded the configured visual item bound.", this);
                return;
            }
            while (visualItems.Count < expected)
            {
                FactoryItemVisual instance = Instantiate(itemPrefab, worldRoot, false);
                instance.name = "Лист · очередь";
                instance.SetPackaged(false);
                visualItems.Add(new VisualItem { view = instance });
            }
            while (visualItems.Count > expected)
            {
                int last = visualItems.Count - 1;
                VisualItem extra = visualItems[last];
                visualItems.RemoveAt(last);
                Kill(ref extra.movement);
                if (extra.view != null) Destroy(extra.view.gameObject);
            }

            // The locked marker occupies the future roller footprint. The HUD owns
            // activation of the actual station; this view owns the locked state.
            if (rollerLockedPlaceholder != null)
                rollerLockedPlaceholder.SetActive(!runtime.RollerUnlocked);

            int index = 0;
            // Visual items are kept in FIFO order from downstream to upstream.
            // Existing final-stage items therefore retain their final anchors when
            // the roller unlock inserts a new stage into the route.
            if (runtime.PackagerHasWip && index < visualItems.Count)
                SetItemStage(visualItems[index++], 5, 0);
            for (int i = 0; i < runtime.PackagerInput && index < visualItems.Count; i++)
                SetItemStage(visualItems[index++], 4, i);
            if (runtime.RollerHasWip && index < visualItems.Count)
                SetItemStage(visualItems[index++], 3, 0);
            for (int i = 0; i < runtime.RollerInput && index < visualItems.Count; i++)
                SetItemStage(visualItems[index++], 2, i);
            if (runtime.DryerHasWip && index < visualItems.Count)
                SetItemStage(visualItems[index++], 1, 0);
            for (int i = 0; i < runtime.SourceBuffer && index < visualItems.Count; i++)
                SetItemStage(visualItems[index++], 0, i);
        }

        private void SetItemStage(VisualItem item, int stage, int slot)
        {
            if (item.stage == stage && item.slot == slot) return;
            int previousStage = item.stage;
            item.stage = stage;
            item.slot = slot;
            if (stage == 1 && previousStage != 1) dryerDurationAtStart = Mathf.Max(1, runtime.DryerRemaining);
            if (stage == 3 && previousStage != 3) rollerDurationAtStart = Mathf.Max(1, runtime.RollerRemaining);
            if (stage == 5 && previousStage != 5) packagerDurationAtStart = Mathf.Max(1, runtime.PackagerRemaining);
            if (item.view == null) return;

            Transform destination;
            if (stage == 0) destination = sourceQueueSlots[slot];
            else if (stage == 1) destination = dryerWipAnchor;
            else if (stage == 2) destination = rollerQueueSlots[slot];
            else if (stage == 3) destination = rollerWipAnchor;
            else if (stage == 4) destination = packagerQueueSlots[slot];
            else destination = packagerWipAnchor;
            if (destination == null)
            {
                Debug.LogError("A production line item anchor is not assigned.", this);
                return;
            }

            item.view.SetPackaged(stage >= 4);
            item.view.name = stage >= 4 ? "Пакетик · очередь продажи" : "Лист · очередь";
            Kill(ref item.movement);
            if (previousStage < 0)
            {
                item.view.transform.position = destination.position;
                return;
            }
            item.movement = item.view.transform.DOMove(destination.position, transportDuration)
                .SetEase(Ease.InOutSine).SetUpdate(true);
            if (!tweeningEnabled) item.movement.Pause();
        }

        private void RefreshProgress()
        {
            if (rollerProgressFill != null && rollerProgressFill.parent != null)
                rollerProgressFill.parent.gameObject.SetActive(runtime.RollerUnlocked);
            UpdateProgress(dryerProgressFill, dryerProgressImage, runtime.DryerHasWip,
                runtime.DryerRemaining, dryerDurationAtStart, ref dryerBarTween,
                ref lastDryerActive, ref lastDryerRemaining, ref lastDryerDuration);
            UpdateProgress(rollerProgressFill, rollerProgressImage, runtime.RollerHasWip,
                runtime.RollerRemaining, rollerDurationAtStart, ref rollerBarTween,
                ref lastRollerActive, ref lastRollerRemaining, ref lastRollerDuration);
            UpdateProgress(packagerProgressFill, packagerProgressImage, runtime.PackagerHasWip,
                runtime.PackagerRemaining, packagerDurationAtStart, ref packagerBarTween,
                ref lastPackagerActive, ref lastPackagerRemaining, ref lastPackagerDuration);
        }

        private void UpdateProgress(RectTransform fill, Image image, bool active, int remaining,
            int duration, ref Tween tween, ref bool previousActive, ref int previousRemaining,
            ref int previousDuration)
        {
            if (fill == null || image == null) return;
            if (previousActive == active && previousRemaining == remaining && previousDuration == duration) return;
            previousActive = active;
            previousRemaining = remaining;
            previousDuration = duration;
            if (!active)
            {
                Kill(ref tween);
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(0f, 1f);
            }
            else
            {
                float target = Mathf.Clamp01(1f - (float)remaining / Mathf.Max(1, duration));
                Kill(ref tween);
                tween = fill.DOAnchorMax(new Vector2(target, 1f), 0.82f)
                    .SetEase(Ease.Linear).SetUpdate(true);
                if (!tweeningEnabled) tween.Pause();
            }
            Color color = image.color;
            color.a = active ? 1f : 0.28f;
            image.color = color;
        }

        private void SetTweeningEnabled(bool enabledState)
        {
            tweeningEnabled = enabledState;
            for (int i = 0; i < visualItems.Count; i++)
                SetTween(visualItems[i].movement, enabledState);
            SetTween(harvestPulse, enabledState);
            SetTween(dryerBarTween, enabledState);
            SetTween(rollerBarTween, enabledState);
            SetTween(packagerBarTween, enabledState);
        }

        private static void SetTween(Tween tween, bool enabledState)
        {
            if (tween == null || !tween.IsActive()) return;
            if (enabledState) tween.Play();
            else tween.Pause();
        }

        private static void Kill(ref Tween tween)
        {
            if (tween == null) return;
            if (tween.IsActive()) tween.Kill();
            tween = null;
        }
    }
}
