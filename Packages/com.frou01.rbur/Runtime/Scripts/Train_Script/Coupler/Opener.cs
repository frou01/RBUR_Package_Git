
using UdonSharp;
using UnityEngine;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Opener : UdonSharpBehaviour
    {
        public CouplerObj coupler;

        public override void Interact()
        {
            coupler.couplerUnlock();
            coupler.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "couplerUnlock");
        }
        public void Interact_()
        {
            coupler.couplerUnlock();
            coupler.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "couplerUnlock");
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (coupler)
            {
                Gizmos.color = new Color(0.2f, 0.4f, 0.2f, 0.5f);
                Gizmos.DrawSphere(transform.position, 0.1f);
                Gizmos.DrawLine(transform.position, coupler.transform.position);
            }
        }
#endif
    }
}
