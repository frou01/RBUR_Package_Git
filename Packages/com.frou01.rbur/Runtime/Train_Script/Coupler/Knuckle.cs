
using UdonSharp;
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
    }
}
