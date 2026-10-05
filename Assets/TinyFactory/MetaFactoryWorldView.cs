using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TinyFactory
{
    /// <summary>Owns fixed-plot selection, vertical pan, and settle-to-plot camera motion.</summary>
    public sealed class MetaFactoryWorldView : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private MetaFactoryHudView hud;
        [SerializeField] private Transform plotsRoot;
        [SerializeField] private float minimumCameraY = -12f;
        [SerializeField] private float maximumCameraY = 0f;
        [SerializeField, Min(1f)] private float gestureThresholdDp = 12f;
        [SerializeField, Min(0f)] private float selectionHysteresisDp = 24f;
        [SerializeField, Range(.1f, .55f)] private float viewportBottomFraction = .49f;
        [SerializeField, Range(.51f, .95f)] private float viewportTopFraction = .83f;
        [SerializeField, Min(.05f)] private float snapDuration = .24f;

        private int activePointerId = int.MinValue;
        private Vector2 pointerStart;
        private bool dragging;
        private bool gestureClaimed;
        private float cameraStartY;
        private float cameraHomeY;
        private Tween snapTween;
        private readonly List<RaycastResult> pointerRaycasts = new List<RaycastResult>(8);
        private MetaFactoryPlot[] plots;

        private void Start()
        {
            if (worldCamera == null || hud == null || plotsRoot == null)
            {
                Debug.LogError("MetaFactoryWorldView requires the authored camera, HUD and plot root.", this);
                enabled = false;
                return;
            }
            ValidateOneColumn();
            plots = plotsRoot.GetComponentsInChildren<MetaFactoryPlot>(true);
            cameraHomeY = worldCamera.transform.position.y;
            Vector3 cameraPosition = worldCamera.transform.position;
            cameraPosition.y = ClampCameraY(cameraHomeY + hud.GetSavedCameraVerticalOffset());
            worldCamera.transform.position = cameraPosition;
            SnapToNearestPlot();
        }

        private void OnDisable() { KillSnap(); }
        private void OnDestroy() { KillSnap(); }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelGestureAndSettle();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) CancelGestureAndSettle();
        }

        private void CancelGestureAndSettle()
        {
            bool snapActive = snapTween != null && snapTween.IsActive();
            if (!snapActive && activePointerId == int.MinValue) return;
            activePointerId = int.MinValue;
            dragging = false;
            gestureClaimed = false;
            KillSnap();
            SnapImmediatelyToNearestPlot();
        }

        private void Update()
        {
            if (!enabled || worldCamera == null) return;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 mouse = Mouse.current.position.ReadValue();
                if (Mouse.current.leftButton.wasPressedThisFrame) BeginPointer(-1, mouse);
                if (Mouse.current.leftButton.isPressed && activePointerId == -1) MovePointer(mouse);
                if (Mouse.current.leftButton.wasReleasedThisFrame && activePointerId == -1) EndPointer(mouse);
            }
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    int id = touch.touchId.ReadValue();
                    Vector2 position = touch.position.ReadValue();
                    UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                    if (phase == UnityEngine.InputSystem.TouchPhase.Began) BeginPointer(id, position);
                    else if (id == activePointerId && (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)) MovePointer(position);
                    else if (id == activePointerId && (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)) EndPointer(position);
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) BeginPointer(-1, Input.mousePosition);
            if (Input.GetMouseButton(0) && activePointerId == -1) MovePointer(Input.mousePosition);
            if (Input.GetMouseButtonUp(0) && activePointerId == -1) EndPointer(Input.mousePosition);
