using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC;

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
        /// Stored value of whether the player is allowed to fly or not.
        /// </summary>
        private bool IsAllowedToFly;

        public FlightProperties FP;

        private VRCPlayerApi Localplayer;

        public void Start()
        {
            Sender.enabled = false;
            Localplayer = Networking.LocalPlayer;
        }

        internal void OnFlyingChanged(bool boolState)
        {
            Sender.enabled = boolState;
            Logger.Log("Avatar OF_IsFlying Contact " + boolState, this);
        }

        public override void OnContactEnter(ContactEnterInfo contactInfo)
        {
            //Check if local user contact
            if (Localplayer == contactInfo.contactSender.player)
            {
                string[] tags = contactInfo.matchingTags;

                bool canfly = false;
                bool cannotfly = false;

                // We can't use string.Contain which requires linq or string.exists which requires lambda functions, Udon doesn't expose either.
                foreach (var tag in tags)
                {
                    switch (tag)
                    {
                        case "OF_NotisOn":
                            //Forcefully turn on notification
                            FP.notifications = true;
                            break;

                        case "OF_NotisOff":
                            //Forcefully turn off notification
                            FP.notifications = false;
                            break;

                        case "OF_JumpFlyOn":
                            //Forcefully turn on jump to fly
                            FP.requireJump = true;
                            break;

                        case "OF_JumpFlyOff":
                            //Forcefully turn off jump to fly
                            FP.requireJump = false;
                            break;
                        
                        case "OF_BankingOn":
                            //Forcefully turn on banking
                            FP.bankingTurns = true;
                            break;

                        case "OF_BankingOff":
                            //Forcefully turn off banking
                            FP.bankingTurns = false;
                            break;

                        case "OF_WingOffsetMod":
                            //Tell system that the wing offset is being modified.
                            float contactX = Math.Abs(contactInfo.contactSender.position.x - this.transform.position.x);
                            float newoffset = Mathf.Lerp(0, 40, contactX);
                            FP.wingtipOffset = newoffset;
                            break;

                        case "OF_FlapStrengthMod":
                            //Tell system that flap strength is being modified
                            float contactY = Math.Abs(contactInfo.contactSender.position.y - this.transform.position.y);
                            int newstrength = (int)Mathf.Lerp(100, 800, contactY);
                            FP.flapStrengthBase = newstrength;
                            
                            break;
                        case "OF_FrictionMod":
                            //Tell system that friction is being modified
                            float contactZ = Math.Abs(contactInfo.contactSender.position.z - this.transform.position.z);
                            float newfriction = Mathf.Lerp(0.0f, 0.2f, contactZ);
                            FP.airFriction = newfriction;
                            break;

                        case "OF_CanFly":
                            canfly = true;
                            cannotfly = false;
                            break;

                        case "OF_CanNotFly":
                            canfly = false;
                            cannotfly = true;
                            break;
                    }
                    //if (tag == "OF_CanFly")
                    //{
                    //    canfly = true;
                    //    cannotfly = false;
                    //}
                    //else if (tag == "OF_CanNotFly")
                    //{
                    //    canfly = false;
                    //    cannotfly = true;
                    //}
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
        }

        public override void OnContactExit(ContactExitInfo contactInfo)
        {
            //Check if local user contact
            if (Localplayer == contactInfo.contactSender.player)
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
}