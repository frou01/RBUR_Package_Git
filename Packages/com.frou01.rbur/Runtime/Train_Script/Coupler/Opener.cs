
using UdonSharp;

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
    }
}
