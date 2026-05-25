
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    public class Knuckle : UdonSharpBehaviour
    {
        public CouplerObj coupler;
        void Start()
        {

        }
        public override void Interact()
        {
            //if(joint.connectedBody != dummy) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "disConnect");

            if (!coupler.Knuckle_Closed) coupler.SendCustomNetworkEvent(NetworkEventTarget.All, "knuckleClose");
            else if (coupler.state == 2) coupler.SendCustomNetworkEvent(NetworkEventTarget.All, "reLockCoupler");
            else coupler.SendCustomNetworkEvent(NetworkEventTarget.All, "knuckleOpen");
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (coupler)
            {
                Gizmos.color = new Color(0.4f, 0.2f, 0.2f, 0.5f);
                Gizmos.DrawSphere(transform.position, 0.1f);
                Gizmos.DrawLine(transform.position, coupler.transform.position);
            }
        }
#endif
    }
}
