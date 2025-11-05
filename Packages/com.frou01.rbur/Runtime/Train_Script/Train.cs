
using Cinemachine;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    public class Train : UdonSharpBehaviour
    {
        //PresetByBuildProcess
        [HideInInspector] public TrainManager trainManager;
        [HideInInspector] public RailsManager railsManager;
        [HideInInspector] public int TrainID;//連結他同期用


        //43byte
        //tips:Max 1600bit

        [SerializeField] private Vector3 CenterOfMass;

        public CouplerObj CouplerF;
        public CouplerObj CouplerB;

        //public Vector3 gravity = new Vector3(0, -9.8f, 0);

        [SerializeField] Animator controllerAnimator;

        [SerializeField] TrainConnectionReciever[] connectionRecievers;
        public TrainConnectionReciever GetConnectionRecieverByTag(string targetTag)
        {
            foreach (TrainConnectionReciever connection in connectionRecievers)
            {
                foreach (string tag in connection.connectionTags)
                {
                    if (tag == targetTag)
                    {
                        return connection;
                    }
                }
            }
            return null;
        }

        public Train connectedTrain_F;
        public Train connectedTrain_B;
        public void setConnectedTrain(Train connectedTrain, bool F_B)//車両連結/解結
        {
            if (F_B)
            {
                connectedTrain_F = connectedTrain;
                if (connectedTrain_F != null && Networking.GetOwner(connectedTrain_F.gameObject) != Networking.GetOwner(gameObject))
                    Networking.SetOwner(Networking.GetOwner(gameObject), connectedTrain_F.gameObject);

            }
            else
            {
                connectedTrain_B = connectedTrain;
                if (connectedTrain_B != null && Networking.GetOwner(connectedTrain_B.gameObject) != Networking.GetOwner(gameObject))
                    Networking.SetOwner(Networking.GetOwner(gameObject), connectedTrain_B.gameObject);
            }
            if (connectedTrain_F != null && connectedTrain_B != null)
            {
                syncInterval = 2.9f + UnityEngine.Random.Range(0, 0.2f);
            }
            else
            {
                syncInterval = 0.2f;
            }
            nextSync = syncInterval;
            expectedSyncedPosition = syncedPosition = transform.localPosition;
            if (syncedVelocity != Vector3.zero) expectedSyncedVelocity = syncedVelocity = currentVelocity;
            if (stopSync)
            {
                expectedSyncedVelocity = syncedVelocity = Vector3.zero;
            }
            updatePredicteBezier();

            foreach (TrainConnectionReciever reciever in connectionRecievers)
            {
                reciever.TrainConnectionUpdate(connectedTrain, F_B);
            }
        }

        [SerializeField] private Rail_Script BogieRail_F;
        [SerializeField] private Rail_Script BogieRail_B;
        [SerializeField] private Transform Bogie_F;
        [SerializeField] private Rigidbody BogieWheel_F;
        [SerializeField] private Transform Bogie_B;
        [SerializeField] private Rigidbody BogieWheel_B;



#pragma warning disable CS0414
        [Obsolete][SerializeField] float BrakeMultiplier = 0.01f;
        //float brakeFactor;

        //float m_legacy_brakePressure_float;//4byte,[MPa]
        [Obsolete][SerializeField] public float baseBrakePressure = 1;
        //float currentFriction;
        [Obsolete][SerializeField] float friction = 0.004f;
        [Obsolete][SerializeField] float static_friction = 0.013f;
#pragma warning restore CS0414



        GameObject mineGameObject;
        Transform chacedTransform;
        private int pathResolution = 3;
        Rigidbody rigidbody_;
        bool isOwnerState;
        bool started = false;
        private bool hasAnimator;
        private int rigidBodySpeedParamaterID;
        public void Start()
        {
            rigidbody_ = GetComponent<Rigidbody>();
            //rigidBodyMass = rigidbody_.mass;
            BogieStart();
            bodyCenterInterpole_F =
                Mathf.Abs(Bogie_F.localPosition.z) / Mathf.Abs(Bogie_F.localPosition.z - Bogie_B.localPosition.z);

            mineGameObject = gameObject;
            chacedTransform = transform;
            rigidBodySpeedParamaterID = Animator.StringToHash("RigidBodySpeed");

            setPositionFromBogie();
            FixedDeltaTime = Time.fixedDeltaTime;

            hasAnimator = controllerAnimator != null;

            setConnectedTrain(connectedTrain_F,true);
            setConnectedTrain(connectedTrain_B,false);
        }
        private void PostStart()
        {
            pathResolution = trainManager.pathRes;
            if (Networking.IsOwner(mineGameObject))
            {
                rigidbody_.isKinematic = false;
                rigidbody_.centerOfMass = CenterOfMass;
                rigidbody_.ResetInertiaTensor();
            }
            isOwnerState = Networking.IsOwner(mineGameObject);
            //transform.parent = null;
        }


        private bool needSync;
        private bool stopSync;
        private float distanceErrorThreshold;
        private float FixedDeltaTime;
        private Vector3 currentVelocity;
        private Vector3 localVelocity;

        private Vector3 positionBogie_F;
        private Vector3 positionBogie_B;
        private float m_nowSpeed;
        [HideInInspector] public float[] Rigidbody_Speed_LocalZ = new float[1];
        void FixedUpdate()
        {
            if (!started)
            {
                started = Networking.IsObjectReady(gameObject);
                if (started) PostStart();
                return;
            }
            currentVelocity = rigidbody_.velocity;
            localVelocity = Quaternion.Inverse(chacedTransform.rotation) * currentVelocity;
            m_nowSpeed = localVelocity.z;
            fromLastSync += FixedDeltaTime;
            if (isOwnerState)
            {
                if (!needSync && Mathf.Abs(m_nowSpeed) > 0.01f && Vector3.Distance(syncedPosition, chacedTransform.localPosition) > 0.002) needSync = true;
                if (moveableRail_F) needSync = true;
                if (moveableRail_B) needSync = true;
                if (!stopSync && Mathf.Abs(m_nowSpeed) <= 0.01f && (Vector3.Distance(syncedPosition, chacedTransform.localPosition) > 0.002 || syncedVelocity != Vector3.zero))
                {
                    stopSync = true;
                    needSync = true;
                }
                if (needSync && fromLastSync > syncInterval)
                {
                    RequestSerialization();
                    needSync = false;
                    stopSync = false;
                }
            }
            else
            {
                onRemote();
            }
            if (hasAnimator)
            {
                controllerAnimator.SetFloat(rigidBodySpeedParamaterID, m_nowSpeed / 100);
            }
            positionBogie_F = Bogie_F.position;
            positionBogie_B = Bogie_B.position;
            distanceErrorThreshold = FixedDeltaTime * (1 + Mathf.Abs(m_nowSpeed));
            BogieCalculateNextPos();
            Rigidbody_Speed_LocalZ[0] = m_nowSpeed;
        }

        Vector3 calculatedVelocity;
        Vector3 dif;
        float bezierVectorScaler;
        Vector3 bezierB;
        Vector3 bezierC;
        float initWaiting = 0;
        float RailErrorTime = 0;
        private void onRemote()
        {
            if (InitsyncRecieveMode)
            {
                initWaiting += FixedDeltaTime;
                if(initWaiting > 20)
                    trainManager.SendCustomEvent("ReSyncRequest");
            }
            float t = fromLastSync / nextSync;
            float conT = 1 - t;
            if (t < 1)
            {
                if ((prevSyncedVelocity - syncedVelocity).magnitude > 0.000001f)
                {
                    expectedSyncedPosition = conT * conT * conT * prevSyncedPosition +
                        3 * conT * conT * t * bezierB +
                        3 * conT * t * t * bezierC +
                        t * t * t * syncedPosition;
                }
                else
                {
                    expectedSyncedPosition = Vector3.Lerp(prevSyncedPosition, syncedPosition, t);
                }
                expectedSyncedVelocity = Vector3.Lerp(prevSyncedVelocity, syncedVelocity, t);
                dif = Quaternion.Inverse(chacedTransform.localRotation) * (expectedSyncedPosition - chacedTransform.localPosition);
            }
            else if (syncedVelocity.sqrMagnitude > 0.001f)
            {
                prevSyncedPosition = expectedSyncedPosition = syncedPosition + syncedVelocity * (fromLastSync - nextSync);
                prevSyncedVelocity = expectedSyncedVelocity = syncedVelocity;
                dif = Quaternion.Inverse(chacedTransform.localRotation) * (expectedSyncedPosition - chacedTransform.localPosition);
            }
            else
            {
                prevSyncedPosition = expectedSyncedPosition = syncedPosition;
                prevSyncedVelocity = expectedSyncedVelocity = syncedVelocity;
                dif = Quaternion.Inverse(chacedTransform.localRotation) * (expectedSyncedPosition - chacedTransform.localPosition);
                if (dif.sqrMagnitude < 0.0004) dif = Vector3.zero;
            }


            if (syncInterval < 1)
            {
                dif.x = 0;
                dif.y = 0;
                calculatedVelocity = (dif - localVelocity);
            }
            else if (expectedSyncedVelocity.sqrMagnitude > 1)
            {
                dif.x = 0;
                dif.y = 0;
                calculatedVelocity = (expectedSyncedVelocity - localVelocity) / (syncInterval * 10);
            }
            else
            {
                calculatedVelocity = Vector3.zero;
            }

            //if(dif.magnitude > 0.1)
            //{
            //    Debug.Log("ID  " + TrainID);
            //    Debug.Log("dif " + dif);
            //    Debug.Log("syncedVelocity " + syncedVelocity);
            //    Debug.Log(" localPosition " + chacedTransform.localPosition);
            //    Debug.Log("syncedPosition " + syncedPosition);
            //}
            calculatedVelocity.x = 0;
            calculatedVelocity.y = 0;
            if(calculatedVelocity.z * calculatedVelocity.z > 0.0001) rigidbody_.AddRelativeForce(calculatedVelocity, ForceMode.VelocityChange);
            orProxy = false;
            if (RailID_F != SyncedRailID_F)orProxy = true;
            if (SyncedRailID_B != RailID_B) orProxy = true;
            if (SyncedRailUnitOrder != onRailPoint_F > onRailPoint_B) orProxy = true;
            //if (RailID_F != SyncedRailID_F || SyncedRailID_B != RailID_B || SyncedRailUnitOrder != onRailPoint_F > onRailPoint_B)
            if (orProxy)
            {
                RailErrorTime += FixedDeltaTime;
                if (RailErrorTime > 30)
                {
                    trainManager.SendCustomEvent(nameof(trainManager.ReSyncRequest));
                    RailErrorTime = 0;
                }
            }
            else
            {
                if (RailErrorTime > 0) RailErrorTime -= FixedDeltaTime * 2;
            }
        }

        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            fromLastSync = 0;
            nextSync = syncInterval;

            if (Networking.LocalPlayer == player)
            {
                Debug.Log("transfering subObject owner " + GetHierarchyPath(transform));
                isDiscontinuitySync = true;

                Networking.SetOwner(player, CouplerF.gameObject);
                Networking.SetOwner(player, CouplerB.gameObject);
                if (connectedTrain_F != null)
                    Networking.SetOwner(player, connectedTrain_F.gameObject);
                if (connectedTrain_B != null)
                    Networking.SetOwner(player, connectedTrain_B.gameObject);

                rigidbody_.isKinematic = false;
                rigidbody_.centerOfMass = CenterOfMass;
                rigidbody_.ResetInertiaTensor();
            }
            else if (!isOwnerState)
            {
                expectedSyncedPosition = syncedPosition = transform.localPosition;
                expectedSyncedVelocity = syncedVelocity = currentVelocity;
                syncedPosition = transform.localPosition;
                SyncedRailID_F = RailID_F;
                SyncedRailID_B = RailID_B;
                SyncedRailPoint_F = onRailPoint_F;
                SyncedRailPoint_B = onRailPoint_B;
                SyncedRailUnitOrder = onRailPoint_F > onRailPoint_B;
            }
            else
            {
                expectedSyncedPosition = syncedPosition = transform.localPosition;
                expectedSyncedVelocity = syncedVelocity = currentVelocity;
                SyncedRailID_F = RailID_F;
                SyncedRailID_B = RailID_B;
                SyncedRailPoint_F = onRailPoint_F;
                SyncedRailPoint_B = onRailPoint_B;
                SyncedRailUnitOrder = onRailPoint_F > onRailPoint_B;
            }
            updatePredicteBezier();
            isOwnerState = Networking.IsOwner(mineGameObject);
        }
        private string GetHierarchyPath(Transform transform)
        {
            string text = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                text = transform.name + "/" + text;
            }

            return text;
        }

        private float bodyCenterInterpole_F;


        //前ボギー フィールド ココから
        private float onRailPoint_F;
        private Vector3 onRailPosition_F;

        private int RailID_F;
        private CinemachinePathBase BogieCinemachinePath_F;
        private float railMaxPoint_F;

        private Vector3 RailStartPoint_F;
        private Vector3 RailEnd__Point_F;

        private bool moveableRail_F;


        //後ボギー フィールド ココから
        private float onRailPoint_B;
        private Vector3 onRailPosition_B;

        private int RailID_B;
        private CinemachinePathBase BogieCinemachinePath_B;
        private float railMaxPoint_B;

        private Vector3 RailStartPoint_B;
        private Vector3 RailEnd__Point_B;

        private bool moveableRail_B;




        [HideInInspector] [UdonSynced] int SyncedRailID_F;//4byte
        [HideInInspector] [UdonSynced] int SyncedRailID_B;//4byte
        [HideInInspector] [UdonSynced] float SyncedRailPoint_F;//4byte
        [HideInInspector] [UdonSynced] float SyncedRailPoint_B;//4byte
        [HideInInspector] [UdonSynced] bool SyncedRailUnitOrder;//1byte

        public void BogieStart()
        {
            onRailPoint_F = BogieRail_F.GetF(Bogie_F.position);
            copyRailProperties_F();
            onRailPosition_F = BogieCinemachinePath_F.EvaluatePosition(onRailPoint_F);


            onRailPoint_B = BogieRail_B.GetF(Bogie_B.position);
            copyRailProperties_B();
            onRailPosition_B = BogieCinemachinePath_B.EvaluatePosition(onRailPoint_B);

            prevpositionBogieF = positionBogie_F = Bogie_F.position;
            prevpositionBogieB = positionBogie_B = Bogie_B.position;
        }

        public void setPositionFromBogie()
        {
            BogieWheel_F.position = onRailPosition_F = BogieCinemachinePath_F.EvaluatePosition(onRailPoint_F);
            BogieWheel_F.rotation = BogieCinemachinePath_F.EvaluateOrientation(onRailPoint_F);

            BogieWheel_B.position = onRailPosition_B = BogieCinemachinePath_B.EvaluatePosition(onRailPoint_B);
            BogieWheel_B.rotation = BogieCinemachinePath_B.EvaluateOrientation(onRailPoint_B);



            Vector3 forward = onRailPosition_F - onRailPosition_B;
            if (forward.sqrMagnitude > 0.001)
            {
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.up = Vector3.up;
                gameObject.transform.forward = forward;
                rigidbody_.rotation = gameObject.transform.rotation;
            }


            gameObject.transform.position = onRailPosition_F * (1 - bodyCenterInterpole_F) + onRailPosition_B * bodyCenterInterpole_F;

            rigidbody_.position = (gameObject.transform.position);


            prevpositionBogieF = positionBogie_F = Bogie_F.position;
            prevpositionBogieB = positionBogie_B = Bogie_B.position;

        }





        private float BogieToWheelPosLength_F;
        private bool tooLongDiffF;
        private float BogieToWheelPosLength_B;
        private bool tooLongDiffB;

        private Vector3 prevpositionBogieF;
        private Vector3 prevpositionBogieB;

        private bool isDirty;

        private Vector3 tempVector;
        private bool orProxy;
        public void BogieCalculateNextPos()
        {
            if (moveableRail_F)
            {
                onMovable(true);
            }
            orProxy = false;
            if (moveableRail_F) orProxy = true;
            if (tooLongDiffF) orProxy = true;
            //if (moveableRail_F || tooLongDiffF)
            //Get Next onRailPoint
            if (orProxy)
            {
                //Use findClosest
                isDirty = true;
                onRailPoint_F = BogieCinemachinePath_F.FindClosestPoint(positionBogie_F, (int)onRailPoint_F, 1, pathResolution);
                prevpositionBogieF = positionBogie_F;
                //Debug.Log("Use FindClosest");
            }else if (Vector3.Distance(positionBogie_F, prevpositionBogieF) > distanceErrorThreshold)
            {
                //Use tangent algorithm
                isDirty = true;
                tempVector = BogieCinemachinePath_F.EvaluateTangent(onRailPoint_F);
                float Dot = Vector3.Dot(positionBogie_F - prevpositionBogieF, tempVector.normalized);
                onRailPoint_F += Dot / tempVector.magnitude;
                if (onRailPoint_F < 0) onRailPoint_F = 0;
                if (onRailPoint_F > railMaxPoint_F) onRailPoint_F = railMaxPoint_F;
                prevpositionBogieF = positionBogie_F;
            }
            if (isDirty) orProxy = true;
            if (orProxy)
            {
                updateWheel(true);
                tryChangeRailF();
            }

            if (moveableRail_B)
            {
                onMovable(false);
            }
            orProxy = false;
            if (moveableRail_B) orProxy = true;
            if (tooLongDiffB) orProxy = true;
            if (orProxy)
            {
                isDirty = true;
                onRailPoint_B = BogieCinemachinePath_B.FindClosestPoint(positionBogie_B, (int)onRailPoint_B, 1, pathResolution);
                prevpositionBogieB = positionBogie_B;
            }
            else if (Vector3.Distance(positionBogie_B, prevpositionBogieB) > distanceErrorThreshold)
            {
                tempVector = BogieCinemachinePath_B.EvaluateTangent(onRailPoint_B);
                float Dot = Vector3.Dot(positionBogie_B - prevpositionBogieB, tempVector.normalized);
                isDirty = true;
                onRailPoint_B += Dot / tempVector.magnitude;
                if (onRailPoint_B < 0) onRailPoint_B = 0;
                if (onRailPoint_B > railMaxPoint_B) onRailPoint_B = railMaxPoint_B;
                prevpositionBogieB = positionBogie_B;
            }
            if (isDirty) orProxy = true;
            if (orProxy)
            {
                updateWheel(false);
                tryChangeRailB();
            }
        }
        private void onMovable(bool dir)
        {
            if (dir)
            {
                RailStartPoint_F = BogieCinemachinePath_F.EvaluatePosition(BogieCinemachinePath_F.MinPos);
                RailEnd__Point_F = BogieCinemachinePath_F.EvaluatePosition(BogieCinemachinePath_F.MaxPos);
            }
            else
            {
                RailStartPoint_B = BogieCinemachinePath_B.EvaluatePosition(BogieCinemachinePath_B.MinPos);
                RailEnd__Point_B = BogieCinemachinePath_B.EvaluatePosition(BogieCinemachinePath_B.MaxPos);
            }
        }

        private void updateWheel(bool dir)
        {
            if (dir)
            {
                BogieWheel_F.position = onRailPosition_F = BogieCinemachinePath_F.EvaluatePosition(onRailPoint_F);
                BogieWheel_F.rotation = BogieCinemachinePath_F.EvaluateOrientation(onRailPoint_F);
                tempVector = onRailPosition_F - positionBogie_F;
                BogieToWheelPosLength_F = tempVector.sqrMagnitude;
                tooLongDiffF = BogieToWheelPosLength_F > distanceErrorThreshold*5;
                prevpositionBogieF = onRailPosition_F;
            }
            else
            {
                BogieWheel_B.position = onRailPosition_B = BogieCinemachinePath_B.EvaluatePosition(onRailPoint_B);
                BogieWheel_B.rotation = BogieCinemachinePath_B.EvaluateOrientation(onRailPoint_B);
                tempVector = onRailPosition_B - positionBogie_B;
                BogieToWheelPosLength_B = tempVector.sqrMagnitude;
                tooLongDiffB = BogieToWheelPosLength_B > distanceErrorThreshold*5;
                prevpositionBogieB = onRailPosition_B;
            }
        }

        private void tryChangeRailF()
        {
            if (BogieToWheelPosLength_F > (onRailPosition_F - RailEnd__Point_F).sqrMagnitude)
            {
                changeRailF(true);
            }
            else if (BogieToWheelPosLength_F > (onRailPosition_F - RailStartPoint_F).sqrMagnitude)
            {
                changeRailF(false);
            }
        }
        private void changeRailF(bool dir)
        {
            if (dir)
            {
                if (BogieRail_F.next != null)
                {
                    Rail_Script NextRail = BogieRail_F.next;
                    BogieRail_F = NextRail;
                    copyRailProperties_F();
                    onRailPoint_F = BogieCinemachinePath_F.FindClosestPoint(positionBogie_F, 0, -1, 10);
                    BogieWheel_F.position = onRailPosition_F = BogieCinemachinePath_F.EvaluatePosition(onRailPoint_F);
                    BogieWheel_F.rotation = BogieCinemachinePath_F.EvaluateOrientation(onRailPoint_F);
                }
            }
            else
            {
                if (BogieRail_F.prev != null)
                {
                    Rail_Script NextRail = BogieRail_F.prev;
                    BogieRail_F = NextRail;
                    copyRailProperties_F();
                    onRailPoint_F = BogieCinemachinePath_F.FindClosestPoint(positionBogie_F, 0, -1, 10);
                    BogieWheel_F.position = onRailPosition_F = BogieCinemachinePath_F.EvaluatePosition(onRailPoint_F);
                    BogieWheel_F.rotation = BogieCinemachinePath_F.EvaluateOrientation(onRailPoint_F);
                }
            }
        }


        public void copyRailProperties_F()
        {
            if (BogieRail_F != null)
            {
                BogieCinemachinePath_F = BogieRail_F.cinemachinePath;
                moveableRail_F = BogieRail_F.moveableRail;
                RailStartPoint_F = BogieRail_F.GetStartPoint();
                RailEnd__Point_F = BogieRail_F.GetEndPoint();
                railMaxPoint_F = BogieCinemachinePath_F.MaxPos;
                RailID_F = BogieRail_F.RailID;
                RailErrorTime = 0;
            }
        }

        private void tryChangeRailB()
        {
            if (BogieToWheelPosLength_B > (onRailPosition_B - RailEnd__Point_B).sqrMagnitude)
            {
                changeRailB(true);
            }
            else if (BogieToWheelPosLength_B > (onRailPosition_B - RailStartPoint_B).sqrMagnitude)
            {
                changeRailB(false);
            }
        }
        private void changeRailB(bool dir)
        {
            if (dir)
            {
                if (BogieRail_B.next != null)
                {
                    Rail_Script NextRail = BogieRail_B.next;
                    BogieRail_B = NextRail;
                    copyRailProperties_B();
                    onRailPoint_B = BogieCinemachinePath_B.FindClosestPoint(positionBogie_B, 0, -1, 10);
                    BogieWheel_B.position = onRailPosition_B = BogieCinemachinePath_B.EvaluatePosition(onRailPoint_B);
                    BogieWheel_B.rotation = BogieCinemachinePath_B.EvaluateOrientation(onRailPoint_B);
                }
            }
            else
            {
                if (BogieRail_B.prev != null)
                {
                    Rail_Script NextRail = BogieRail_B.prev;
                    BogieRail_B = NextRail;
                    copyRailProperties_B();
                    onRailPoint_B = BogieCinemachinePath_B.FindClosestPoint(positionBogie_B, 0, -1, 10);
                    BogieWheel_B.position = onRailPosition_B = BogieCinemachinePath_B.EvaluatePosition(onRailPoint_B);
                    BogieWheel_B.rotation = BogieCinemachinePath_B.EvaluateOrientation(onRailPoint_B);
                }
            }
        }

        public void copyRailProperties_B()
        {
            if (BogieRail_B != null)
            {
                BogieCinemachinePath_B = BogieRail_B.cinemachinePath;
                moveableRail_B = BogieRail_B.moveableRail;
                RailStartPoint_B = BogieRail_B.GetStartPoint();
                RailEnd__Point_B = BogieRail_B.GetEndPoint();
                railMaxPoint_B = BogieCinemachinePath_B.MaxPos;
                RailID_B = BogieRail_B.RailID;
                RailErrorTime = 0;
            }

        }
        private Vector3 prevSyncedVelocity;
        private Vector3 prevSyncedPosition;
        private Vector3 expectedSyncedPosition;
        private Vector3 expectedSyncedVelocity;
        [UdonSynced] private Vector3 syncedPosition;//12byte
        [UdonSynced] private Vector3 syncedVelocity;//12byte
        [UdonSynced] private float syncInterval = 1;//4byte
        private float nextSync = 1;

        private float fromLastSync;

        [System.NonSerialized] public bool InitsyncRecieveMode = true;
        [System.NonSerialized] public System.DateTime LastSent_Resync = System.DateTime.MinValue;

        [HideInInspector] [UdonSynced] private bool isDiscontinuitySync;
        public void resync()
        {
            isOwnerState = Networking.IsOwner(mineGameObject);
            isDiscontinuitySync = true;
            RequestSerialization();
        }
        public override void OnPreSerialization()
        {
            if (stopSync)
            {
                currentVelocity = Vector3.zero;
            }

            if (!isDiscontinuitySync)
            {
                syncedVelocity = currentVelocity;
            }
            else
            {
                CouplerF.RequestSerialization();
                CouplerB.RequestSerialization();
                syncedVelocity = Vector3.zero;
            }
            expectedSyncedVelocity.Set(0, 0, 0);
            syncedPosition = transform.localPosition;
            SyncedRailID_F = RailID_F;
            SyncedRailID_B = RailID_B;
            SyncedRailPoint_F = onRailPoint_F;
            SyncedRailPoint_B = onRailPoint_B;
            SyncedRailUnitOrder = onRailPoint_F > onRailPoint_B;
            fromLastSync = 0;
            InitsyncRecieveMode = false;
            isOwnerState = true;
        }

        public override void OnDeserialization()
        {
            isOwnerState = false;
            if (!isDiscontinuitySync)
            {
                updatePredicteBezier();

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
                    BogieRail_F = railsManager.Rails[SyncedRailID_F];
                    BogieRail_B = railsManager.Rails[SyncedRailID_B];
                    onRailPoint_F = SyncedRailPoint_F;
                    onRailPoint_B = SyncedRailPoint_B;

                    Debug.Log("trainID " + TrainID);
                    Debug.Log("onRailPoint_F " + onRailPoint_F);
                    Debug.Log("onRailPoint_B " + onRailPoint_B);

                    copyRailProperties_F();
                    copyRailProperties_B();
                    setPositionFromBogie();

                    transform.localPosition = syncedPosition;

                    onMovable(true);
                    updateWheel(true);

                    onMovable(false);
                    updateWheel(false);
                }

                prevSyncedPosition = expectedSyncedPosition = syncedPosition;
                prevSyncedVelocity = expectedSyncedVelocity = syncedVelocity = Vector3.zero;

                rigidbody_.isKinematic = true;
                SendCustomEventDelayedSeconds(nameof(releaseLock), 0.4f);

                Debug.Log("position " + transform.localPosition);
                Debug.Log("syncedPosition " + syncedPosition);

                updatePredicteBezier();

                fromLastSync = 0;
                nextSync = syncInterval;
            }

        }

        private float uPBa;
        private float uPBb;
        private float uPBc;
        void updatePredicteBezier()
        {
            prevSyncedPosition = expectedSyncedPosition;
            prevSyncedVelocity = expectedSyncedVelocity;
            uPBa = prevSyncedVelocity.sqrMagnitude + syncedVelocity.sqrMagnitude + 2 * prevSyncedVelocity.magnitude * syncedVelocity.magnitude
                - (-syncedVelocity - prevSyncedVelocity).sqrMagnitude / 4;
            uPBb = -Vector3.Dot(syncedPosition - prevSyncedPosition, -syncedVelocity - prevSyncedVelocity);
            uPBc = -(syncedPosition - prevSyncedPosition).sqrMagnitude;
            bezierVectorScaler = (-uPBb + Mathf.Sqrt(uPBb * uPBb - 4 * uPBa * uPBc)) / (2 * uPBa);
            //Debug.Log(bezierVectorScaler);
            if (bezierVectorScaler > 1f) bezierVectorScaler = 1f;
             bezierB = prevSyncedPosition + prevSyncedVelocity * bezierVectorScaler;
            bezierC = syncedPosition - syncedVelocity * bezierVectorScaler;
        }
        public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result) {
            if(!result.success && isDiscontinuitySync)
            {
                Debug.Log("Something went wrong, retry Init sync");
                SendCustomEventDelayedSeconds(nameof(resync), UnityEngine.Random.Range(10f,20f));
            }
            else
            {
                isDiscontinuitySync = false;
            }
        }
        public void releaseLock()
        {
            rigidbody_.isKinematic = false;
            rigidbody_.velocity = Vector3.zero;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawSphere(transform.TransformPoint(CenterOfMass), 0.3f);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(Bogie_F.transform.position, 0.3f);
            Gizmos.DrawLine(Bogie_F.transform.position, BogieWheel_F.transform.position);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(Bogie_B.transform.position, 0.3f);
            Gizmos.DrawLine(Bogie_B.transform.position, BogieWheel_B.transform.position);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Gizmos.DrawSphere(transform.TransformPoint(CenterOfMass), 0.3f);
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.DrawSphere(Bogie_F.transform.position, 0.3f);
            Gizmos.DrawLine(Bogie_F.transform.position, BogieWheel_F.transform.position);
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.DrawSphere(Bogie_B.transform.position, 0.3f);
            Gizmos.DrawLine(Bogie_B.transform.position, BogieWheel_B.transform.position);
            if(connectedTrain_F != null) drawKnucle(connectedTrain_F,CouplerF);
            Gizmos.color = new Color(0f, 0f, 1f, 1f);
            if (connectedTrain_B != null) drawKnucle(connectedTrain_B,CouplerB);
        }
        private Vector3 drawingPos;
        private Quaternion drawingRot;
        private Vector3 drawingStart;
        private Vector3 drawingEnd;

        void drawKnucle(Train connectedTrain, CouplerObj ConnectingCoupler)
        {
            Transform ConnectingCouplerTransform = ConnectingCoupler.transform;
            CouplerObj connectCoupler = searchKnucle(connectedTrain);
            Transform connectCouplerTransform = connectCoupler.transform;

            float scale = (connectCouplerTransform.position - ConnectingCoupler.transform.position).magnitude + 0.4f;

            drawingPos = ConnectingCouplerTransform.position + ConnectingCouplerTransform.forward * -0.4f;
            drawingRot = ConnectingCouplerTransform.rotation;
            drawingStart = new Vector3();
            drawingEnd = new Vector3();

            drawingPos.y += 2f;

            drawingEnd.x = 0.5f * scale;
            drawingEnd.z = 0.6f * scale;
            DrawLine(drawingPos, drawingRot, drawingStart, drawingEnd);

            drawingStart = drawingEnd;
            drawingEnd.x = 0;
            drawingEnd.z = 1.2f * scale;
            DrawLine(drawingPos, drawingRot, drawingStart, drawingEnd);

            if (connectCoupler.FrontOrBack)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 1f);
            }
            else
            {
                Gizmos.color = new Color(0f, 0f, 1f, 1f);
            }
                drawingPos = connectCouplerTransform.position + connectCouplerTransform.forward * -0.4f;
            drawingRot = connectCouplerTransform.rotation;
            drawingStart = new Vector3();
            drawingEnd = new Vector3();

            drawingPos.y += 2f;

            drawingEnd.x = 0.5f * scale;
            drawingEnd.z = 0.6f * scale;
            DrawLine(drawingPos, drawingRot, drawingStart, drawingEnd);

            drawingStart = drawingEnd;
            drawingEnd.x = 0;
            drawingEnd.z = 1.2f * scale;
            DrawLine(drawingPos, drawingRot, drawingStart, drawingEnd);
        }
        void DrawLine(Vector3 originOffset , Quaternion rotation, Vector3 start,Vector3 end)
        {
            Gizmos.DrawLine(originOffset + rotation * start, originOffset + rotation * end);
        }

        CouplerObj searchKnucle(Train connectedTrain)
        {

            if (Vector3.Dot(connectedTrain.transform.forward, transform.position - connectedTrain.transform.position) > 0)
            {
                return connectedTrain.CouplerF;
            }
            else
            {
                return connectedTrain.CouplerB;
            }
        }
#endif
    }

}
