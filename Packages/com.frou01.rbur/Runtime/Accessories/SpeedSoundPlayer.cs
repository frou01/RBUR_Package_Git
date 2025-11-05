
using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SpeedSoundPlayer : UdonSharpBehaviour
{
    [SerializeField] Train train;
    [SerializeField] AbstractBrake brakeModule;
    [SerializeField] AudioSource runningSound;
    [SerializeField] AudioSource brakingSound;
    [SerializeField] float speedMultiply;
    [SerializeField] float brakeMultiply;
    [SerializeField] float underThreshold;

    private float[] speed = new float[1];
    private float[] brake = new float[1];
    private float baseBrakePressure = 0f;
    bool playing;
    bool hasRunning = false;
    bool hasBrake = false;
    void Start()
    {
        speed = train.Rigidbody_Speed_LocalZ;
        brake = train.legacy_brakePressure_float;
        baseBrakePressure = train.baseBrakePressure;
        if(runningSound) hasRunning = true;
        if(brakingSound) hasBrake = true;
    }

    float brakePressure;
    float Speed;

    Vector3 prevPos;

    private void Update()
    {
        Speed = Mathf.Abs(speed[0]);
        if(underThreshold < Speed)
        {
            if (!playing)
            {
                if(hasRunning) runningSound.enabled = true;
                if(hasBrake) brakingSound.enabled = true;
                playing = true;
            }
            if (hasRunning)
            {
                runningSound.volume = Speed * speedMultiply;
                if (!runningSound.isPlaying) runningSound.Play();
            }

            if (hasBrake)
            {
                brakePressure = Mathf.Clamp01((baseBrakePressure - brake[0]) * 3.57f);
                if (brakePressure > 0.01f)
                {
                    brakingSound.volume = brakePressure * Speed * brakeMultiply;
                    if (!brakingSound.isPlaying) brakingSound.Play();
                }
                else
                {
                    if (brakingSound.isPlaying) brakingSound.Stop();
                }
            }
        }
        else if (playing)
        {
            if (hasRunning)
            {
                runningSound.Stop();
                runningSound.enabled = false;
            }
            if (hasBrake)
            {
                brakingSound.Stop();
                brakingSound.enabled = false;
            }
            playing = false;
        }
    }
}
