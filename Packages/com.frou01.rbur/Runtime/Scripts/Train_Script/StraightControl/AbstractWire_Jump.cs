using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
public class AbstractWire_Jump : UdonSharpBehaviour
    {
        [SerializeField] AbstractWire_Connector[] AttachedConnector;

        public void UpdateConnectorReference()
        {
            if (AttachedConnector[0])
            {
                AttachedConnector[0].setConnectedJumpCable(this);
            }
            if (AttachedConnector[1])
            {
                AttachedConnector[1].setConnectedJumpCable(this);
            }
        }
        public AbstractWire_Connector getOtherAttachedConnector(AbstractWire_Connector cameFrom)
        {
            if (cameFrom == AttachedConnector[0])
            {
                return AttachedConnector[1];
            }
            else if (cameFrom == AttachedConnector[1])
            {
                return AttachedConnector[0];
            }
            else
            {
                return null;
            }
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 1f, 0.2f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 1f, 0.2f, 1f);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
#endif
    }
}