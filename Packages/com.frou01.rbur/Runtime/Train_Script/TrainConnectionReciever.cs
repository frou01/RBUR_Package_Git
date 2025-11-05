
using frou01.RigidBodyTrain;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TrainConnectionReciever : UdonSharpBehaviour
{
    public string[] connectionTags;
    public virtual void TrainConnectionUpdate(Train connectedTrain, bool F_B)
    {

    }
}
