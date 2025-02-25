/**
 * @ Maintainer: Mattshark89
 */

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace OpenFlightVRC
{
#if !COMPILER_UDONSHARP && UNITY_EDITOR // These using statements must be wrapped in this check to prevent issues on builds
using UnityEditor;
using UdonSharpEditor;
#endif

	// This is a custom inspector for the WingFlightPlusGlide script. It currently just adds a reset to defaults button
#if !COMPILER_UDONSHARP && UNITY_EDITOR
[CustomEditor(typeof(WingFlightPlusGlide))]
public class WingFlightPlusGlideEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WingFlightPlusGlide script = (WingFlightPlusGlide)target;

        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

        if (GUILayout.Button("Reset to Prefab Defaults"))
        {
            // Reset all values to the default in the prefab
            PrefabUtility.RevertObjectOverride(script, InteractionMode.AutomatedAction);
        }

        DrawDefaultInspector();
    }
}
#endif

	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class WingFlightPlusGlide : LoggableUdonSharpBehaviour
	{
		#region Settings
		/// <summary>
		/// Base strength of a flap. This value is affected by the avatar's armspan. See sizeCurve.
		/// </summary>
		/// <seealso cref="sizeCurve"/>
		[Header("Basic Settings")]
		// Both of these "base" values are by default affected by the avatar's armspan. See sizeCurve.
		[Tooltip("Want flaps to be stronger or weaker? Change this value first. (Default: 285)")]
		[Range(100, 500)]
		public int flapStrengthBase = 285;

		/// <inheritdoc cref="flapStrengthBase"/>
		int flapStrengthBase_DEFAULT = 285;

		/// <summary>
		/// Base gravity while flying.
		/// </summary>
		[Tooltip("Base gravity while flying (Default: 0.4)")]
		public float flightGravityBase = 0.4f;

		/// <inheritdoc cref="flightGravityBase"/>
		float flightGravityBase_DEFAULT = 0.4f;

		/// <summary>
		/// Require the player to jump before flapping can occur? Makes it less likely to trigger a flap by accident.
		/// </summary>
		[Tooltip("Require the player to jump before flapping can occur? Makes it less likely to trigger a flap by accident. (Default: true) CURRENTLY HAS NO EFFECT.")]
		public bool requireJump = true;

		/// <inheritdoc cref="requireJump"/>
		bool requireJump_DEFAULT = true;

		/// <summary>
		/// Allow locomotion (wasd/left joystick) while flying?
		/// </summary>
		[Tooltip("Allow locomotion (wasd/left joystick) while flying? (Default: false)")]
		public bool allowLoco = false;

		/// <inheritdoc cref="allowLoco"/>
		bool allowLoco_DEFAULT = false;

		/// <summary>
		/// Avatars using the avatar detection system may have wingtip, weight, etc. modifiers intended to personalize how they feel in the air. Set this value to true to use these modifiers or false if you want them disregarded for consistency. (Note: avatar size detection is not an Avatar Modifier; size-related calculations will always apply even if this setting is set to false.)
		/// </summary>
		[Tooltip(
			"Avatars using the avatar detection system may have wingtip, weight, etc. modifiers intended to personalize how they feel in the air. Set this value to true to use these modifiers or false if you want them disregarded for consistency. (Note: avatar size detection is not an Avatar Modifier; size-related calculations will always apply even if this setting is set to false.) (Default: true)"
		)]
		public bool useAvatarModifiers = true;

		/// <inheritdoc cref="useAvatarModifiers"/>
		bool useAvatarModifiers_DEFAULT = true;

		/// <summary>
		/// Allow gliding?
		/// </summary>
		[Tooltip("Allow gliding. (Default: true)")]
		public bool canGlide = true;

		/// <inheritdoc cref="canGlide"/>
		bool canGlide_DEFAULT = true;

		/// <summary>
		/// Avatars can glide directly from a fall without having to flap first. This behavior is more intuitive for gliding off cliffs, but may cause players to trigger gliding on accident more often when they just want to fall.
		/// </summary>
		[Tooltip(
			"Avatars can glide directly from a fall without having to flap first. This behavior is more intuitive for gliding off cliffs, but may cause players to trigger gliding on accident more often when they just want to fall. (Default: false)"
		)]
		public bool fallToGlide = true;

		/// <inheritdoc cref="fallToGlide"/>
		bool fallToGlide_DEFAULT = true;

		#region Advanced Settings
		/// <summary>
		/// Angle to offset the gliding direction by from your hands.
		/// </summary>
		[Header("Advanced Settings (Only for specialized use!)")]
		[Tooltip("Angle to offset the gliding direction by from your hands. (Default: 0)")]
		public float glideAngleOffset = 0f;

		/// <inheritdoc cref="glideAngleOffset"/>
		float glideAngleOffset_DEFAULT = 0f;

		/// <summary>
		/// How much Flap Strength and Flight Gravity are affected by an avatar's armspan. Default values will make smaller avis feel lighter and larger avis heavier.
		/// </summary>
		[Tooltip(
			"How much Flap Strength and Flight Gravity are affected by an avatar's armspan. Default values will make smaller avis feel lighter and larger avis heavier."
		)]
		public AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0.05f, 2), new Keyframe(1, 1), new Keyframe(20, 0.00195f));

		/// <summary>
		/// Modifier for horizontal flap strength. Makes flapping forwards easier.
		/// </summary>
		[Tooltip("Modifier for horizontal flap strength. Makes flapping forwards easier. (Default: 1.5)")]
		public float horizontalStrengthMod = 1.5f;

		/// <inheritdoc cref="horizontalStrengthMod"/>
		float horizontalStrengthMod_DEFAULT = 1.5f;

		/// <summary>
		/// How tight you want your turns while gliding. May be dynamically decreased by Avatar Modifier: weight.
		/// </summary>
		/// <remarks>
		/// Do not reduce this below 1; it will break under some weight values if you do
		/// </remarks>
		[Tooltip("How tight you want your turns while gliding. May be dynamically decreased by Avatar Modifier: weight. (Default: 2.5)")]
		[Range(1f, 5f)]
		public float glideControl = 2.5f; // Do not reduce this below 1; it will break under some weight values if you do

		/// <inheritdoc cref="glideControl"/>
		float glideControl_DEFAULT = 2.5f;

		/// <summary>
		/// Slows gliding down over time.
		/// </summary>
		[Tooltip("Slows gliding down over time. (Default: 0.02)")]
		[Range(0f, 0.2f)]
		public float airFriction = 0.02f;

		/// <inheritdoc cref="airFriction"/>
		float airFriction_DEFAULT = 0.02f;

		/// <summary>
		/// If enabled, flight gravity will use Gravity Curve's curve instead of Size Curve's curve multiplied by Flight Gravity Base.
		/// </summary>
		[Tooltip("If enabled, flight gravity will use Gravity Curve's curve instead of Size Curve's curve multiplied by Flight Gravity Base. (Default: false)")]
		public bool useGravityCurve = false;

		/// <inheritdoc cref="useGravityCurve"/>
		bool useGravityCurve_DEFAULT = false;

		/// <summary>
		/// Similar to Size Curve, but instead of modifying Flap Strength, it only affects Gravity. This value is ignored (Size Curve will be used instead) unless Use Gravity Curve is enabled.
		/// </summary>
		[Tooltip(
			"Similar to Size Curve, but instead of modifying Flap Strength, it only affects Gravity. This value is ignored (Size Curve will be used instead) unless Use Gravity Curve is enabled."
		)]
		public AnimationCurve gravityCurve = new AnimationCurve(new Keyframe(0.05f, 0.4f), new Keyframe(1, 0.2f), new Keyframe(20, 0.00039f));

		/// <summary>
		/// If a GameObject with a TextMeshPro component is attached here, debug some basic info into it. (Default: unset)
		/// </summary>
		[Tooltip("If a GameObject with a TextMeshPro component is attached here, debug some basic info into it. (Default: unset)")]
		public TextMeshProUGUI debugOutput;

		/// <summary>
		/// If enabled, banking to the left or right will force the player to rotate.
		/// </summary>
		/// <remarks>
		/// Can possibly cause network lag, but in testing it doesnt seem to.
		[Tooltip("Banking to the left or right will force the player to rotate. May cause network lag? (Default: true)")]
		public bool bankingTurns = true;

		/// <inheritdoc cref="bankingTurns"/>
		bool bankingTurns_DEFAULT = true;

		/// <summary>
		/// If enabled, gravity and movement will be saved each time the user takes off, instead of just at the start of the world.
		/// </summary>
		[Tooltip("If enabled, gravity and movement will be saved each time the user takes off, instead of just at the start of the world. (Default: false)")]
		public bool dynamicPlayerPhysics = false;

		/// <summary>
		/// Helper property. Do not remove the GameObject. If it somehow got unset, then set it to an empty game object (A Load Bearing Transform object should exist in the prefab for this purpose).
		/// </summary>
		[Header("Helper property (Do not touch, unless empty, then set to empty game object)")]
		[Tooltip("Do not remove, if empty add the 'Load Bearing' game object, script will fail at runtime without this setup.")]
		public Transform loadBearingTransform; //Transforms cannot be created, they can only be gotten from game objects, it isn't possible to create either in code.
		#endregion
		#endregion

		// State Control Variables
		/// <summary>
		/// Cached reference to the local player. This is set in Start() and should not be modified.
		/// </summary>
		private VRCPlayerApi LocalPlayer;

		/// <summary>
		/// The ticks per second in deltatime form.
		/// For example, a value of 0.02f would be 50 ticks per second, or 1/50.
		/// </summary>
		private float tps_dt = 0.05f; // The ticks per second in deltatime form. IE 0.02f would be 50 ticks per second, or 1/50

		/// <summary>
		/// The current time tick value.
		/// It cycles from 0 to 99 at a rate of 50 ticks per second.
		/// </summary>
		private int timeTick = -1; // -1 until the player is valid, then this value cycles from 0-99 at 50 ticks per second
		private Vector3 RHPos;
		private Vector3 LHPos;
		private Vector3 RHPosLast = new Vector3(0f, float.NegativeInfinity, 0f);
		private Vector3 LHPosLast = new Vector3(0f, float.NegativeInfinity, 0f);
		private Quaternion RHRot;
		private Quaternion LHRot;

		/// <summary>
		/// Determines whether the controllers are held outside of an imaginary cylinder.
		/// </summary>
		private bool handsOut = false; // Are the controllers held outside of an imaginary cylinder?

		/// <summary>
		/// Indicates whether the hands are in opposite positions.
		/// </summary>
		private bool handsOpposite = false;

		[HideInInspector]
		/// <summary> If true, the player is currently in the process of flapping. </summary>
		public bool isFlapping = false; // Doing the arm motion

		[HideInInspector]
		/// <summary> If true, the player is currently flying. </summary>
		public bool isFlying = false; // Currently in the air after/during a flap

		[HideInInspector]
		/// <summary> If true, the player is currently gliding. </summary>
		public bool isGliding = false; // Has arms out while flying

		/// <summary>
		/// If >0, disables flight then decreases itself by one
		/// </summary>
		private int cannotFlyTick = 0;

		/// <summary>
		/// Increased by one every tick one's y velocity < 0
		/// </summary>
		private int fallingTick = 0;
		private float dtFake = 0;

		// Variables related to Velocity
		private Vector3 finalVelocity; // Modify this value instead of the player's velocity directly, then run `setFinalVelocity = true`
		private bool setFinalVelocity;
		private Vector3 newVelocity; // tmp var
		private Vector3 targetVelocity; // tmp var, usually associated with slerping/lerping
		private float downThrust = 0f;
		private float flapAirFriction = 0.04f; // Prevents the gain of infinite speed while flapping. Set to 0 to remove this feature. THIS IS NOT A MAX SPEED

		// Variables related to gliding
		internal Vector3 wingDirection;
		private float steering;
		private bool spinningRightRound = false; // Can't get that Protogen animation out of my head
		private float rotSpeed = 0;
		private float rotSpeedGoal = 0;
		private float glideDelay = 0; // Minus one per tick, upon hitting ten gliding will gradually come into effect. Zero means gliding functions fully

		// "old" values are the world's defaults (recorded immediately before they are modified)
		private float oldGravityStrength;
		private float oldWalkSpeed;
		private float oldRunSpeed;
		private float oldStrafeSpeed;

		// Avatar-specific properties
		private HumanBodyBones rightUpperArmBone; // Bones won't be given a value until LocalPlayer.IsValid()
		private HumanBodyBones leftUpperArmBone;
		private HumanBodyBones rightLowerArmBone;
		private HumanBodyBones leftLowerArmBone;
		private HumanBodyBones rightHandBone;
		private HumanBodyBones leftHandBone;
		private float shoulderDistance = 0; // Distance between the two shoulders

		[HideInInspector]
		public float armspan = 1f;

		[Tooltip("Default avatar wingtipOffset. (Default: 0)")]
		public float wingtipOffset = 0;
		float wingtipOffset_DEFAULT = 0;

		[Tooltip("Default avatar weight. (Default: 1)")]
		[Range(0f, 2f)]
		public float weight = 1.0f;

		//Banking variables
		private Vector3 playerHolder;

		public void Start()
		{
			LocalPlayer = Networking.LocalPlayer;
			//save the user gravity if dynamic gravity is disabled
			if (!dynamicPlayerPhysics)
			{
				oldGravityStrength = LocalPlayer.GetGravityStrength();
				oldWalkSpeed = LocalPlayer.GetWalkSpeed();
				oldRunSpeed = LocalPlayer.GetRunSpeed();
				oldStrafeSpeed = LocalPlayer.GetStrafeSpeed();
				Logger.Log("Player Physics saved.", this);
			}
		}

		public void OnEnable()
		{
			timeTick = -20;
			isFlapping = false;
			isFlying = false;
			isGliding = false;
			spinningRightRound = false;
		}

		public void OnDisable()
		{
			if (isFlying)
			{
				Land();
			}
			Logger.Log("Disabled.", this);
		}

		public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float eyeHeight) // According to the docs, this also runs upon changing avatars
		{
			if (player == LocalPlayer)
			{
				// Bug: if avatar has been swapped, sometimes the player will be launched straight up.
				// Fix: while cannotFlyTick > 0, do not allow flying. Decreases by one each tick.
				cannotFlyTick = 20;
				setFinalVelocity = false;

				CalculateStats();
			}
		}

		public void Update()
		{
			// FixedUpdate()'s tick rate varies per VR headset.
			// Therefore, I am using Update() to create my own fake homebrew FixedUpdate()
			// It is called MainFlightTick()
			if ((LocalPlayer != null) && LocalPlayer.IsValid())
			{
				dtFake += Time.deltaTime;
				if (dtFake >= tps_dt)
				{
					dtFake -= tps_dt;
					MainFlightTick(tps_dt);
				}
			}
			// Banking turns should feel smooth since it's heavy on visuals. So this block exists in Update() instead of MainFlightTick()
			if (spinningRightRound)
			{
				// Avatar modifiers affect spin speed
				if (useAvatarModifiers)
				{
					rotSpeed += (rotSpeedGoal - rotSpeed) * Time.deltaTime * 6 * (1 - (weight - 1));
				}
				else
				{
					rotSpeed += (rotSpeedGoal - rotSpeed) * Time.deltaTime * 6;
				}

				// --- BEGIN MACKANDELIUS NO-JITTER BANKING TURNS FIX ---
				//Playspace origin and actual player position seems to work as parent and child objects,
				//therefore the conclusion is that we must make the playspace origin orbit the player.
				//
				//Caching positional data and modifying a virtual origin to be translated.
				loadBearingTransform.position = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
				loadBearingTransform.rotation = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation;
				playerHolder = LocalPlayer.GetPosition();

				//This function is strange.
				//I am in awe of the Unity engineers that had to fix the edge case of someone wanting to rotate the parent around a child.
				//Sure is useful in this case though.
				loadBearingTransform.RotateAround(playerHolder, Vector3.up, rotSpeed * Time.deltaTime);

				//Teleport based on playspace position, with an offset to place the player at the teleport location instead of the playspace origin.
				LocalPlayer.TeleportTo(
					playerHolder + (loadBearingTransform.position - playerHolder),
					loadBearingTransform.rotation,
					VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint,
					true
				);
				// --- END FIX ---
			}
		}

		private void MainFlightTick(float fixedDeltaTime)
		{
			if (timeTick < 0)
			{
				// This block only runs once shortly after joining the world
				timeTick = 0;
				leftLowerArmBone = HumanBodyBones.LeftLowerArm;
				rightLowerArmBone = HumanBodyBones.RightLowerArm;
				leftUpperArmBone = HumanBodyBones.LeftUpperArm;
				rightUpperArmBone = HumanBodyBones.RightUpperArm;
				leftHandBone = HumanBodyBones.LeftHand;
				rightHandBone = HumanBodyBones.RightHand;
				CalculateStats();
			}
			// Only affect velocity this tick if setFinalVelocity == true by the end
			setFinalVelocity = false;
			// Check if hands are being moved downward while above a certain Y threshold
			// We're using LocalPlayer.GetPosition() to turn these global coordinates into local ones
			RHPos = LocalPlayer.GetPosition() - LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
			LHPos = LocalPlayer.GetPosition() - LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
			LHRot = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
			RHRot = LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;

			downThrust = 0;
			if ((RHPos.y - RHPosLast.y) + (LHPos.y - LHPosLast.y) > 0)
			{
				downThrust = ((RHPos.y - RHPosLast.y) + (LHPos.y - LHPosLast.y)) * fixedDeltaTime / armspan;
			}

			// Check if player is falling
			if ((!LocalPlayer.IsPlayerGrounded()) && LocalPlayer.GetVelocity().y < 0)
			{
				fallingTick++;
			}
			else
			{
				fallingTick = 0;
			}

			// Hands are out if they are a certain distance from the torso
			handsOut = (
				Vector2.Distance(
					new Vector2(LocalPlayer.GetBonePosition(rightUpperArmBone).x, LocalPlayer.GetBonePosition(rightUpperArmBone).z),
					new Vector2(LocalPlayer.GetBonePosition(rightHandBone).x, LocalPlayer.GetBonePosition(rightHandBone).z)
				)
					> armspan / 3.3f
				&& Vector2.Distance(
					new Vector2(LocalPlayer.GetBonePosition(leftUpperArmBone).x, LocalPlayer.GetBonePosition(leftUpperArmBone).z),
					new Vector2(LocalPlayer.GetBonePosition(leftHandBone).x, LocalPlayer.GetBonePosition(leftHandBone).z)
				)
					> armspan / 3.3f
			);

			//if (Vector3.Angle(LHRot * Vector3.right, RHRot * Vector3.right) > 90)
			handsOpposite = (
				Vector3.Distance(LocalPlayer.GetBonePosition(leftHandBone), LocalPlayer.GetBonePosition(rightHandBone)) > (armspan / 3.3 * 2) + shoulderDistance
			);

			if (!isFlapping)
			{
				// Check for the beginning of a flap
				if (
					(isFlying || handsOut)
					// && (requireJump ? !LocalPlayer.IsPlayerGrounded() : true)
					&& (!LocalPlayer.IsPlayerGrounded())
					&& RHPos.y < LocalPlayer.GetPosition().y - LocalPlayer.GetBonePosition(rightUpperArmBone).y
					&& LHPos.y < LocalPlayer.GetPosition().y - LocalPlayer.GetBonePosition(leftUpperArmBone).y
					&& downThrust > 0.002f
				)
				{
					isFlapping = true;
					// TakeOff() will check !isFlying
					TakeOff();
				}
			}

			if (isFlapping)
			{
				FlapTick();
			}

			// See fallToGlide tooltip
			if (fallToGlide && fallingTick >= 20 && handsOut && handsOpposite && canGlide)
			{
				TakeOff();
			}

			// Flying starts when a player first flaps and ends when they become grounded
			if (isFlying)
			{
				FlyTick(fixedDeltaTime);
			}

			RHPosLast = RHPos;
			LHPosLast = LHPos;

			if (cannotFlyTick > 0)
			{
				setFinalVelocity = false;
				cannotFlyTick--;
			}

			if (setFinalVelocity)
			{
				LocalPlayer.SetVelocity(finalVelocity);
			}
		}

		/// <summary>
		/// Flying starts when a player first flaps and ends when they become grounded
		/// </summary>
		/// <param name="dt"></param>
		private void FlyTick(float dt)
		{
			// Check if FlyTick should be skipped this tick
			if (IsMainMenuOpen() || ((!isFlapping) && LocalPlayer.IsPlayerGrounded()))
			{
				Land();
			}
			else
			{
				// Ensure Gravity is correct
				if (LocalPlayer.GetGravityStrength() != GetFlightGravity() && LocalPlayer.GetVelocity().y < 0)
				{
					LocalPlayer.SetGravityStrength(GetFlightGravity());
				}

				// Check for a gliding pose
				// Verbose explanation: (Ensure you're not flapping) && (check for handsOut frame one, ignore handsOut afterwards) && Self Explanatory && Ditto
				if ((!isFlapping) && (isGliding || handsOut) && handsOpposite && canGlide)
				{
					// Forgot what this bugfixed
					if (LocalPlayer.GetVelocity().y > -1f && (!isGliding))
					{
						glideDelay = 3;
					}

					isGliding = true;
					newVelocity = setFinalVelocity ? finalVelocity : LocalPlayer.GetVelocity();

					if (glideDelay <= 1)
					{
						Vector3 newForwardRight = Quaternion.Euler(glideAngleOffset, 0, 0) * Vector3.forward;
						Vector3 newForwardLeft = Quaternion.Euler(-glideAngleOffset, 0, 0) * Vector3.forward;
						// wingDirection is a normal vector pointing towards the forward direction, based on arm/wing angle
						wingDirection = Vector3.Normalize(Vector3.Slerp(RHRot * newForwardRight, LHRot * newForwardLeft, 0.5f));
					}
					else
					{
						wingDirection = newVelocity.normalized;
						glideDelay -= 5 * dt;
					}

					// Bug: In rare cases (more common with extremely small avatars) a player's velocity is perfectly straight up/down, which breaks gliding
					// Fix: Always have some form of horizontal velocity while falling.
					if (newVelocity.y < 0.3f && newVelocity.x == 0 && newVelocity.z == 0)
					{
						Vector2 tmpV2 = new Vector2(wingDirection.x, wingDirection.z).normalized * 0.145f;
						newVelocity = new Vector3(Mathf.Round(tmpV2.x * 10) / 10, newVelocity.y, Mathf.Round(tmpV2.y * 10) / 10);
					}

					steering = (RHPos.y - LHPos.y) * 80 / armspan;
					//clamp steering to 45 degrees
					steering = Mathf.Clamp(steering, -45, 45);

					if (bankingTurns)
					{
						// "Where's the logic for banking turns?" See Update()
						spinningRightRound = true;
						rotSpeedGoal = steering;
					}
					else
					{
						// Fallback "banking" which is just midair strafing. Nobody likes how this feels, should depreciate it
						wingDirection = Quaternion.Euler(0, steering, 0) * wingDirection;
					}

					// Favoring Fun over Realism
					// Verbose: X and Z are purely based on which way the wings are pointed ("forward") instead of calculating how the wind would hit each wing, for ease of VR control
					targetVelocity = Vector3.ClampMagnitude(newVelocity + (Vector3.Normalize(wingDirection) * newVelocity.magnitude), newVelocity.magnitude);

					float newGlideControl = (useAvatarModifiers && weight > 1) ? glideControl - ((weight - 1) * 0.6f) : glideControl;
					if (glideDelay > 0)
					{
						glideDelay -= 5 * dt;
					}
					newGlideControl *= (1 - glideDelay) / 1;

					finalVelocity = Vector3.Slerp(newVelocity, targetVelocity, dt * newGlideControl);

					// Apply Air Friction
					finalVelocity *= 1 - (airFriction * 0.011f);
					setFinalVelocity = true;
				}
				else // Not in a gliding pose?
				{
					isGliding = false;
					rotSpeedGoal = 0;
					glideDelay = 0;
				}
			}
		}

		/// <summary>
		/// Flapping starts when a player first flaps and ends when they stop flapping. FlapTick will run every tick.
		/// </summary>
		private void FlapTick()
		{
			if (downThrust > 0)
			{
				// Calculate force to apply based on the flap
				newVelocity = 0.011f * GetFlapStrength() * ((RHPos - RHPosLast) + (LHPos - LHPosLast));
				if (LocalPlayer.IsPlayerGrounded())
				{
					// Prevents skiing along the ground
					newVelocity = new Vector3(0, newVelocity.y, 0);
				}
				else
				{
					newVelocity.Scale(new Vector3(horizontalStrengthMod, 1, horizontalStrengthMod));
				}
				finalVelocity = LocalPlayer.GetVelocity() + newVelocity;
				// Speed cap (check, then apply flapping air friction)
				if (finalVelocity.magnitude > 0.02f * GetFlapStrength())
				{
					finalVelocity = finalVelocity.normalized * (finalVelocity.magnitude - (flapAirFriction * GetFlapStrength() * 0.011f));
				}
				setFinalVelocity = true;
			}
			else
			{
				// Bug: Stations store your velocity, releasing it all at once when you hop off. Meaning you can flap while seated to infinitely build velocity.
				// Fix: set velocity to zero if grounded. Unfortunately breaks the RequireJump() setting, which will be refactored in the future.
				if (LocalPlayer.IsPlayerGrounded())
				{
					finalVelocity = Vector3.zero;
					setFinalVelocity = true;
				}
				isFlapping = false;
			}
		}

		public void FixedUpdate()
		{
			if (timeTick >= 0)
			{
				timeTick++;
				// Automatically update the debug output every 0.2 seconds (sorta, since certain VR headsets affect FixedUpdate())
				if (timeTick > 9)
				{
					timeTick = 0;
					if (debugOutput != null)
					{
						debugOutput.text =
							string.Concat("\nIsFlying: ", isFlying.ToString())
							+ string.Concat("\nIsFlapping: ", isFlapping.ToString())
							+ string.Concat("\nIsGliding: ", isGliding.ToString())
							+ string.Concat("\nHandsOut: ", handsOut.ToString())
							+ string.Concat("\nDownThrust: ", downThrust.ToString())
							+ string.Concat("\nCannotFly: ", (cannotFlyTick > 0).ToString())
							+ string.Concat("\nGlideDelay: ", glideDelay.ToString())
							+ string.Concat("\ngrounded: ", LocalPlayer.IsPlayerGrounded())
							+ string.Concat("\nYmagnitude: ", LocalPlayer.GetVelocity().y.ToString());
					}
				}
			}
		}

		/// <summary>
		/// Immobilizes the player's locomotion. This is useful for preventing the player from moving while flying. Still allows the player to rotate, unlike VRC's method of immobilization.
		/// </summary>
		/// <param name="immobilize"></param>
		private void ImmobilizePart(bool immobilize)
		{
			if (immobilize)
			{
				if (dynamicPlayerPhysics)
				{
					oldWalkSpeed = LocalPlayer.GetWalkSpeed();
					oldRunSpeed = LocalPlayer.GetRunSpeed();
					oldStrafeSpeed = LocalPlayer.GetStrafeSpeed();
				}
				LocalPlayer.SetWalkSpeed(0.001f);
				LocalPlayer.SetRunSpeed(0.001f);
				LocalPlayer.SetStrafeSpeed(0.001f);
				return;
			}

			LocalPlayer.SetWalkSpeed(oldWalkSpeed);
			LocalPlayer.SetRunSpeed(oldRunSpeed);
			LocalPlayer.SetStrafeSpeed(oldStrafeSpeed);
		}

		/// <summary>
		/// Running this function will recalculate important variables needed for Flap Strength.
		/// </summary>
		private void CalculateStats()
		{
			// `armspan` does not include the distance between shoulders. shoulderDistance stores this value by itself.
			armspan =
				Vector3.Distance(LocalPlayer.GetBonePosition(leftUpperArmBone), LocalPlayer.GetBonePosition(leftLowerArmBone))
				+ Vector3.Distance(LocalPlayer.GetBonePosition(leftLowerArmBone), LocalPlayer.GetBonePosition(leftHandBone))
				+ Vector3.Distance(LocalPlayer.GetBonePosition(rightUpperArmBone), LocalPlayer.GetBonePosition(rightLowerArmBone))
				+ Vector3.Distance(LocalPlayer.GetBonePosition(rightLowerArmBone), LocalPlayer.GetBonePosition(rightHandBone));
			shoulderDistance = Vector3.Distance(LocalPlayer.GetBonePosition(leftUpperArmBone), LocalPlayer.GetBonePosition(rightUpperArmBone));
			Logger.Log("Armspan: " + armspan.ToString() + " Shoulder Distance: " + shoulderDistance.ToString(), this);
		}

		/// <summary>
		/// Set necessary values for beginning flight. Automatically ensures it only runs on the first tick of flight.
		/// </summary>
		public void TakeOff()
		{
			if (!isFlying)
			{
				isFlying = true;
				if (dynamicPlayerPhysics)
				{
					oldGravityStrength = LocalPlayer.GetGravityStrength();
				}
				else
				{
					CheckPhysicsUnChanged();
				}
				LocalPlayer.SetGravityStrength(GetFlightGravity());
				if (!allowLoco)
				{
					ImmobilizePart(true);
				}
				Logger.Log("Took off.", this);
			}
		}

		/// <summary>
		/// Checks if the world gravity or player movement has changed from the saved values and throws a warning if so.
		/// </summary>
		private void CheckPhysicsUnChanged()
		{
			// Log a warning if gravity values differ from what we have saved
			if (LocalPlayer.GetGravityStrength() != oldGravityStrength)
			{
				Logger.LogWarning(
					"World gravity is different than the saved gravity, this may cause issues. If you want to avoid this, edit scripts to inform OpenFlight of the new world gravity using UpdatePlayerPhysics().",
					this
				);
				Logger.LogWarning("Saved Gravity: " + oldGravityStrength.ToString(), this);
			}

			// Log a warning if movement values differ from what we have saved
			if (LocalPlayer.GetWalkSpeed() != oldWalkSpeed || LocalPlayer.GetRunSpeed() != oldRunSpeed || LocalPlayer.GetStrafeSpeed() != oldStrafeSpeed)
			{
				Logger.LogWarning(
					"Player movement is different than the saved movement, this may cause issues. If you want to avoid this, edit scripts to inform OpenFlight of the new player movement using UpdatePlayerPhysics().",
					this
				);
				Logger.LogWarning(
					"Saved Walk Speed: " + oldWalkSpeed.ToString() + " Saved Run Speed: " + oldRunSpeed.ToString() + " Saved Strafe Speed: " + oldStrafeSpeed.ToString(),
					this
				);
			}
		}

