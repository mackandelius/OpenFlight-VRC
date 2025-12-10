
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC.Hud
{
    public class HudHandler : UdonSharpBehaviour
    {
        public GameObject HudNotificationObject;

        private Material HudMaterial;

        void Start()
        {
            HudMaterial = HudNotificationObject.GetComponent<Renderer>().material;
        }

        public void NotifyFlightCapable()
        {
            HudMaterial.SetInt("_SwapState", 1);
            HudNotificationObject.SetActive(true);
        }
        
        public void NotifyNotFlightCapable()
        {
            HudMaterial.SetInt("_SwapState", 0);
            HudNotificationObject.SetActive(true);
        }
    }
}
