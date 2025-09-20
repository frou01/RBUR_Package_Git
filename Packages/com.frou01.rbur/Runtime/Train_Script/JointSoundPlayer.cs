
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

            if (sound.gameObject.activeSelf)
            {
                sound.enabled = true;
                sound.transform.position = this.transform.position;
                sound.Play();
            }
        }
    }
}
