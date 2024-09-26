
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class JointSoundPlayer : UdonSharpBehaviour
    {
        public AudioSource sound;
        void Start()
        {
            this.enabled = false;
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<SoundDetector>() != null)
            {
                if (sound.gameObject.activeSelf)
                {
                    sound.enabled = true;
                    sound.Play();
                }
            }
        }
    }
}
