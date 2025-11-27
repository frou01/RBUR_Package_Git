
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class FlangeSoundPlayer : UdonSharpBehaviour
{
    [SerializeField] Rigidbody trainBody;
    [SerializeField] Transform wheelBody_transform;
    [SerializeField] AudioSource FlangeSound;
    private bool FlangeSound_stop = false;

    [SerializeField] float DotThreshould = 0.0001f;
    [SerializeField] float MagnitudeThreshould = 1f;
    void Start()
    {
        
    }
    Vector3 trainVelocity;
    float flangeDot;
    float currentMagnitude;
    private void Update()
    {
        trainVelocity = trainBody.velocity;
        currentMagnitude = trainVelocity.magnitude;
        if (currentMagnitude > MagnitudeThreshould && currentMagnitude - (flangeDot = Mathf.Abs(Vector3.Dot(trainVelocity, wheelBody_transform.forward))) > DotThreshould)
        {
            playAudioSource((currentMagnitude - flangeDot - DotThreshould) * 2000);
        }
        else if (!FlangeSound_stop)
        {
            stopAudioSource();
        }
    }
    void playAudioSource(float volume)
    {
        if (FlangeSound_stop)
        {
            FlangeSound.enabled = true;
            FlangeSound.Play();
            FlangeSound_stop = false;
        }
        FlangeSound.volume = volume;
    }
    void stopAudioSource()
    {
        FlangeSound.Stop();
        FlangeSound.volume = 0;
        FlangeSound.enabled = false;
        FlangeSound_stop = true;
    }
}
