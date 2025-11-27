
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BrakeConnectorValve : UdonSharpBehaviour
    {
        [SerializeField] CouplerObj coupler;
        [SerializeField] public AbstractBrake brakeModule;//Refered by BuildProcess

        void Start()
        {

            if (coupler == null)
            {
                coupler = transform.parent.gameObject.GetComponent<CouplerObj>();
            }
        }

        public override void Interact()
        {
            brakeModule.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(brakeModule.changeBrakeValve), coupler.FrontOrBack);
        }
    }
}
