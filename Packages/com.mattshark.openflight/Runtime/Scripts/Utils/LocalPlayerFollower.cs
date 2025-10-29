
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LocalPlayerFollower : UdonSharpBehaviour
{
    public Transform Target;

    public void Start()
    {
        if (Target == null)
        {
            Target = transform;
        }
    }
    public override void PostLateUpdate()
    {
        Target.position = Networking.LocalPlayer.GetPosition();
        Target.rotation = Networking.LocalPlayer.GetRotation();
    }
}