#pragma warning disable IDE0044 // Add readonly modifier. We want functions to be able to modify this
		private Collider[] _colliders = new Collider[0];
#pragma warning restore IDE0044 // Add readonly modifier
		/// <summary>
		/// Utility method to detect main menu status. Technique pulled from <see href="https://github.com/Superbstingray/UdonPlayerPlatformHook">UdonPlayerPlatformHook</see>
		/// </summary>
		/// <returns>True if the main menu is open, false otherwise</returns>
		private bool IsMainMenuOpen()
		{
			//swapped from OverlapSphere to OverlapSphereNonAlloc, as it does not allocate memory each time it is called,
			//saving on garbage collection. Also doesnt require a .Length check, as it returns the number of colliders it found inherently.
			int uiColliderCount = Physics.OverlapSphereNonAlloc(LocalPlayer.GetPosition(), 10f, _colliders, 524288);
			//commented out due to extern count, this uses 3
			//return uiColliderCount == 8 || uiColliderCount == 9 || uiColliderCount == 10;

			//this uses 2 externs
			return 8 <= uiColliderCount && uiColliderCount <= 10;
		}

		/// <summary>
		/// Effectually disables all flight-related variables and functions. This does not permanently disable flight (the player can just flap again); disable the GameObject instead.
		/// </summary>
		public void Land()
		{
			isFlying = false;
			isFlapping = false;
			isGliding = false;
			spinningRightRound = false;
			rotSpeed = 0;
			rotSpeedGoal = 0;
			LocalPlayer.SetGravityStrength(oldGravityStrength);
			if (!allowLoco)
			{
				ImmobilizePart(false);
			}
			if (!dynamicPlayerPhysics)
			{
				CheckPhysicsUnChanged();
			}
			Logger.Log("Landed.", this);
		}

		private float GetFlapStrength()
		{
			if (useAvatarModifiers)
			{
				// default settings
				return sizeCurve.Evaluate(armspan) * (flapStrengthBase + (wingtipOffset * 8));
			}

			return sizeCurve.Evaluate(armspan) * flapStrengthBase + 10;
		}

		private float GetFlightGravity()
		{
			float gravity = 0;
			if (useGravityCurve)
			{
				gravity = gravityCurve.Evaluate(armspan) * armspan;
			}
			else
			{
				// default settings
				gravity = sizeCurve.Evaluate(armspan) * flightGravityBase * armspan;
			}

			if (useAvatarModifiers)
			{
				// default settings
				return gravity * weight;
			}
			return gravity;
		}

		/// <summary>
		/// Calling this function tells the script to pull in the worlds values for player physics. This is useful if you have a world that changes gravity or movement often, but still want water systems to work.
		/// </summary>
		/// <remarks>
		/// This function is only useful if dynamic player physics is disabled. Otherwise, it will do nothing.
		/// </remarks>
		public void UpdatePlayerPhysics()
		{
			if (dynamicPlayerPhysics)
			{
				Logger.Log("Dynamic Player Physics is enabled. Player Physics will be updated automatically.", this);
				return;
			}

			oldGravityStrength = LocalPlayer.GetGravityStrength();
			oldWalkSpeed = LocalPlayer.GetWalkSpeed();
			oldRunSpeed = LocalPlayer.GetRunSpeed();
			oldStrafeSpeed = LocalPlayer.GetStrafeSpeed();
			Logger.Log("Player Physics updated.", this);
		}

		/// <summary>
		/// Initializes all default values. This should not be called by end users in most cases.
		/// </summary>
		public void InitializeDefaults()
		{
			flapStrengthBase_DEFAULT = flapStrengthBase;
			flightGravityBase_DEFAULT = flightGravityBase;
			requireJump_DEFAULT = requireJump;
			allowLoco_DEFAULT = allowLoco;
			useAvatarModifiers_DEFAULT = useAvatarModifiers;
			wingtipOffset_DEFAULT = wingtipOffset;
			canGlide_DEFAULT = canGlide;
			fallToGlide_DEFAULT = fallToGlide;
			horizontalStrengthMod_DEFAULT = horizontalStrengthMod;
			glideControl_DEFAULT = glideControl;
			airFriction_DEFAULT = airFriction;
			useGravityCurve_DEFAULT = useGravityCurve;
			bankingTurns_DEFAULT = bankingTurns;
			glideAngleOffset_DEFAULT = glideAngleOffset;
			Logger.Log("Defaults initialized.", this);
		}

		/// <summary>
		/// Restores all values to their prefab defaults
		/// </summary>
		public void RestoreDefaults()
		{
			flapStrengthBase = flapStrengthBase_DEFAULT;
			flightGravityBase = flightGravityBase_DEFAULT;
			requireJump = requireJump_DEFAULT;
			allowLoco = allowLoco_DEFAULT;
			useAvatarModifiers = useAvatarModifiers_DEFAULT;
			wingtipOffset = wingtipOffset_DEFAULT;
			canGlide = canGlide_DEFAULT;
			fallToGlide = fallToGlide_DEFAULT;
			horizontalStrengthMod = horizontalStrengthMod_DEFAULT;
			glideControl = glideControl_DEFAULT;
			airFriction = airFriction_DEFAULT;
			useGravityCurve = useGravityCurve_DEFAULT;
			bankingTurns = bankingTurns_DEFAULT;
			glideAngleOffset = glideAngleOffset_DEFAULT;
			Logger.Log("Defaults restored.", this);
		}
	}
}
