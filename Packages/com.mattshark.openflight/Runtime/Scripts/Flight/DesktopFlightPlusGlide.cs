
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC
{
    public class DesktopFlightPlusGlide : LoggableUdonSharpBehaviour
    {
        private VRCPlayerApi LocalPlayer;
        private Quaternion faceDirection;
        private bool sliding;
        private Vector3 currentVel;
        private Vector3 direction;
        public float speed;
        private float singleStep;
        private float oldGravity;
        public float gravity;
        private bool pressDown;
        public GameObject ThisGameObject;

        private Vector3 finalDir;
        private float turningSpeed = 0.0f;

        private bool glidesliding = false;
        private float dt;

        /// <summary>
		/// Udon behavior handling the contact system, for telling an avatar it is flying and an avatar telling OpenFlight it can fly.
		/// </summary>
		[Tooltip("Has to link to the correct contact udon behavior for contact detection and sending to work.")]
		public Contact.AvatarContacts AviContact;

        [FieldChangeCallback(nameof(isFlying))]
		private bool _isFlying = false;

        [HideInInspector]
		/// <summary> If true, the player is currently flying. </summary>
		public bool isFlying // Currently in the air after/during a flap
		{
			get { return _isFlying; }
			set
			{
				if (value == _isFlying)
				{
					return;
				}
				_isFlying = value;

				//forward the event to the AvatarContacts handler
				AviContact.OnFlyingChanged(_isFlying);
			}
		}

        void Start()
        {
            LocalPlayer = Networking.LocalPlayer;
            oldGravity = LocalPlayer.GetGravityStrength();

            if (LocalPlayer.IsUserInVR())
            {
                ThisGameObject.SetActive(false);
            }
        }

        public void OnDisable()
		{
			if (isFlying)
            {
                //Land();
                LocalPlayer.SetGravityStrength(oldGravity);
			}
			Logger.Log("Disabled.", this);
		}

        public void Update()
        {
            if (LocalPlayer.IsPlayerGrounded() && !pressDown)
            {
                LocalPlayer.SetGravityStrength(oldGravity);
                isFlying = false;
            }

            dt = Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                isFlying = true;
                glidesliding = true;
                //holdPos = LocalPlayer.GetPosition().y;

            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                glidesliding = false;
            }

            if (!LocalPlayer.IsPlayerGrounded())
            {

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    isFlying = true;
                    pressDown = true;
                    faceDirection = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                    currentVel = LocalPlayer.GetVelocity();
                    finalDir = Vector3.RotateTowards(currentVel, currentVel + (faceDirection * Vector3.forward) * speed, 0.1f, 10f);

                    //For infinite gliding
                    if (glidesliding)
                    {
                        finalDir.y = 0 + gravity / 10;
                    }
                    LocalPlayer.SetVelocity(finalDir);
                }
                else if ((Input.GetKey(KeyCode.Space) && pressDown) || glidesliding)
                {
                    isFlying = true;
                    //LocalPlayer.SetGravityStrength(gravity);
                    faceDirection = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                    currentVel = LocalPlayer.GetVelocity();
                    if (!glidesliding)
                    {
                        turningSpeed = 0.2f;
                        finalDir = Vector3.RotateTowards(currentVel, currentVel + (faceDirection * Vector3.forward), turningSpeed, 0.0f);
                    }
                    else
                    {

                        if (Input.GetKey(KeyCode.W))
                        {
                            finalDir += (faceDirection * Vector3.forward);
                        }
                        if (Input.GetKey(KeyCode.S))
                        {
                            finalDir += (faceDirection * Vector3.back);
                        }
                        if (Input.GetKey(KeyCode.A))
                        {
                            finalDir += (faceDirection * Vector3.left);
                        }
                        if (Input.GetKey(KeyCode.D))
                        {
                            finalDir += (faceDirection * Vector3.right);
                        }

                        finalDir = Vector3.RotateTowards(currentVel, currentVel + (finalDir * 10 * dt), Mathf.Deg2Rad * 45f, dt);

                        //For infinite gliding
                        finalDir.y = 0 + gravity / 10;

                        if (Input.GetKey(KeyCode.Space))
                        {
                            finalDir.y += 20 * dt;
                        }
                        if (Input.GetKey(KeyCode.LeftAlt))
                        {
                            finalDir.y -= 20 * dt;
                        }
                    }

                    LocalPlayer.SetVelocity(finalDir);
                }
                if (Input.GetKeyUp(KeyCode.Space) && !glidesliding)
                {
                    pressDown = false;
                    if (LocalPlayer.IsPlayerGrounded())
                    {
                        LocalPlayer.SetGravityStrength(oldGravity);
                    }
                }
            }
        }
    }
}