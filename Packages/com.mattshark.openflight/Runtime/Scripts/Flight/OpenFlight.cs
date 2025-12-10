/**
 * @ Maintainer: Mattshark89
 */

using UdonSharp;
using UnityEngine;
using Unity.Collections;
using VRC.SDKBase;

namespace OpenFlightVRC
{
	using UnityEditor;
	//This chunk of code allows the OpenFlight version number to be set automatically from the package.json file
	//its done using this method for dumb unity reasons but it works so whatever
#if !COMPILER_UDONSHARP && UNITY_EDITOR
	using UnityEditor.Callbacks;

	using VRC.SDKBase.Editor.BuildPipeline;

	public class OpenFlightScenePostProcessor
	{
		[PostProcessScene]
		public static void OnPostProcessScene()
		{
			//get the path of this script asset
			string guid = AssetDatabase.FindAssets(string.Format("t:Script {0}", typeof(OpenFlight).Name))[0];
			string path = AssetDatabase.GUIDToAssetPath(guid);

			//get the openflight package info
			UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);

			//find all the OpenFlight scripts in the scene
			OpenFlight[] openFlightScripts = Object.FindObjectsOfType<OpenFlight>();
			foreach (OpenFlight openFlightScript in openFlightScripts)
			{
				//set their version number
				openFlightScript.OpenFlightVersion = packageInfo.version;
			}
		}
	}

	public class OpenFlightChecker : VRC.SDKBase.Editor.BuildPipeline.IVRCSDKBuildRequestedCallback
	{
		public int callbackOrder => 0;

		public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
		{
			//check to make sure the world scale of openflight is 1
			OpenFlight[] openFlightScripts = Object.FindObjectsOfType<OpenFlight>();

			foreach (OpenFlight openFlightScript in openFlightScripts)
			{
				if (openFlightScript.transform.lossyScale != Vector3.one)
				{
					//show a popup
					EditorUtility.DisplayDialog("OpenFlight World Scale Error", "The world scale of the OpenFlight object must be 1.0. Please reset the scale of the OpenFlight object to 1.0.", "OK");

					Debug.LogError("OpenFlight: The world scale of the OpenFlight object must be 1.0. Please reset the scale of the OpenFlight object to 1.0.", openFlightScript);
					return false;
				}
			}

			return true;
		}
	}
