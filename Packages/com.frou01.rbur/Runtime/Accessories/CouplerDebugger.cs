
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CouplerDebugger : UdonSharpBehaviour
    {
        [SerializeField]CouplerObj coupler;
        [SerializeField] TMP_Text display;
        private void Update()
        {
            display.text = "<color=white>肘 : " + (coupler.Knuckle_Closed ? "閉" : "開") + "<br>錠 : ";
            {
                switch (coupler.state)
                {
                    case 0:
                        display.text += "錠掛け";
                        break;
                    case 1:
                        display.text += "錠揚げ";
                        break;
                    case 2:
                        display.text += "錠控え";
                        break;
                }
            }
            {
                if (coupler.TrainScript != null && coupler.TrainScript.started)
                {
                    if (coupler.BrakeModule)
                    {
                        if (coupler.FrontOrBack)
                            display.text += "<br>空制弁 : " + (coupler.BrakeModule.BrakeOpenF ? "開" : "閉");
                        else
                            display.text += "<br>空制弁 : " + (coupler.BrakeModule.BrakeOpenB ? "開" : "閉");
                    }
                    display.text += "<br> debug_force" + (coupler.chachedTransform.InverseTransformVector(coupler.joint.currentForce) * 100000);
                }
            }
        }
    }
}
