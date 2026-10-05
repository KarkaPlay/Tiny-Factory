using UnityEngine;

namespace TinyFactory
{
    public sealed class FactoryItemVisual : MonoBehaviour
    {
        [SerializeField] private GameObject leafVisual;
        [SerializeField] private GameObject packagedVisual;
        
        private bool stateInitialized;
        private bool isPackaged;

        public void SetPackaged(bool packaged)
        {
            if (stateInitialized && isPackaged == packaged) return;

            stateInitialized = true;
            isPackaged = packaged;
            if (leafVisual != null) leafVisual.SetActive(!packaged);
            if (packagedVisual != null) packagedVisual.SetActive(packaged);
        }

        private void Awake()
        {
            if (leafVisual == null || packagedVisual == null)
                Debug.LogError("FactoryItemVisual requires leaf and packaged child references.", this);
            SetPackaged(false);
        }
    }
}