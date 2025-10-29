
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC.Hud
{
    public class HudHandler : UdonSharpBehaviour
    {
        public GameObject HudNotificationObject;

        public void NotifyFlightCapable()
        {
            HudNotificationObject.SetActive(true);
        }
        
        public void NotifyNotFlightCapable()
        {
            HudNotificationObject.SetActive(true);
        }
    }
}
