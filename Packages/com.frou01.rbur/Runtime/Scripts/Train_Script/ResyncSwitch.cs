using UdonSharp;
using UnityEngine;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ResyncSwitch : UdonSharpBehaviour
    {
        [SerializeField] TrainManager trainManager;
        void Start()
        {
            InteractionText = "Resync";
        }

        public override void Interact()
        {
            trainManager.SendCustomEvent("ReSyncRequest");
        }
    }
}
