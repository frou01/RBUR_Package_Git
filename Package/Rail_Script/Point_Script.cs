
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Point_Script : UdonSharpBehaviour
    {
        public string sinroName1 = "本線";
        public string sinroName2 = "給水線";

        public bool changeType;//true:nextを変える false:prevを変える
        public bool changeType_sub;//true:nextを変える false:prevを変える
        public Rail_Script pointPrevRail;
        public Rail_Script pointPrevRail_sub;//X分岐用
        public Rail_Script pointNextRail1;
        public Rail_Script pointNextRail2;
        public TextMeshProUGUI display;

        private bool prevState;
        public Animator machineAnimator;

        [UdonSynced(UdonSyncMode.None)] public bool state;

        void Start()
        {
            if (changeType)
            {
                pointPrevRail.next = state ? pointNextRail1 : pointNextRail2;
            }
            else
            {
                pointPrevRail.prev = state ? pointNextRail1 : pointNextRail2;
            }
            if (pointPrevRail_sub != null)
            {
                if (changeType_sub)
                {
                    pointPrevRail_sub.next = state ? pointNextRail1 : pointNextRail2;
                }
                else
                {
                    pointPrevRail_sub.prev = state ? pointNextRail1 : pointNextRail2;
                }
            }
            if (display != null) display.text = state ? sinroName1 : sinroName2;

            if (machineAnimator == null) machineAnimator = gameObject.GetComponent<Animator>();
            changeValue();

        }

        public override void OnDeserialization()
        {
            changeValue();
        }

        private void changeValue()
        {
            if (display != null) display.text = state ? sinroName1 : sinroName2;

            //if (!pointPrevRail.started) pointPrevRail.Start();
            //if (pointPrevRail_sub != null && !pointPrevRail_sub.started) pointPrevRail_sub.Start();

            if (changeType)
            {
                pointPrevRail.next = state ? pointNextRail1 : pointNextRail2;
            }
            else
            {
                pointPrevRail.prev = state ? pointNextRail1 : pointNextRail2;
            }
            if (pointPrevRail_sub != null)
            {
                if (changeType_sub)
                {
                    pointPrevRail_sub.next = state ? pointNextRail1 : pointNextRail2;
                }
                else
                {
                    pointPrevRail_sub.prev = state ? pointNextRail1 : pointNextRail2;
                }
            }
            if (machineAnimator != null) machineAnimator.SetBool("state", state);
        }

        public override void Interact()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "change");
        }
        public void change()
        {
            state = !state;
            changeValue();
            this.RequestSerialization();
        }
    }
}
