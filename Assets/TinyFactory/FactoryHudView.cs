using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory
{
    public sealed class FactoryHudView : MonoBehaviour
    {
        [Header("State and safe area")]
        [SerializeField] private FactoryRuntime runtime;
        [SerializeField] private Camera worldCamera;
        [SerializeField, Min(0.1f)] private float minimumOrthographicSize = 6.4f;
        [SerializeField, Min(0.1f)] private float requiredHalfWidth = 3.5f;

        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private Transform bushLabelAnchor;
        [SerializeField] private Transform dryerLabelAnchor;
        [SerializeField] private Transform saleLabelAnchor;

        [Header("Status labels")]
        [SerializeField] private TMP_Text walletLabel;
        [SerializeField] private TMP_Text soldLabel;
        [SerializeField] private TMP_Text firstActionLabel;
        [SerializeField] private TMP_Text queueLabel;
        [SerializeField] private TMP_Text autoLabel;
        [SerializeField] private TMP_Text bushLabel;
        [SerializeField] private TMP_Text dryerStationLabel;
        [SerializeField] private TMP_Text saleStationLabel;
        [SerializeField] private TMP_Text dryerStatusLabel;
        [SerializeField] private TMP_Text packagerStatusLabel;
        [SerializeField] private TMP_Text speedLabel;
        [SerializeField] private TMP_Text feedbackLabel;
        
        [Header("Feedback palette")]
        [SerializeField] private Color acceptedFeedbackColor = new Color(0.72f, 1f, 0.68f);
        [SerializeField] private Color automaticFeedbackColor = new Color(0.75f, 0.9f, 0.68f);
        [SerializeField] private Color saleFeedbackColor = Color.white;
        [SerializeField] private Color upgradeFeedbackColor = new Color(0.73f, 1f, 0.69f);
        [SerializeField] private CanvasGroup feedbackGroup;

        [Header("Persistent UnityEvent targets")]
        [SerializeField] private Button harvestButton;
        [SerializeField] private Button speedButton;

        [SerializeField] private Button productivityButton;
        [SerializeField] private Button automationButton;
        [SerializeField] private TMP_Text productivityLabel;
        [SerializeField] private TMP_Text automationLabel;
        private Tween feedbackTween;
        private Tween walletPulse;
        private int observedSpeedLevel;
        private bool manualHarvestSeen;
        private bool feedbackIsSale;
        private bool pendingSpeedAck;
        private bool lastForeground;
        private bool subscribed;
        private bool tweeningEnabled;

        private void OnEnable()
        {
            if (runtime == null)
            {
                Debug.LogError("FactoryHudView requires its FactoryRuntime scene reference.", this);
                enabled = false;
                return;
            }
            if (!subscribed)
            {
                runtime.Changed += Refresh;
                runtime.ItemAccepted += OnItemAccepted;
                runtime.SaleCommitted += OnSaleCommitted;
                subscribed = true;
            }
            observedSpeedLevel = runtime.SpeedLevel;
            lastForeground = runtime.IsForeground;
            SetTweeningEnabled(lastForeground);
            Refresh();
        }

        private void OnDisable()
        {
            SetTweeningEnabled(false);
            if (subscribed && runtime != null)
            {
                runtime.Changed -= Refresh;
                runtime.ItemAccepted -= OnItemAccepted;
                runtime.SaleCommitted -= OnSaleCommitted;
                subscribed = false;
            }
        }

        private void OnDestroy()
        {
            Kill(ref feedbackTween);
            Kill(ref walletPulse);
        }

        private void Update()
        {
            if (runtime == null || safeAreaRoot == null) return;
            if (worldCamera != null && worldCamera.orthographic)
            {
                float targetSize = Mathf.Max(minimumOrthographicSize,
                    requiredHalfWidth / Mathf.Max(0.01f, worldCamera.aspect));
                if (!Mathf.Approximately(worldCamera.orthographicSize, targetSize))
                    worldCamera.orthographicSize = targetSize;
            }
            Rect safe = Screen.safeArea;
            safeAreaRoot.anchorMin = new Vector2(safe.xMin / Mathf.Max(1f, Screen.width), safe.yMin / Mathf.Max(1f, Screen.height));
            safeAreaRoot.anchorMax = new Vector2(safe.xMax / Mathf.Max(1f, Screen.width), safe.yMax / Mathf.Max(1f, Screen.height));
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            bool active = runtime.IsForeground;
            if (active != lastForeground)
            {
                lastForeground = active;
                SetTweeningEnabled(active);
                Refresh();
            }

            autoLabel.text = "Автосбор: " + runtime.Config.ManualBatch(runtime.ProductivityLevel) + " лист каждые " +
                runtime.Config.AutomationInterval(runtime.AutomationLevel) + " с\nСледующий через " + runtime.SecondsUntilAuto + " с";
            dryerStatusLabel.text = runtime.DryerHasWip ? "Сушка · " + runtime.DryerRemaining + " с" : "Сушка · ожидание";
            packagerStatusLabel.text = runtime.PackagerHasWip ? "Продажа · " + runtime.PackagerRemaining + " с" : "Продажа · ожидание";
            bushLabel.text = "Куст · сбор ×" + runtime.Config.ManualBatch(runtime.ProductivityLevel);
            saleStationLabel.text = runtime.SealerUnlocked ? "Запайщик · касса" : "Стол · упаковка";
            dryerStationLabel.text = runtime.RollerUnlocked ? "Сушилка → скрутчик" : "Сушилка";
            PositionWorldLabel(bushLabel.transform.parent as RectTransform, bushLabelAnchor);
            PositionWorldLabel(dryerStationLabel.transform.parent as RectTransform, dryerLabelAnchor);
            PositionWorldLabel(saleStationLabel.transform.parent as RectTransform, saleLabelAnchor);
        }

        public void HandleHarvestButton()
        {
            if (runtime != null) runtime.RequestHarvest();
        }

        public void HandleSpeedUpgradeButton() => RequestUpgrade(UpgradeBranch.Speed);
        public void HandleProductivityUpgradeButton() => RequestUpgrade(UpgradeBranch.Productivity);
        public void HandleAutomationUpgradeButton() => RequestUpgrade(UpgradeBranch.Automation);

        private void RequestUpgrade(UpgradeBranch branch)
        {
            if (runtime != null) runtime.RequestUpgrade(branch);
        }


        private void Refresh()
        {
            if (runtime == null || walletLabel == null) return;
            walletLabel.text = "●  " + runtime.Coins + " монет";
            soldLabel.text = "Продано  " + runtime.Sold;
            queueLabel.text = "Листьев в очереди " + runtime.TotalInFlight;
            harvestButton.interactable = runtime.IsForeground &&
                runtime.SourceBuffer + runtime.Config.ManualBatch(runtime.ProductivityLevel) <= runtime.Capacity;

            if (observedSpeedLevel == 0 && runtime.SpeedLevel == 1)
            {
                if (feedbackIsSale) pendingSpeedAck = true;
                else ShowFeedback("Сушка ускорена: с " + runtime.Config.DryerDuration(0) +
                    " до " + runtime.Config.DryerDuration(1) + " с", upgradeFeedbackColor);
                observedSpeedLevel = 1;
            }

            RefreshUpgrade(UpgradeBranch.Speed, speedLabel, speedButton);
            RefreshUpgrade(UpgradeBranch.Productivity, productivityLabel, productivityButton);
            RefreshUpgrade(UpgradeBranch.Automation, automationLabel, automationButton);
            if (!runtime.RollerUnlocked)
                dryerStationLabel.text = "Сушилка · скрутчик " + runtime.Sold + "/" + runtime.Config.rollerUnlockSales;
            if (!runtime.SealerUnlocked)
                saleStationLabel.text = "Стол · запайщик " + runtime.Sold + "/" + runtime.Config.sealerUnlockSales;
        }

        private void RefreshUpgrade(UpgradeBranch branch, TMP_Text label, Button button)
        {
            if (label == null || button == null) return;
            int level = runtime.Level(branch);
            if (level >= 3)
            {
                string name = BranchName(branch);
                label.text = name + " · L3/3 · MAX";
                button.interactable = false;
                return;
            }
            int cost = runtime.Config.UpgradeCost(level);
            int next = level + 1;
            long deficit = System.Math.Max(0L, cost - runtime.Coins);
            string effect = branch switch
            {
                UpgradeBranch.Speed => "Станки " + runtime.Config.DryerDuration(level) + "→" + runtime.Config.DryerDuration(next) + " с",
                UpgradeBranch.Productivity => "Сбор ×" + runtime.Config.ManualBatch(level) + "→×" + runtime.Config.ManualBatch(next),
                _ => "Автосбор " + runtime.Config.AutomationInterval(level) + "→" + runtime.Config.AutomationInterval(next) + " с"
            };
            label.text = BranchName(branch) + " · L" + level + "/3 · " + effect + "\n" +
                (deficit == 0 ? "Улучшить · " + cost + " монет" : cost + " монет · нужно ещё " + deficit);
            button.interactable = runtime.IsForeground && deficit == 0;
        }

        private static string BranchName(UpgradeBranch branch) => branch switch
        {
            UpgradeBranch.Speed => "Скорость",
            UpgradeBranch.Productivity => "Производительность",
            _ => "Автоматизация"
        };

        private void OnItemAccepted(bool automatic)
        {
            if (automatic)
            {
                ShowFeedback("Автосбор · лист добавлен в очередь", automaticFeedbackColor);
            }
            else
            {
                manualHarvestSeen = true;
                firstActionLabel.text = "Путь листа: куст / сушка / упаковка / продажа +" + runtime.Config.saleValue;
                ShowFeedback("Лист принят · очередь +1", acceptedFeedbackColor);
            }
        }

        private void OnSaleCommitted(long amount)
        {
            feedbackIsSale = true;
            feedbackLabel.text = "Продажа  +" + amount;
            feedbackLabel.color = saleFeedbackColor;
            feedbackGroup.alpha = 1f;
            Kill(ref feedbackTween);
            feedbackTween = DOTween.Sequence()
                .Append(feedbackLabel.transform.DOScale(1.12f, 0.16f).SetEase(Ease.OutBack))
                .Append(feedbackLabel.transform.DOScale(1f, 0.16f))
                .AppendInterval(0.4f)
                .Append(feedbackGroup.DOFade(0f, 0.22f))
                .OnComplete(() =>
                {
                    feedbackIsSale = false;
                    feedbackLabel.text = string.Empty;
                    if (pendingSpeedAck)
                    {
                        pendingSpeedAck = false;
                        ShowFeedback("Сушка ускорена: с " + runtime.Config.DryerDuration(0) +
                            " до " + runtime.Config.DryerDuration(1) + " с", upgradeFeedbackColor);
                    }
                }).SetUpdate(true);
            Kill(ref walletPulse);
            walletPulse = DOTween.Sequence()
                .Append(walletLabel.transform.DOScale(1.08f, 0.12f).SetEase(Ease.OutBack))
                .Append(walletLabel.transform.DOScale(1f, 0.16f)).SetUpdate(true);
            if (!tweeningEnabled) SetTweeningEnabled(false);
            if (!manualHarvestSeen && runtime.Sold == 1)
                firstActionLabel.text = "Автосбор запустил линию · нажмите «Собрать лист»";
        }

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackLabel == null || feedbackGroup == null || feedbackIsSale) return;
            feedbackLabel.text = message;
            feedbackLabel.color = color;
            feedbackGroup.alpha = 1f;
            Kill(ref feedbackTween);
            feedbackTween = DOTween.Sequence().AppendInterval(0.85f)
                .Append(feedbackGroup.DOFade(0f, 0.2f))
                .OnComplete(() => feedbackLabel.text = string.Empty).SetUpdate(true);
            if (!tweeningEnabled) feedbackTween.Pause();
        }

        private void PositionWorldLabel(RectTransform label, Transform anchor)
        {
            if (label == null || anchor == null || worldCamera == null || safeAreaRoot == null) return;
            Vector3 viewport = worldCamera.WorldToViewportPoint(anchor.position);
            if (viewport.z <= 0f || safeAreaRoot.rect.width <= 0f || safeAreaRoot.rect.height <= 0f) return;

            Rect safe = Screen.safeArea;
            float minX = safe.xMin / Mathf.Max(1f, Screen.width);
            float minY = safe.yMin / Mathf.Max(1f, Screen.height);
            float width = safe.width / Mathf.Max(1f, Screen.width);
            float height = safe.height / Mathf.Max(1f, Screen.height);
            float safeX = Mathf.Clamp01((viewport.x - minX) / Mathf.Max(0.001f, width));
            float safeY = Mathf.Clamp01((viewport.y - minY) / Mathf.Max(0.001f, height));

            float halfWidth = label.rect.width * 0.5f / safeAreaRoot.rect.width;
            float halfHeight = label.rect.height * 0.5f / safeAreaRoot.rect.height;
            safeX = Mathf.Clamp(safeX, halfWidth, 1f - halfWidth);
            safeY = Mathf.Clamp(safeY, halfHeight, 1f - halfHeight);
            Vector2 anchorPoint = new Vector2(safeX, safeY);
            label.anchorMin = anchorPoint;
            label.anchorMax = anchorPoint;
            label.anchoredPosition = Vector2.zero;
        }

        private void SetTweeningEnabled(bool enabledState)
        {
            tweeningEnabled = enabledState;
            SetTween(feedbackTween, enabledState);
            SetTween(walletPulse, enabledState);
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