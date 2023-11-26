
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Opener : UdonSharpBehaviour
    {
        public CouplerObj coupler;

        //public InputField text;

        private Train TrainScript;

        private bool FrontOrBack;


        void Start()
        {
            FrontOrBack = coupler.FrontOrBack;
            TrainScript = coupler.TrainScript;
        }

        //private void FixedUpdate()
        //{
        //    //if (started && (Networking.LocalPlayer == null || (transform.position - Networking.LocalPlayer.GetPosition()).sqrMagnitude < 16))
        //    //{
        //    //    text.text = "肘 : " + (coupler.Knuckle_Closed ? "閉" : "開") + "<br>錠 : ";
        //    //    {
        //    //        switch (coupler.state)
        //    //        {
        //    //            case 0:
        //    //                text.text += "錠掛け";
        //    //                break;
        //    //            case 1:
        //    //                text.text += "錠揚げ";
        //    //                break;
        //    //            case 2:
        //    //                text.text += "錠控え";
        //    //                break;
        //    //        }
        //    //    }
        //    //    {
        //    //        if (TrainScript != null && TrainScript.started)text.text += "<br>空制弁 : " + (TrainScript.BrakeOpen[FrontOrBack ? 0 : 1] ? "開" : "閉");
        //    //    }
        //    //}
        //}

        public override void Interact()
        {
            coupler.couplerUnlock();
            coupler.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "couplerUnlock");
        }
    }
}