#endif
        }

        private void BeginPointer(int id, Vector2 position)
        {
            if (activePointerId != int.MinValue || IsPointerOverUi(id, position)) return;
            KillSnap();
            activePointerId = id;
            pointerStart = position;
            cameraStartY = worldCamera.transform.position.y;
            dragging = false;
            gestureClaimed = false;
        }

        private void MovePointer(Vector2 position)
        {
            if (activePointerId == int.MinValue) return;
            Vector2 total = position - pointerStart;
            float thresholdPixels = DpToPixels(gestureThresholdDp);
            if (!gestureClaimed && total.magnitude >= thresholdPixels) gestureClaimed = true;
            if (!dragging && gestureClaimed && Mathf.Abs(total.y) >= thresholdPixels && Mathf.Abs(total.y) > Mathf.Abs(total.x) * 1.2f) dragging = true;
            if (!dragging) return;
            float worldUnitsPerPixel = (worldCamera.orthographic ? worldCamera.orthographicSize * 2f : 20f) / Mathf.Max(1, Screen.height);
            Vector3 camera = worldCamera.transform.position;
            camera.y = Mathf.Clamp(cameraStartY - total.y * worldUnitsPerPixel,
                cameraHomeY + minimumCameraY, cameraHomeY + maximumCameraY);
            worldCamera.transform.position = camera;
            MetaFactoryPlot nearest = FindNearestVisiblePlot();
            if (nearest != null) hud.PreviewWorldSelection(nearest.Station);
        }

        private void EndPointer(Vector2 position)
        {
            if (activePointerId == int.MinValue) return;
            Vector2 total = position - pointerStart;
            bool wasDrag = dragging;
            activePointerId = int.MinValue;
            dragging = false;
            if (wasDrag) SnapToNearestPlot();
            else if (total.magnitude < DpToPixels(gestureThresholdDp)) SelectPlot(position);
            // A gesture over the threshold that did not become a vertical drag is intentionally not a tap.
            gestureClaimed = false;
        }

        private void SelectPlot(Vector2 position)
        {
            Ray ray = worldCamera.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;
            MetaFactoryPlot plot = hit.collider.GetComponentInParent<MetaFactoryPlot>();
            if (plot != null) hud.SelectStation(plot.Station);
        }

        private void SnapToNearestPlot()
        {
            if (plots == null || plots.Length == 0) return;
            MetaFactoryPlot targetPlot = FindNearestVisiblePlot();
            if (targetPlot == null) return;
            float targetY = FindCameraYForPlotCenter(targetPlot);
            KillSnap();
            float fromY = worldCamera.transform.position.y;
            snapTween = DOTween.To(() => fromY, value =>
            {
                Vector3 camera = worldCamera.transform.position;
                camera.y = ClampCameraY(value);
                worldCamera.transform.position = camera;
            }, targetY, snapDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            snapTween.OnComplete(() =>
            {
                snapTween = null;
                float offset = worldCamera.transform.position.y - cameraHomeY;
                hud.CommitWorldSelection(targetPlot.Station, Mathf.Clamp(offset, minimumCameraY, maximumCameraY));
            });
            snapTween.SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void SnapImmediatelyToNearestPlot()
        {
            MetaFactoryPlot nearest = FindNearestVisiblePlot();
            if (nearest == null) return;
            Vector3 camera = worldCamera.transform.position;
            camera.y = FindCameraYForPlotCenter(nearest);
            worldCamera.transform.position = camera;
            hud.CommitWorldSelection(nearest.Station, Mathf.Clamp(camera.y - cameraHomeY, minimumCameraY, maximumCameraY));
        }

        private float FindCameraYForPlotCenter(MetaFactoryPlot plot)
        {
            float center = Screen.height * (viewportBottomFraction + viewportTopFraction) * .5f;
            float originalY = worldCamera.transform.position.y;
            float bestY = originalY;
            float bestError = float.MaxValue;
            const int samples = 97;
            for (int i = 0; i < samples; i++)
            {
                float offset = Mathf.Lerp(minimumCameraY, maximumCameraY, (float)i / (samples - 1));
                Vector3 camera = worldCamera.transform.position;
                camera.y = cameraHomeY + offset;
                worldCamera.transform.position = camera;
                Vector3 screen = worldCamera.WorldToScreenPoint(PlotAnchor(plot));
                float error = Mathf.Abs(screen.y - center);
                if (screen.z > 0f && error < bestError)
                {
                    bestError = error;
                    bestY = camera.y;
                }
            }
            Vector3 restored = worldCamera.transform.position;
            restored.y = originalY;
            worldCamera.transform.position = restored;
            return ClampCameraY(bestY);
        }

        private MetaFactoryPlot FindNearestVisiblePlot()
        {
            float bottom = Screen.height * viewportBottomFraction;
            float top = Screen.height * viewportTopFraction;
            float center = (bottom + top) * .5f;
            float nearestDistance = float.MaxValue;
            MetaFactoryPlot nearest = null;
            MetaFactoryPlot current = null;
            foreach (MetaFactoryPlot plot in plots)
            {
                if (plot == null || !plot.gameObject.activeInHierarchy) continue;
                Vector3 point = worldCamera.WorldToScreenPoint(PlotAnchor(plot));
                if (point.z <= 0f || point.y < bottom || point.y > top) continue;
                float distance = Mathf.Abs(point.y - center);
                if (distance < nearestDistance) { nearestDistance = distance; nearest = plot; }
                if (hud != null && plot.Station == hud.SelectedStation) current = plot;
            }
            if (current != null)
            {
                Vector3 point = worldCamera.WorldToScreenPoint(PlotAnchor(current));
                float currentDistance = Mathf.Abs(point.y - center);
                if (point.z > 0f && point.y >= bottom && point.y <= top && currentDistance <= nearestDistance + DpToPixels(selectionHysteresisDp))
                    return current;
            }
            if (nearest != null) return nearest;
            // At the hard ends of the range, choose the visible plot nearest the viewport.
            foreach (MetaFactoryPlot plot in plots)
            {
                if (plot == null || !plot.gameObject.activeInHierarchy) continue;
                Vector3 point = worldCamera.WorldToScreenPoint(PlotAnchor(plot));
                if (point.z <= 0f) continue;
                float distance = point.y < bottom ? bottom - point.y : point.y > top ? point.y - top : Mathf.Abs(point.y - center);
                if (distance < nearestDistance) { nearestDistance = distance; nearest = plot; }
            }
            return nearest;
        }

        private static Vector3 PlotAnchor(MetaFactoryPlot plot) => plot.transform.TransformPoint(new Vector3(0f, 1.1f, 0f));
        private float ClampCameraY(float value) => Mathf.Clamp(value, cameraHomeY + minimumCameraY, cameraHomeY + maximumCameraY);
        private float DpToPixels(float value) => value * (Screen.dpi > 0f ? Screen.dpi / 160f : Mathf.Max(.5f, Screen.height / 1920f));

        private bool IsPointerOverUi(int id, Vector2 position)
        {
            if (EventSystem.current == null) return false;
            PointerEventData pointer = new PointerEventData(EventSystem.current) { position = position };
            pointerRaycasts.Clear();
            EventSystem.current.RaycastAll(pointer, pointerRaycasts);
            for (int i = 0; i < pointerRaycasts.Count; i++)
                if (pointerRaycasts[i].module is GraphicRaycaster) return true;
            return id < 0 && EventSystem.current.IsPointerOverGameObject();
        }

        private void KillSnap()
        {
            if (snapTween == null) return;
            if (snapTween.IsActive()) snapTween.Kill();
            snapTween = null;
        }

        private void ValidateOneColumn()
        {
            MetaFactoryPlot[] found = plotsRoot.GetComponentsInChildren<MetaFactoryPlot>(true);
            if (found.Length != 3)
            {
                Debug.LogError("M05 world requires exactly three fixed production plots.", this);
                return;
            }
            System.Array.Sort(found, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            for (int i = 0; i < found.Length; i++)
                if (Mathf.Abs(found[i].transform.position.x - found[0].transform.position.x) > 0.01f ||
                    found[i].Station != (MetaStation)(2 - i))
                    Debug.LogError("M05 plots must form one vertical column in Garden, Dryer, Packer order.", this);
        }
    }
}
