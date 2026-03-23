
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
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (coupler)
            {
                Gizmos.color = new Color(0.2f, 0.2f, 0.4f, 0.5f);
                Gizmos.DrawSphere(transform.position, 0.1f);
                Gizmos.DrawLine(transform.position, coupler.transform.position);
            }
        }
#endif
    }
}
