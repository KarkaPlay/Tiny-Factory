using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory
{
    /// <summary>View controller for the authored HUD; it does not create or own the scene hierarchy.</summary>
    public sealed class MetaFactoryHudView : MonoBehaviour
    {
        private const string ForegroundDisclosure = "Прототип 0.5 · производство только в открытой игре";
        [Header("Runtime and safe area")]
        [SerializeField] private MetaFactoryRuntime runtime;
        [SerializeField] private RectTransform safeAreaRoot;
        [SerializeField] private TMP_Text walletLabel;
        [SerializeField] private TMP_Text activeOrderLabel;
        [SerializeField] private TMP_Text statusLabel;

        [Header("Pinned lower card and tabs")]
        [SerializeField] private GameObject factoryPanel;
        [SerializeField] private GameObject warehousePanel;
        [SerializeField] private GameObject ordersPanel;
        [SerializeField] private Button factoryTabButton;
        [SerializeField] private Button warehouseTabButton;
        [SerializeField] private Button ordersTabButton;
        [SerializeField] private TMP_Text selectedStationTitle;
        [SerializeField] private TMP_Text selectedStationState;
        [SerializeField] private TMP_Text stationBuildLabel;
        [SerializeField] private TMP_Text stationUpgradeLabel;
        [SerializeField] private TMP_Text transferPreviewLabel;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button upgradeButton;

        [Header("Warehouse")]
        [SerializeField] private TMP_Text warehouseLabel;

        [Header("Orders")]
        [SerializeField] private Transform offerRowsRoot;
        [SerializeField] private MetaOrderRowView orderRowPrefab;
        [SerializeField] private TMP_Text activeOrderDetails;
        [SerializeField] private Button turnInButton;
        [SerializeField] private Button cancelOrderButton;
        [SerializeField] private GameObject cancelConfirmationPanel;
        [SerializeField] private Button confirmCancelButton;
        [SerializeField] private Button dismissCancelButton;

        [Header("Tutorial and migration")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TMP_Text tutorialLabel;
        [SerializeField] private GameObject migrationPanel;
        [SerializeField] private TMP_Text migrationLabel;
        [SerializeField] private Button migrationConfirmButton;

        private bool cancelConfirmation;
        private bool subscribed;
        private MetaStation selectedStation;
        private MetaFactoryRuntime.PanelTab selectedTab;
        public MetaStation SelectedStation => selectedStation;

        private void OnEnable()
        {
            if (runtime == null) runtime = GetComponent<MetaFactoryRuntime>();
            if (!HasRequiredReferences())
            {
                Debug.LogError("MetaFactoryHudView has missing authored references. Run Tiny Factory/Author M05 Scene before entering Play Mode.", this);
                enabled = false;
                return;
            }
            selectedStation = runtime.SelectedStation;
            selectedTab = runtime.SelectedTab;
            Bind(factoryTabButton, () => SelectTab(MetaFactoryRuntime.PanelTab.Factory));
            Bind(warehouseTabButton, () => SelectTab(MetaFactoryRuntime.PanelTab.Warehouse));
            Bind(ordersTabButton, () => SelectTab(MetaFactoryRuntime.PanelTab.Orders));
            Bind(buildButton, BuildSelected);
            Bind(upgradeButton, UpgradeSelected);
            Bind(turnInButton, TurnIn);
            Bind(cancelOrderButton, BeginCancel);
            Bind(confirmCancelButton, ConfirmCancel);
            Bind(dismissCancelButton, DismissCancel);
            Bind(migrationConfirmButton, ConfirmMigration);
            runtime.Changed += Refresh;
            runtime.TimerChanged += RefreshTimerLabels;
            runtime.Notice += ShowStatus;
            subscribed = true;
            Refresh();
        }

        private void OnDisable()
        {
            if (!subscribed || runtime == null) return;
            runtime.Changed -= Refresh;
            runtime.TimerChanged -= RefreshTimerLabels;
            runtime.Notice -= ShowStatus;
            subscribed = false;
        }

        private void Update()
        {
            if (safeAreaRoot == null) return;
            Rect safe = Screen.safeArea;
            safeAreaRoot.anchorMin = new Vector2(safe.xMin / Mathf.Max(1f, Screen.width), safe.yMin / Mathf.Max(1f, Screen.height));
            safeAreaRoot.anchorMax = new Vector2(safe.xMax / Mathf.Max(1f, Screen.width), safe.yMax / Mathf.Max(1f, Screen.height));
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        public void SelectStation(MetaStation station)
        {
            if (!System.Enum.IsDefined(typeof(MetaStation), station)) return;
            selectedStation = station;
            PersistUiState();
            Refresh();
        }

        public void CommitWorldSelection(MetaStation station, float offset)
        {
            if (!System.Enum.IsDefined(typeof(MetaStation), station)) return;
            selectedStation = station;
            runtime.SetUiState(selectedStation, selectedTab, runtime.TransferQuantity, offset);
            Refresh();
        }

        public void PreviewWorldSelection(MetaStation station)
        {
            if (!System.Enum.IsDefined(typeof(MetaStation), station) || selectedStation == station) return;
            selectedStation = station;
            Refresh();
        }

        public void ReportActionResult(string message) => ShowStatus(message);

        public void SaveWorldCameraOffset(float offset) => runtime.SetUiState(selectedStation, selectedTab, runtime.TransferQuantity, offset);
        public float GetSavedCameraVerticalOffset() => runtime.CameraVerticalOffset;

        private bool HasRequiredReferences() => runtime != null && safeAreaRoot != null && walletLabel != null && activeOrderLabel != null &&
            statusLabel != null && factoryPanel != null && warehousePanel != null && ordersPanel != null && selectedStationTitle != null &&
            selectedStationState != null && stationBuildLabel != null && stationUpgradeLabel != null && transferPreviewLabel != null &&
            buildButton != null && upgradeButton != null && warehouseLabel != null && offerRowsRoot != null && orderRowPrefab != null &&
            activeOrderDetails != null && turnInButton != null && cancelOrderButton != null && cancelConfirmationPanel != null &&
            confirmCancelButton != null && dismissCancelButton != null && tutorialPanel != null && tutorialLabel != null &&
            migrationPanel != null && migrationLabel != null && migrationConfirmButton != null;

        private void Refresh()
        {
            if (!enabled || runtime == null) return;
            walletLabel.text = "Монеты · " + runtime.Coins.ToString("N0");
            MetaFactoryRuntime.OrderOffer active = runtime.ActiveOrder;
            if (active == null) activeOrderLabel.text = "Активный заказ · выберите предложение";
            else
            {
                string shortfall;
                bool readyToTurnIn = runtime.CanTurnIn(out shortfall);
                activeOrderLabel.text = (active.IsPilotGoal ? "Цель · " : "Заказ · ") + Format(active) + " · " +
                    (readyToTurnIn ? "готов" : "Не хватает: " + shortfall) + " · " + active.reward + " монет";
            }
            bool migrating = runtime.MigrationPending;
            migrationPanel.SetActive(migrating);
            migrationLabel.text = FormatMigrationNotice(runtime.MigrationLegacyCoins, runtime.MigrationRefund, runtime.MigrationResultCoins,
                runtime.Config.walletCapacity, runtime.Config.freshStartQuantity, runtime.Config.freshSaveCoins);
            factoryPanel.SetActive(!migrating && selectedTab == MetaFactoryRuntime.PanelTab.Factory);
            warehousePanel.SetActive(!migrating && selectedTab == MetaFactoryRuntime.PanelTab.Warehouse);
            ordersPanel.SetActive(!migrating && selectedTab == MetaFactoryRuntime.PanelTab.Orders);
            RefreshTabHighlight();
            cancelConfirmationPanel.SetActive(cancelConfirmation && runtime.ActiveOrder != null);
            if (cancelConfirmationPanel.activeSelf) cancelConfirmationPanel.transform.SetAsLastSibling();
            tutorialPanel.SetActive(!migrating && !runtime.TutorialComplete);
            RefreshStation();
            RefreshWarehouse();
            RefreshOrders();
            RefreshTutorial();
            statusLabel.text = ForegroundDisclosure + (string.IsNullOrEmpty(runtime.StatusText) ? string.Empty : "\n" + runtime.StatusText);
        }

        private void RefreshStation()
        {
            MetaFactoryRuntime.StationState station = runtime.Station(selectedStation);
            string title = selectedStation == MetaStation.Garden ? "Чайный сад" : selectedStation == MetaStation.Dryer ? "Сушильня" : "Фасовка";
            selectedStationTitle.text = title;
            if (station == null) { selectedStationState.text = "Участок недоступен"; return; }
            if (!station.built)
            {
                bool unlocked = selectedStation != MetaStation.Packer || runtime.LifetimeDryTeaProduced >= runtime.Config.dryerUnlockPackerAtProduced;
                long cost = selectedStation == MetaStation.Dryer ? runtime.Config.dryerBuildCost : runtime.Config.packerBuildCost;
                stationBuildLabel.text = unlocked ? (runtime.Coins >= cost ? "Постройка · " + cost + " монет" : "Постройка · " + cost + " монет · не хватает " + (cost - runtime.Coins)) :
                    "Нужно произвести " + ProductName(MetaProduct.DryTea) + " · " + runtime.Config.dryerUnlockPackerAtProduced + " · сейчас " + runtime.LifetimeDryTeaProduced;
                buildButton.gameObject.SetActive(unlocked);
                SetActionAvailability(buildButton, unlocked && runtime.Coins >= cost);
                buildButton.GetComponentInChildren<TMP_Text>().text = "Построить · " + cost;
                stationUpgradeLabel.text = string.Empty;
                upgradeButton.gameObject.SetActive(false);
                transferPreviewLabel.text = string.Empty;
                selectedStationState.text = "Участок закрыт";
                return;
            }
            buildButton.gameObject.SetActive(false);
            int speed = selectedStation == MetaStation.Garden ? runtime.Config.gardenSecondsByLevel[station.level] :
                selectedStation == MetaStation.Dryer ? runtime.Config.dryerSecondsByLevel[station.level] : runtime.Config.packerSecondsByLevel[station.level];
            selectedStationState.text = "Уровень " + station.level + (station.level == 2 ? " · MAX" : "") + "\n" +
                (selectedStation == MetaStation.Garden ? ProductName(MetaProduct.FreshLeaf) + " · выход " + station.output + "/" + runtime.Config.localCapacity + " · следующий лист через " + station.gardenRemainingSeconds + " с" :
                 "Вход " + station.input + "/" + runtime.Config.localCapacity + " · работа " + (station.remainingSeconds > 0 ? station.remainingSeconds + " с" : station.outputBlocked ? "выход заполнен" : "ожидает сырьё") +
                 " · выход " + station.output + "/" + runtime.Config.localCapacity + " · цикл " + speed + " с");
            if (station.output > 0)
            {
                MetaProduct product = selectedStation == MetaStation.Garden ? MetaProduct.FreshLeaf : selectedStation == MetaStation.Dryer ? MetaProduct.DryTea : MetaProduct.TeaPacket;
                selectedStationState.text += "\nГотово к сбору · " + ProductName(product) + " ×" + station.output;
            }
            long upgradeCost = runtime.NextUpgradeCost(selectedStation);
            stationUpgradeLabel.text = upgradeCost < 0 ? "MAX · конечное улучшение" : runtime.Coins >= upgradeCost ?
                "Следующее улучшение · " + upgradeCost + " монет" : "Следующее улучшение · " + upgradeCost + " монет · не хватает " + (upgradeCost - runtime.Coins);
            upgradeButton.gameObject.SetActive(upgradeCost >= 0); SetActionAvailability(upgradeButton, upgradeCost >= 0 && runtime.Coins >= upgradeCost);
            if (upgradeCost >= 0) upgradeButton.GetComponentInChildren<TMP_Text>().text = "Улучшить · " + upgradeCost;
            RefreshStationInventory(station);
        }

        private void RefreshStationInventory(MetaFactoryRuntime.StationState station)
        {
            int collectMax = runtime.MaxCollect(selectedStation);
            MetaProduct output = selectedStation == MetaStation.Garden ? MetaProduct.FreshLeaf : selectedStation == MetaStation.Dryer ? MetaProduct.DryTea : MetaProduct.TeaPacket;
            MetaProduct input = selectedStation == MetaStation.Dryer ? MetaProduct.FreshLeaf : MetaProduct.DryTea;
            string inputText = selectedStation == MetaStation.Garden ? "" : "Вход " + station.input + "/" + runtime.Config.localCapacity + " · " + ProductName(input) +
                (selectedStation == MetaStation.Dryer ? " · рецепт " + runtime.Config.freshLeafPerBatch : "") +
                " · склад " + runtime.Warehouse(input);
            string outputText = "Выход " + station.output + "/" + runtime.Config.localCapacity + " " + ProductName(output) +
                " · на склад сейчас поместится " + collectMax + " · общий склад " + runtime.WarehouseTotal + "/" + runtime.Config.warehouseTotalCapacity;
            transferPreviewLabel.text = (inputText.Length > 0 ? inputText + "\n" : string.Empty) + outputText;
        }

        private void RefreshWarehouse()
        {
            warehouseLabel.text = "Общий склад · " + runtime.WarehouseTotal + "/" + runtime.Config.warehouseTotalCapacity + "\n" +
                ProductName(MetaProduct.FreshLeaf) + " · " + runtime.Warehouse(MetaProduct.FreshLeaf) + "/" + runtime.Config.warehouseProductCapacity + "\n" +
                ProductName(MetaProduct.DryTea) + " · " + runtime.Warehouse(MetaProduct.DryTea) + "/" + runtime.Config.warehouseProductCapacity + "\n" +
                ProductName(MetaProduct.TeaPacket) + " · " + runtime.Warehouse(MetaProduct.TeaPacket) + "/" + runtime.Config.warehouseProductCapacity;
        }

        private void RefreshOrders()
        {
            for (int i = offerRowsRoot.childCount - 1; i >= 0; i--) Destroy(offerRowsRoot.GetChild(i).gameObject);
            MetaFactoryRuntime.OrderOffer active = runtime.ActiveOrder;
            activeOrderDetails.text = active == null ? "Нет активного заказа · товары остаются доступными для производства" :
                Format(active) + "\nНаграда · " + active.reward + " монет · товары не резервируются";
            turnInButton.gameObject.SetActive(active != null);
            cancelOrderButton.gameObject.SetActive(active != null);
            string shortfall;
            if (active != null)
            {
                turnInButton.interactable = runtime.CanTurnIn(out shortfall);
                turnInButton.GetComponentInChildren<TMP_Text>().text = turnInButton.interactable ? "Сдать заказ" : "Нужны товары · сдача целиком";
                cancelOrderButton.interactable = true;
            }
            for (int i = 0; i < runtime.OfferCount; i++)
            {
                MetaOrderRowView row = Instantiate(orderRowPrefab, offerRowsRoot, false);
                row.Bind(runtime, i, runtime.Offers[i], active != null, Refresh);
            }
        }

        private void RefreshTabHighlight()
        {
            ApplyTabStyle(factoryTabButton, selectedTab == MetaFactoryRuntime.PanelTab.Factory);
            ApplyTabStyle(warehouseTabButton, selectedTab == MetaFactoryRuntime.PanelTab.Warehouse);
            ApplyTabStyle(ordersTabButton, selectedTab == MetaFactoryRuntime.PanelTab.Orders);
        }

        private static void ApplyTabStyle(Button button, bool selected)
        {
            if (button == null) return;
            // Selection itself is the cue; pointer hover must not darken the selected gold fill.
            button.transition = Selectable.Transition.None;
            Image image = button.targetGraphic as Image;
            if (image != null) image.color = selected ? new Color32(224, 180, 77, 255) : new Color32(26, 61, 15, 255);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = selected ? new Color32(31, 47, 23, 255) : new Color32(255, 255, 255, 255);
                text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private static void SetActionAvailability(Button button, bool available)
        {
            if (button == null) return;
            button.interactable = available;
            button.transition = Selectable.Transition.None;
            Image image = button.targetGraphic as Image;
            if (image != null) image.color = available ? new Color32(47, 96, 37, 255) : new Color32(190, 188, 174, 255);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null) text.color = available ? Color.white : new Color32(85, 82, 74, 255);
        }

        private void RefreshTimerLabels()
        {
            if (runtime == null || runtime.MigrationPending) return;
            if (factoryPanel != null && factoryPanel.activeSelf) RefreshStation();
        }

        private void RefreshTutorial()
        {
            int step = runtime.TutorialStep;
            string[] steps = { "1 · Выберите учебное предложение: " + ProductName(MetaProduct.FreshLeaf) + " ×" + runtime.Config.onboardingFreshLeafQuantity + ".", "2 · Сдайте первые " + runtime.Config.onboardingFreshLeafQuantity + " " + ProductName(MetaProduct.FreshLeaf) + " со склада.",
                "3 · Постройте сушильню за " + runtime.Config.dryerBuildCost + " монет.", "4 · Загрузите " + runtime.Config.freshLeafPerBatch + " " + ProductName(MetaProduct.FreshLeaf) + " по одному через +1; после первого листа дождитесь второго.", "5 · Заберите первый " + ProductName(MetaProduct.DryTea) + " на общий склад." };
            tutorialLabel.text = step >= 0 && step < steps.Length ? steps[step] : "Обучение завершено";
        }

        private void BuildSelected() { if (!runtime.Build(selectedStation)) ShowActionFailure("Постройка сейчас недоступна"); Refresh(); }
        private void UpgradeSelected() { if (!runtime.Upgrade(selectedStation)) ShowActionFailure("Улучшение сейчас недоступно"); Refresh(); }
        private void SelectTab(MetaFactoryRuntime.PanelTab value) { selectedTab = value; PersistUiState(); Refresh(); }
        private void TurnIn() { if (!runtime.TurnInActiveOrder()) ShowActionFailure("Полный набор изменился; заказ не списан"); Refresh(); }
        private void BeginCancel() { cancelConfirmation = true; Refresh(); }
        private void ConfirmCancel()
        {
            if (runtime.CancelActiveOrder(true)) cancelConfirmation = false;
            else ShowActionFailure("Отказ не применён · сохранение или состояние заказа не изменилось");
            Refresh();
        }
        private void DismissCancel() { cancelConfirmation = false; Refresh(); }
        private void ConfirmMigration() { if (runtime.ConfirmMigration()) { selectedTab = MetaFactoryRuntime.PanelTab.Factory; Refresh(); } }
        private void PersistUiState() => runtime.SetUiState(selectedStation, selectedTab, runtime.TransferQuantity, runtime.CameraVerticalOffset);
        private void ShowStatus(string message) { if (statusLabel != null) statusLabel.text = ForegroundDisclosure + "\n" + message; }
        private string ActionFailure(string fallback) => runtime != null && runtime.PersistenceBlocked ? runtime.StatusText : fallback;
        private void ShowActionFailure(string fallback) => ShowStatus(ActionFailure(fallback));

        public static string ProductName(MetaProduct product) => product == MetaProduct.FreshLeaf ? "Свежий лист" : product == MetaProduct.DryTea ? "Сухой чай" : "Пакетики";
        public static string FormatMigrationNotice(long legacyCoins, int calculatedRefund, long resultCoins, long walletCapacity, int freshStartQuantity, long freshSaveCoins)
        {
            long creditedLegacy = System.Math.Min(legacyCoins, walletCapacity);
            long creditedRefund = System.Math.Max(0L, resultCoins - creditedLegacy);
            return "Баланс 0.4: " + legacyCoins.ToString("N0") + " монет; расчётная компенсация старых уровней " +
                calculatedRefund.ToString("N0") + ", зачислено из неё " + creditedRefund.ToString("N0") + " (лимит кошелька " + walletCapacity.ToString("N0") + "). Итог " + resultCoins.ToString("N0") +
                ". Новая кампания начнётся с " + freshStartQuantity + " " + ProductName(MetaProduct.FreshLeaf) +
                "; новый стартовый бонус " + freshSaveCoins + " монет не добавляется. Продажи 0.4 не считаются заказами.";
        }
        private static string Format(MetaFactoryRuntime.OrderOffer offer)
        {
            string result = string.Empty;
            for (int i = 0; i < offer.lines.Count; i++) result += (i == 0 ? "" : " + ") + ProductName(offer.lines[i].product) + " ×" + offer.lines[i].count;
            return result;
        }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }

}
