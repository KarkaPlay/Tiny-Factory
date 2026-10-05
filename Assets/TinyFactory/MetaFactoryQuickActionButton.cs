using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyFactory
{
    /// <summary>Prevents a swipe that began on a near-building button from becoming a click.</summary>
    public sealed class MetaFactoryQuickActionButton : Button
    {
        [SerializeField, Min(1f)] private float movementThresholdDp = 12f;
        private Vector2 pointerStart;
        private int pointerId = int.MinValue;

        public override void OnPointerDown(PointerEventData eventData)
        {
            pointerStart = eventData.position;
            pointerId = eventData.pointerId;
            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId)
            {
                float scale = Screen.dpi > 0f ? Screen.dpi / 160f : Mathf.Max(.5f, Screen.height / 1920f);
                if (Vector2.Distance(pointerStart, eventData.position) >= movementThresholdDp * scale)
                    eventData.eligibleForClick = false;
                pointerId = int.MinValue;
            }
            base.OnPointerUp(eventData);
        }
    }
}
