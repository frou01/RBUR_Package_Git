
using TMPro;
using UdonSharp;
using UnityEngine;
namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CouplerDebugger : UdonSharpBehaviour
    {
        [SerializeField]CouplerObj coupler;
        [SerializeField] TMP_Text display;
        AbstractBrake Brake;
        private void Update()
        {
            display.text = "<color=white>Knuckle : " + (coupler.Knuckle_Closed ? "Close" : "Open") + "<br>Key : ";
            {
                switch (coupler.state)
                {
                    case 0:
                        display.text += "Lock";
                        break;
                    case 1:
                        display.text += "Open";
                        break;
                    case 2:
                        display.text += "Unlock";
                        break;
                }
            }
            {
                if (coupler.TrainScript != null)
                {
                    if(!Brake) Brake = (AbstractBrake)coupler.TrainScript.GetConnectionRecieverByTag("Brake");

                    if (Brake)
                    {
                        display.text += "<br> brake <br>" + Brake.ConnectionDebug(coupler.FrontOrBack);
                    }
                    display.text += "<br> debug_force" + (coupler.chachedTransform.InverseTransformVector(coupler.joint.currentForce)).ToString("000000.00");
                }
            }
        }
    }
}
