
using UdonSharp;
using UnityEngine;

using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC.Net
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AvatarContacts : CallbackUdonSharpBehaviour
    {
        /// <summary>
		/// The OpenFlight script, used to enable/disable flight
		/// </summary>
		public OpenFlight OpenFlight;

        /// <summary>
		/// The WingFlightPlusGlide script, needed to set the flight properties
		/// </summary>
		public WingFlightPlusGlide WingFlightPlusGlide;

        /// <summary>
        /// The contact sender, for telling avatars that they are flying.
        /// </summary>
        public VRCContactSender Sender;

        public Transform ThisObject;

        //Test
        //public override void InputJump(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        //{
        //    WingFlightPlusGlide.isFlying = !WingFlightPlusGlide.isFlying;
        //}

        public void Start()
        {
            Sender.enabled = false;
        }

        internal void OnFlyingChanged(bool boolState)
        {
            Sender.enabled = boolState;
            Debug.Log("Avatar OF_IsFlying Contact " + boolState);
        }

        public override void PostLateUpdate()
        {
            ThisObject.position = Networking.LocalPlayer.GetPosition();
        }

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            Debug.Log("Activating flying");
            OpenFlight.CanFly();
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            Debug.Log("Deactivating flying");
            OpenFlight.CannotFly();
        }
    }
}