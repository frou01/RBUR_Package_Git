
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BrakeConnectorValve : UdonSharpBehaviour
    {
        [SerializeField] CouplerObj coupler;

        void Start()
        {

            if (coupler == null)
            {
                coupler = transform.parent.gameObject.GetComponent<CouplerObj>();
            }
        }

        public override void Interact()
        {
            coupler.SendCustomNetworkEvent(NetworkEventTarget.Owner, "changeBrakeValve");
        }
    }
}
