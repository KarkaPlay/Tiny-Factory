using UnityEngine;

namespace TinyFactory
{
    public sealed class MetaFactoryPlot : MonoBehaviour
    {
        [SerializeField] private MetaStation station;
        [SerializeField] private MetaFactoryRuntime runtime;
        [SerializeField] private GameObject stationVisual;
        [SerializeField] private GameObject lockedVisual;
        public MetaStation Station => station;
        public Transform StationVisual => stationVisual != null ? stationVisual.transform : transform;
        public Vector3 OutputAnchor => transform.TransformPoint(new Vector3(0f, 1.2f, -.7f));
        public Vector3 InputAnchor => transform.TransformPoint(new Vector3(0f, 1.2f, .7f));

        private void OnEnable()
        {
            if (runtime != null) runtime.Changed += Refresh;
            Refresh();
        }
        private void OnDisable() { if (runtime != null) runtime.Changed -= Refresh; }
        private void Refresh()
        {
            MetaFactoryRuntime.StationState state = runtime != null ? runtime.Station(station) : null;
            bool built = state != null && state.built;
            if (stationVisual != null) stationVisual.SetActive(built);
            if (lockedVisual != null) lockedVisual.SetActive(!built);
        }
    }
}
