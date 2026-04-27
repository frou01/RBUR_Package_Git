using Cinemachine;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace frou01.RigidBodyTrain
{
    [DefaultExecutionOrder(10)]
    public class Bogie_Script : UdonSharpBehaviour
    {
        //PresetByBuildProcess
        [HideInInspector] private TrainManager trainManager;
        [HideInInspector] private RailsManager railsManager;
        [HideInInspector] public Train ParentTrain;
        //Preset reference
        [SerializeField] private Transform m_Bogie;
        [SerializeField] private Rigidbody m_UnderRail;
        [HideInInspector][SerializeField] private Transform m_UnderRailTransform;
        private int pathResolution = 3;

        //Cached Rail Properties
        private int RailID;
        private CinemachinePathBase BogieCinemachinePath;
        private float railMaxPoint;
        private Vector3 RailStart_LocalPosition;
        private Vector3 RailEnd_LocalPosition;

        //Simulation Params
        [SerializeField] private Rail_Script BogieRail;
        private float onRailPoint_PathUnit;
        private float BogieToUnderRailLength;
        private bool tooLongDiffF;
        private Vector3 UnderRail_LocalPosition;
        private Vector3 CachedBogieLocalPosition;
        private bool isDirty;
        private bool orProxy;

        private Vector3 tempVector;
        private Vector3 chachedZero = Vector3.zero;
        private float[] distanceErrorThreshold = new float[1];


        private float nextSync = 1;
        private float fromLastSync;
        public bool InitsyncRecieveMode = true;
        bool isOwnerState;
        [UdonSynced] private bool isDiscontinuitySync;
        [UdonSynced] private float syncInterval = 1;//4byte
        [UdonSynced] int SyncedRailID;//4byte
        [UdonSynced] float SyncedRailPoint_PathUnit;//4byte

        public Transform Bogie { get => m_Bogie;}
        public Rigidbody UnderRail { get => m_UnderRail;}

        public void BogieInit()
        {
            m_UnderRailTransform = UnderRail.transform;
            railsManager = ParentTrain.railsManager;
            trainManager = ParentTrain.trainManager;
            distanceErrorThreshold = ParentTrain.distanceErrorThreshold;
            copyRailProperties();
            onRailPoint_PathUnit = BogieRail.GetF(m_Bogie.position);
            ApplyRailPointToTransform();
            UnderRail_LocalPosition = m_UnderRailTransform.localPosition;
            pathResolution = trainManager.pathRes;
        }


        public void copyRailProperties()
        {
            BogieCinemachinePath = BogieRail.cinemachinePath;
            m_UnderRailTransform.SetParent(BogieRail.cinemachinePath.transform);
            m_Bogie.SetParent(BogieRail.cinemachinePath.transform);
            UnderRail_LocalPosition = m_UnderRailTransform.localPosition;

            RailStart_LocalPosition = BogieCinemachinePath.EvaluateLocalPosition(BogieCinemachinePath.MinPos);
            RailEnd_LocalPosition = BogieCinemachinePath.EvaluateLocalPosition(BogieCinemachinePath.MaxPos);
            railMaxPoint = BogieCinemachinePath.MaxPos;
            RailID = BogieRail.RailID;
        }
        public void ApplyRailPointToTransform()
        {
            m_UnderRailTransform.localPosition = UnderRail_LocalPosition = BogieCinemachinePath.EvaluateLocalPosition(onRailPoint_PathUnit);
            m_UnderRailTransform.localRotation = BogieCinemachinePath.EvaluateLocalOrientation(onRailPoint_PathUnit);

            tempVector = UnderRail_LocalPosition - CachedBogieLocalPosition;
            BogieToUnderRailLength = tempVector.sqrMagnitude;
            tooLongDiffF = BogieToUnderRailLength > distanceErrorThreshold[0] * 5;
        }
        private void changeRail(bool dir)
        {
            if (dir)
            {
                if (BogieRail.next != null)
                {
                    Rail_Script NextRail = BogieRail.next;
                    BogieRail = NextRail;
                    copyRailProperties();
                }
                else return;
            }
            else
            {
                if (BogieRail.prev != null)
                {
                    Rail_Script NextRail = BogieRail.prev;
                    BogieRail = NextRail;
                    copyRailProperties();
                }
                else return;
            }
            onRailPoint_PathUnit = BogieCinemachinePath.FindClosestPoint(m_Bogie.position, 0, -1, 10);
            ApplyRailPointToTransform();
        }

        void FixedUpdate()
        {
            CachedBogieLocalPosition = m_Bogie.localPosition;
            orProxy = false;
            //Get Next onRailPoint

            if (tooLongDiffF)
            {
                //移動するレール上か、漸近探索に失敗して距離が離れている状況
                //Use findClosest
                isDirty = true;
                onRailPoint_PathUnit = BogieCinemachinePath.FindClosestPoint(m_Bogie.position, (int)onRailPoint_PathUnit, 1, pathResolution);
                Debug.Log("Use FindClosest");
            }
            else if (Vector3.Distance(CachedBogieLocalPosition, UnderRail_LocalPosition) > distanceErrorThreshold[0])
            {
                //Use tangent algorithm
                isDirty = true;
                tempVector = BogieCinemachinePath.EvaluateLocalTangent(onRailPoint_PathUnit);//過去位置の接線を取得
                float Dot = Vector3.Dot(CachedBogieLocalPosition - UnderRail_LocalPosition, tempVector.normalized);//内積()
                onRailPoint_PathUnit += Dot / tempVector.magnitude;
                if (onRailPoint_PathUnit < 0) onRailPoint_PathUnit = 0;
                if (onRailPoint_PathUnit > railMaxPoint) onRailPoint_PathUnit = railMaxPoint;
            }
            if (isDirty)
            {
                ApplyRailPointToTransform();
                if (BogieToUnderRailLength > Mathf.Max((UnderRail_LocalPosition - RailEnd_LocalPosition).sqrMagnitude , distanceErrorThreshold[0]/2))
                {
                    changeRail(true);
                }
                else if (BogieToUnderRailLength > Mathf.Max((UnderRail_LocalPosition - RailStart_LocalPosition).sqrMagnitude , distanceErrorThreshold[0]/2))
                {
                    changeRail(false);
                }
            }
        }

        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            fromLastSync = 0;
            nextSync = syncInterval;
            isOwnerState = Networking.IsOwner(this.gameObject);

            if (isOwnerState)//Taking Owner
            {
                isDiscontinuitySync = true;
            }
            else
            {
                SyncedRailID = RailID;
                SyncedRailPoint_PathUnit = onRailPoint_PathUnit;
            }
        }
        public override void OnPreSerialization()
        {

            SyncedRailID = RailID;
            SyncedRailPoint_PathUnit = onRailPoint_PathUnit;
            fromLastSync = 0;
            InitsyncRecieveMode = false;
            isOwnerState = true;
        }

        public override void OnDeserialization()
        {
            isOwnerState = false;
            if (!isDiscontinuitySync)
            {

                nextSync = 2 * syncInterval - fromLastSync;
                if (nextSync < syncInterval / 2) nextSync = syncInterval / 2;
                if (nextSync > syncInterval * 1.5f) nextSync = syncInterval * 1.5f;
                fromLastSync = 0;
            }
            else if (InitsyncRecieveMode)
            {
                Debug.Log("Init Sync");



                InitsyncRecieveMode = false;
                if (!Networking.IsOwner(gameObject))
                {
                    BogieRail = railsManager.Rails[SyncedRailID];
                    onRailPoint_PathUnit = SyncedRailPoint_PathUnit;

                    Debug.Log("onRailPoint " + onRailPoint_PathUnit);

                    copyRailProperties();
                    ApplyRailPointToTransform();
                }


                Debug.Log("position " + transform.localPosition);

                fromLastSync = 0;
                nextSync = syncInterval;
            }

        }
    }

}
