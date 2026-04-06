
using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CouplerMiscSoundPlayer : UdonSharpBehaviour
{
    [SerializeField] AutoDisableSoundPlayer targetSource;
    CouplerObj coupler;
    ConfigurableJoint joint;
    [SerializeField] float pullThreshold;
    [SerializeField] float pushThreshold;
    [SerializeField] float pushStrongThreshold;
    [SerializeField] AudioClip pullClip;
    [SerializeField] AudioClip pushClip;
    [SerializeField] AudioClip pushStrongClip;
    private bool pull;
    private bool push;
    private bool pushStrong;
    [SerializeField] float cooltime = 1;
    [SerializeField] float shockThreshold = 2;

    Transform chachedTransform;
    float prevForce;
    float chachedDeltaTime;
    [UdonSynced] float syncedDeltaForce;
    float cnt;

    bool isOwner = true;


    void Start()
    {
        chachedTransform = this.transform;
        chachedDeltaTime = Time.fixedDeltaTime;
        coupler = this.gameObject.GetComponent<CouplerObj>();
        joint = coupler.TrainScript.GetComponents<ConfigurableJoint>()[coupler.FrontOrBack ? 0 : 1];
    }
    float force, DeltaForce;
    private void Update()
    {
        if (isOwner)
        {
            force = chachedTransform.InverseTransformVector(joint.currentForce).z;
            DeltaForce = (force - prevForce) / chachedDeltaTime;
            if (cnt < 0)
            {
                if (prevForce < shockThreshold && force > shockThreshold && DeltaForce > pullThreshold)
                {
                    //Debug.Log("pull f "+ force + " , pf " + prevForce + " , delta " + DeltaForce);
                    targetSource.Play(pullClip);
                    cnt = cooltime;
                    syncedDeltaForce = DeltaForce;
                    pull = true;
                    push = pushStrong = false;
                    RequestSerialization();
                }
                else if (prevForce > -shockThreshold && force < -shockThreshold && DeltaForce < pushStrongThreshold)
                {
                    //Debug.Log("pushStrong f " + force + " , pf " + prevForce + " , delta " + DeltaForce);
                    targetSource.Play(pushStrongClip);
                    cnt = cooltime;
                    syncedDeltaForce = DeltaForce;
                    pushStrong = true;
                    push = pull = false;
                    RequestSerialization();
                }
                else if (prevForce > -shockThreshold && force < -shockThreshold && DeltaForce < pushThreshold)
                {
                    //Debug.Log("push f " + force + " , pf " + prevForce + " , delta " + DeltaForce);
                    targetSource.Play(pushClip);
                    cnt = cooltime;
                    syncedDeltaForce = DeltaForce;
                    push = true;
                    pushStrong = pull = false;
                    RequestSerialization();
                }
            }
            else
            {
                cnt -= chachedDeltaTime;
            }
            prevForce = force;
        }
    }

    public override void OnDeserialization()
    {
        if (syncedDeltaForce > pullThreshold)
        {
            if (!pull)
            {
                targetSource.Play(pullClip);
                cnt = cooltime;
                pull = true;
            }
            push = pushStrong = false;
        }
        else if (syncedDeltaForce < pushStrongThreshold)
        {
            if (!pushStrong)
            {
                targetSource.Play(pushStrongClip);
                cnt = cooltime;
                pushStrong = true;
            }
            push = pull = false;
        }
        else if (syncedDeltaForce < pushThreshold)
        {
            if (!push)
            {
                targetSource.Play(pushClip);
                cnt = cooltime;
                push = true;
            }
            pushStrong = pull = false;
        }
        isOwner = false;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player.isLocal) isOwner = true;
        else isOwner = false;
    }
}
