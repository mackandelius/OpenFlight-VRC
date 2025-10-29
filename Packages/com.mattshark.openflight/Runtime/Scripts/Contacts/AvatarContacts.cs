using System;

using UdonSharp;
using UnityEngine;

using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC.Contact
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AvatarContacts : CallbackUdonSharpBehaviour
    {
        /// <summary>
		/// The OpenFlight script, used to enable/disable flight
		/// </summary>
		public OpenFlight OpenFlight;

        /// <summary>
        /// The contact sender, for telling avatars that they are flying.
        /// </summary>
        public VRCContactSender Sender;

        /// <summary>
        /// The transform of the game object that the sender is attached to
        /// </summary>
        public Transform ThisObject;

        /// <summary>
        /// Stored value of whether the player is allowed to fly or not.
        /// </summary>
        private bool IsAllowedToFly;

        public void Start()
        {
            Sender.enabled = false;
        }

        internal void OnFlyingChanged(bool boolState)
        {
            Sender.enabled = boolState;
            Logger.Log("Avatar OF_IsFlying Contact " + boolState, this);
        }

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            string[] tags = contactInfo.matchingTags;

            bool canfly = false;
            bool cannotfly = false;

            // We can't use string.Contain which requires linq or string.exists which requires lambda functions, Udon doesn't expose either.
            foreach (var tag in tags)
            {
                if (tag == "OF_CanFly")
                {
                    canfly = true;
                    cannotfly = false;
                }
                else if (tag == "OF_CanNotFly")
                {
                    canfly = false;
                    cannotfly = true;
                }
            }

            if (canfly)
            {
                Logger.Log("Contact is activating flying", this);
                OpenFlight.CanFly();
                IsAllowedToFly = true;
            }
            else if (cannotfly)
            {
                Logger.Log("Contact is deactivating flying", this);
                OpenFlight.CannotFly();
                IsAllowedToFly = false;
            }
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            if (IsAllowedToFly)
            {
                Logger.Log("Contact stopped being detected, deactivating flying", this);
                OpenFlight.CannotFly();
            }
            else
            {
                Logger.Log("Contact stopped being detected, activating flying", this);
                OpenFlight.CanFly();
            }
        }
    }
}