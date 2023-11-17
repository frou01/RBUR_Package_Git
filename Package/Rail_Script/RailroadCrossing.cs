
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class RailroadCrossing : UdonSharpBehaviour
    {
        public Animator[] animators;
        int inNum;
        void Start()
        {

        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<JointSoundPlayer>() != null)
            {
                inNum++;
                for (int id = 0; id < animators.Length; id++)
                {
                    animators[id].enabled = true;
                    animators[id].SetBool("trainCrossing", true);
                }
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.GetComponent<JointSoundPlayer>() != null)
            {
                inNum--;
                if (inNum <= 0)
                {
                    inNum = 0;
                    for (int id = 0; id < animators.Length; id++)
                    {
                        animators[id].SetBool("trainCrossing", false);
                    }
                }
            }
        }

        public void Interruption_True()
        {
            for (int id = 0; id < animators.Length; id++)
            {
                animators[id].SetBool("trainCrossing", true);
            }
        }
        public void Interruption_False()
        {
            if (inNum <= 0)
            {
                inNum = 0;
                for (int id = 0; id < animators.Length; id++)
                {
                    animators[id].SetBool("trainCrossing", false);
                }
            }
        }
    }
}
