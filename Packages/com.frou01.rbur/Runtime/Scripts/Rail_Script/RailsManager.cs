
using UdonSharp;
using Unity.Collections;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class RailsManager : UdonSharpBehaviour
    {

        [HideInInspector][SerializeField] public Rail_Script[] Rails;


        [Tooltip("maximum rail collider mesh divide length [m]")][SerializeField] public float railColliderMaxLength = 200;
        //[Tooltip("minimum rail collider face divide count")][SerializeField] public int railFaceMinDivideCount = 4;
        [Tooltip("minimum rail collider face divide count")][SerializeField] public int railFaceMaxDivide = 20;
        //[Tooltip("split rail collider threshold [m]")][SerializeField] public float railColliderSplitThreshold_Y = 0.1f;
        //[Tooltip("split rail collider threshold [m]")][SerializeField] public float railColliderSplitThreshold_X = 1.5f;
        //[Tooltip("split rail collider threshold [deg]")][SerializeField] public float railColliderSplitThreshold_Roll = 0.5f;//最適化は一旦見送り
        [Tooltip("rail collider face width [m]")][SerializeField] public float railFaceWidth = 2;

        [Tooltip("collider layer name. if not found, use \"Default\" layer")][SerializeField] public string railColliderLayerName = "RBUR_RailAndWheel";

        void Start()
        {
            this.enabled = false;
        }
    }
}
