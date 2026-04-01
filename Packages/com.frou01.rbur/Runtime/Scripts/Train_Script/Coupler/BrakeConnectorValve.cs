
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BrakeConnectorValve : UdonSharpBehaviour
    {
        [Header("auto assign on buildprocess (no overwrite)")]
        [SerializeField] protected CouplerObj coupler;

        [Header("auto assign on buildprocess (no overwrite)")]
        [SerializeField] protected AbstractBrake brakeModule;//Refered by BuildProcess

        public virtual void Init(Train train)
        {

            if (coupler == null)
            {
                coupler = transform.parent.gameObject.GetComponent<CouplerObj>();
            }
            if (brakeModule == null) brakeModule = (AbstractBrake)train.GetConnectionRecieverByTag("Brake");
        }

        public virtual void OpenBrakeValve()
        {
            brakeModule.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(brakeModule.openBrakeValve), coupler.FrontOrBack);
        }
        public virtual void CloseBrakeValve()
        {
            brakeModule.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(brakeModule.closeBrakeValve), coupler.FrontOrBack);
        }

        public override void Interact()
        {
            brakeModule.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(brakeModule.changeBrakeValve), coupler.FrontOrBack);
        }
#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (coupler)
            {
                Gizmos.color = new Color(0.2f, 0.2f, 0.4f, 0.5f);
                Gizmos.DrawSphere(transform.position, 0.1f);
                Gizmos.DrawLine(transform.position, coupler.transform.position);
            }
            else
            {
                if (transform.parent.gameObject.GetComponent<CouplerObj>())
                {
                    Gizmos.color = new Color(0.2f, 0.2f, 0.4f, 0.5f);
                    Gizmos.DrawSphere(transform.position, 0.1f);
                    Gizmos.DrawLine(transform.position, transform.parent.gameObject.GetComponent<CouplerObj>().transform.position);
                }
            }
        }
#endif
    }
}
