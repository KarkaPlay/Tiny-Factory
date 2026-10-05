using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyFactory
{
    public sealed class MetaOrderRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text contents;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button replaceButton;
        private MetaFactoryRuntime boundRuntime;
        private int boundIndex;
        private Action refresh;

        public void Bind(MetaFactoryRuntime runtime, int index, MetaFactoryRuntime.OrderOffer offer, bool activeExists, Action onRefresh)
        {
            boundRuntime = runtime; boundIndex = index; refresh = onRefresh;
            string offerKind = offer.kind == "onboarding" ? " · Учебный заказ" : offer.IsPilotGoal ? " · цель цепочки" : string.Empty;
            title.text = "Предложение " + (index + 1) + offerKind;
            string contentsText = string.Empty;
            for (int i = 0; i < offer.lines.Count; i++)
            {
                MetaFactoryRuntime.OrderLine line = offer.lines[i];
                int stock = runtime.Warehouse(line.product);
                contentsText += (i == 0 ? "" : " + ") + MetaFactoryHudView.ProductName(line.product) + " ×" + line.count + " · склад " + stock + " · не хватает " + Mathf.Max(0, line.count - stock);
            }
            contents.text = contentsText + "\nВыплата · " + offer.reward + " монет";
            acceptButton.interactable = !activeExists;
            acceptButton.onClick.RemoveListener(Accept); acceptButton.onClick.AddListener(Accept);
            bool canReplace = index == 1 && !offer.IsPilotGoal;
            RectTransform acceptRect = acceptButton.GetComponent<RectTransform>();
            acceptRect.anchorMin = new Vector2(.03f, .03f); acceptRect.anchorMax = new Vector2(canReplace ? .49f : .97f, .35f);
            acceptRect.offsetMin = Vector2.zero; acceptRect.offsetMax = Vector2.zero;
            RectTransform replaceRect = replaceButton.GetComponent<RectTransform>();
            replaceRect.anchorMin = new Vector2(.51f, .03f); replaceRect.anchorMax = new Vector2(.97f, .35f);
            replaceRect.offsetMin = Vector2.zero; replaceRect.offsetMax = Vector2.zero;
            replaceButton.gameObject.SetActive(canReplace);
            replaceButton.interactable = canReplace;
            replaceButton.onClick.RemoveListener(Replace); replaceButton.onClick.AddListener(Replace);
        }

        private void Accept() { boundRuntime.AcceptOffer(boundIndex); refresh?.Invoke(); }
        private void Replace() { boundRuntime.ReplaceOffer(boundIndex); refresh?.Invoke(); }
    }
}