#endif

	public enum FlightMode
	{
		Off,
		Auto,
		On
	}

	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class OpenFlight : LoggableUdonSharpBehaviour
	{
		//this removes any override that the editor might have set through the inspector ([HideInInspector] does NOT do that)
		/// <summary>
		/// The version of OpenFlight that is currently installed in the world. This should not be set, as this value is set upon scene load
		/// </summary>
		[System.NonSerialized]
		public string OpenFlightVersion = "?.?.?";

		/// <summary>
		/// 	The WingedFlight game object, used to enable/disable the WingedFlight script
		/// </summary>
		public GameObject wingedFlight;

		/// <summary>
		/// 	The Desktop Flight game object, used to enable/disable the DesktopFlight script
		/// </summary>
		public GameObject desktopFlight;

		/// <summary>
        /// 	All flight properties contained onto one object
        /// </summary>
		public FlightProperties FP;

		/// <summary>
		/// The AvatarDetection script, used to re-evaluate flight upon switching to auto
		/// </summary>
		public AvatarDetection avatarDetection;

		/// <inheritdoc cref="flightMode"/>
		[FieldChangeCallback(nameof(flightMode)), SerializeField]
		private FlightMode _flightMode = FlightMode.Auto;
		/// <summary>
		/// The current flight mode. Will not allow flight if the player is not in VR. Can be bypassed with <see cref="ignoreVRCheck"/>, but doing so is not recommended.
		/// </summary>
		public FlightMode flightMode
		{
			get => _flightMode;
			set
			{
				_flightMode = value;
				const string FLIGHTCANTBESETTEMPLATE = "Flight cannot be set to {0} because the player is not in VR";
				//update the flight mode string
				switch (value)
				{
					case FlightMode.Off:
						flightModeString = "Off";
						SwitchFlight(false);
						Logger.Log("Flight turned off", this);
						break;
					case FlightMode.Auto:
						//if (InVR())
						if (true)
						{
							flightModeString = "Auto";
							SwitchFlight(false);
							//tell the avatar detection script to check if the player can fly again
							if (avatarDetection != null)
							{
								avatarDetection.ReevaluateFlight();
							}
							Logger.Log("Flight set to auto", this);
						}
						else
						{
							flightMode = FlightMode.Off;
							Logger.Log(string.Format(FLIGHTCANTBESETTEMPLATE, "Auto"), this);
						}
						break;
					case FlightMode.On:
						if (true)
						//if (InVR())
						{
							flightModeString = "On";
							SwitchFlight(true);
							Logger.Log("Flight turned on", this);
						}
						else
						{
							flightMode = FlightMode.Off;
							Logger.Log(string.Format(FLIGHTCANTBESETTEMPLATE, "On"), this);
						}
						break;
					default:
						flightModeString = "Unknown";
						break;
				}
			}
		}

		/// <summary>
		/// The current flight mode as a string.
		/// </summary>
		[ReadOnlyInspector]
		public string flightModeString = "";

		private VRCPlayerApi _localPlayer;

		/// <inheritdoc cref="flightAllowed"/>
		[ReadOnly, ReadOnlyInspector, FieldChangeCallback(nameof(flightAllowed))]
		private bool _flightAllowed = false;
		/// <summary>
		/// If true, the player is allowed to fly
		/// </summary>
		public bool flightAllowed
		{
			get => _flightAllowed;
			set
			{
				_flightAllowed = value;

				//update the flight allowed string
				if (value)
				{
					flightAllowedString = "Active";
				}
				else
				{
					//if (InVR())
					if (true)
					{
						flightAllowedString = "Inactive";
					}
					else
					{
						flightAllowedString = "Inactive (Not in VR)";
					}
				}
			}
		}

		/// <summary
		/// Udon behavior handing HUD notification for telling if flight is possible or not.
		/// </summary>
		[Tooltip("Has to link to the correct contact udon behavior for contact detection and sending to work.")]
		public Hud.HudHandler HudNotificationHandler;

		[ReadOnlyInspector]
		public string flightAllowedString = "";

		/// <summary>
		/// If true, the system will ignore the VR check and allow flight even if the player is not in VR
		/// </summary>
		/// <remarks>
		/// You REALLY should not turn this on. This is purely for testing purposes
		/// </remarks>
		[ReadOnlyInspector]
		public bool ignoreVRCheck = true;

		/// <summary>
		/// Turns flight off
		/// </summary>
		/// <param name="value">If true, flight will be turned off</param>
		private void SwitchFlight(bool value)
		{
			if (InVR())
			{
				wingedFlight.SetActive(value);
				flightAllowed = value;
			}
			else
			{
				desktopFlight.SetActive(value);
				flightAllowed = value;
			}
		}

		/// <summary>
		/// Checks if the player is in VR
		/// </summary>
		/// <returns></returns>
		private bool InVR()
		{
			if (ignoreVRCheck)
			{
				Logger.LogWarning("VR check is being ignored! This should not be enabled in a production build!", this);
			}

			//ensure the user is valid
			if (_localPlayer == null)
			{
				_localPlayer = Networking.LocalPlayer;
			}

			return _localPlayer.IsUserInVR() || ignoreVRCheck;
			//return _localPlayer.IsUserInVR();
		}

		public void Start()
		{
			//update the flight mode string by setting the flight mode to itself to trigger the property setter
			flightMode = _flightMode;
			//update the flight allowed string aswell
			flightAllowed = _flightAllowed;

			_localPlayer = Networking.LocalPlayer;
			//if (!InVR())
			//{
			//	FlightOff();
			//}

			//apply flight mode
			switch (flightMode)
			{
				case FlightMode.On:
					FlightOn();
					break;
				case FlightMode.Off:
					FlightOff();
					break;
				case FlightMode.Auto:
					FlightAuto();
					break;
				default:
					Logger.LogWarning("Invalid flight mode: " + flightModeString, this);
					break;
			}

			Logger.Log("OpenFlight version " + OpenFlightVersion, this);
		}

		/// <summary>
		/// Enables flight if the player is in VR
		/// </summary>
		public void FlightOn()
		{
			flightMode = FlightMode.On;

			// TODO Check whether it should even pop up here.
			if (FP.notifications)
			{
				HudNotificationHandler.NotifyFlightCapable();
			}
		}

		/// <summary>
		/// Disables flight
		/// </summary>
		public void FlightOff()
		{
			flightMode = FlightMode.Off;

			// TODO Check whether it should even pop up here.
			if (FP.notifications)
			{
				HudNotificationHandler.NotifyNotFlightCapable();
			}
		}

		/// <summary>
		/// Allows the avatar detection system to control if the player can fly or not
		/// </summary>
		public void FlightAuto()
		{
			flightMode = FlightMode.Auto;
		}

		/// <summary>
		/// Allows flight if flightMode is set to Auto
		/// </summary>
		/// <seealso cref="FlightAuto"/>
		public void CanFly()
		{
			if (flightMode == FlightMode.Auto)
			{
				SwitchFlight(true);

				if (FP.notifications)
				{
					HudNotificationHandler.NotifyFlightCapable();
				}
			}
		}

		/// <summary>
		/// Disables flight if flightMode is set to Auto
		/// </summary>
		public void CannotFly()
		{
			if (flightMode == FlightMode.Auto)
			{
				SwitchFlight(false);

				if (FP.notifications)
				{
					HudNotificationHandler.NotifyNotFlightCapable();
				}
			}
		}
	}
}
