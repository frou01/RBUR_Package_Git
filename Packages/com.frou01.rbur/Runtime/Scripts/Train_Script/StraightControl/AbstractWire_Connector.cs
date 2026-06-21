using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class AbstractWire_Connector : UdonSharpBehaviour
    {
        [SerializeField] AbstractWire_OnTrain ParentWire_OnTrain;
        [SerializeField] AbstractWire_Jump ConnectedJumpWire;
        [SerializeField] AbstractWire_Connector ConnectedConnector;

        public AbstractWire_OnTrain getOtherConnectedWire(AbstractWire_OnTrain cameFrom)
        {
            if (ConnectedJumpWire != null)
            {
                ConnectedConnector = ConnectedJumpWire.getOtherAttachedConnector(this);
            }
            if (ConnectedConnector) return ConnectedConnector.getParentWire();
            return null;
        }

        public void setConnectedJumpCable(AbstractWire_Jump connectedJumpWire)
        {
            this.ConnectedJumpWire = connectedJumpWire;
            ParentWire_OnTrain.onChangeConnection();
        }

        public AbstractWire_OnTrain getParentWire()
        {
            return ParentWire_OnTrain;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
#endif
    }
}