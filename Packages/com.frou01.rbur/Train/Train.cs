
using Cinemachine;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    public class Train : UdonSharpBehaviour
    {
        //43byte
        //tips:Max 1600bit
        GameObject mineGameObject;
        Transform chacedTransform;
        public int TrainID;//連結他同期用

        public TrainManager trainManager;
        public RailsManager railsManager;
        Rigidbody rigidbody_;
        float rigidBodyMass;

        bool isOwnerState;

        [SerializeField] public bool started = false;
        private bool needSync;
        private bool stopSync;

        [SerializeField] public bool handBrakeState;
        [SerializeField] float handBrakeForce;
        [SerializeField] float BrakeMultiplier = 0.01f;
        [SerializeField] float brakeFactor;
        [SerializeField] bool brakeUpdateBypass;

        float prevBrakePressure;
        [UdonSynced] float brakePressure_float;//4byte
        float currentFriction;
        [SerializeField] float friction = 0.004f;
        [SerializeField] float static_friction = 0.013f;


        [SerializeField] public Transform brakePressure;

        [SerializeField] private Vector3 CenterOfMass;

        private Vector3 brakePressure_proxy;

        [UdonSynced] public bool BrakeOpenF;//1byte
        [UdonSynced] public bool BrakeOpenB;//1byte

        private Transform ConnectedBrakePressureF;
        private Transform ConnectedBrakePressureB;

        private bool Coupler_InitedF;
        private bool Coupler_InitedB;
        public CouplerObj CouplerF;
        public CouplerObj CouplerB;

        //public Vector3 gravity = new Vector3(0, -9.8f, 0);

        public Animator controllerAnimator;
        private bool hasAnimator;

        private int rigidBodySpeedParamaterID;
        private int brakePressureParamaterID;
        private int handBrakeStateID;
        private int handBrakeForceID;

        //private Vector3 zero = Vector3.zero;

        private int pathResolution = 3;
        float RailErrorTime = 0;

        public void Start()
        {
            rigidbody_ = GetComponent<Rigidbody>();
            rigidBodyMass = rigidbody_.mass;
            BogieStart();
            bodyCenterInterpole_F =
                Mathf.Abs(Bogie_F.localPosition.z) / Mathf.Abs(Bogie_F.localPosition.z - Bogie_B.localPosition.z);

            mineGameObject = gameObject;
            chacedTransform = transform;
            rigidBodySpeedParamaterID = Animator.StringToHash("RigidBodySpeed");
            brakePressureParamaterID = Animator.StringToHash("BrakePressure");
            handBrakeStateID = Animator.StringToHash("HandBrakeState");
            handBrakeForceID = Animator.StringToHash("HandBrakeForce");

            setPositionFromBogie();
            FixedDeltaTime = Time.fixedDeltaTime;

            hasAnimator = controllerAnimator != null;
        }
        public void PostStart()
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

        private float changedSpeed;
        private float lastSpeed;
        private float nowSpeed;

        Vector3 positionBogie_F;
        Vector3 positionBogie_B;

        float FixedDeltaTime;
        Vector3 calculatedVelocity;

        Vector3 currentVelocity;
        Vector3 localVelocity;


        Vector3 dif;
        float bezierVectorScaler;
        Vector3 bezierB;
        Vector3 bezierC;

        float FunctionProxy_Float1;
        Vector3 FunctionProxy_Vector1;
        void FixedUpdate()
        {
            if (!started)
            {
                started = (trainManager != null && Networking.IsObjectReady(gameObject) && Coupler_InitedF && Coupler_InitedB);
                if (started) PostStart();
                return;
            }
            currentVelocity = rigidbody_.velocity;
            localVelocity = Quaternion.Inverse(chacedTransform.rotation) * currentVelocity;
            nowSpeed = localVelocity.z;
            changedSpeed = nowSpeed - lastSpeed;
            fromLastSync += FixedDeltaTime;
            if (isOwnerState)
            {
                if (!needSync && Mathf.Abs(nowSpeed) > 0.01f && Vector3.Distance(syncedPosition, chacedTransform.localPosition) > 0.002) needSync = true;
                if (moveableRail_F) needSync = true;
                if (moveableRail_B) needSync = true;
                if (!stopSync && Mathf.Abs(nowSpeed) <= 0.01f && (Vector3.Distance(syncedPosition, chacedTransform.localPosition) > 0.002 || syncedVelocity != Vector3.zero))
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
            if (Mathf.Abs(nowSpeed + changedSpeed) * rigidBodyMass > brakeFactor * FixedDeltaTime)
            {
                FunctionProxy_Float1 = nowSpeed > 0 ? -brakeFactor : brakeFactor;
                FunctionProxy_Vector1.z = FunctionProxy_Float1;
                rigidbody_.AddRelativeForce(FunctionProxy_Vector1, ForceMode.Force);
                lastSpeed = nowSpeed + (nowSpeed > 0 ? -brakeFactor : brakeFactor) / rigidBodyMass * FixedDeltaTime;
            }
            else
            {
                FunctionProxy_Float1 = -nowSpeed - changedSpeed;
                FunctionProxy_Vector1.z = FunctionProxy_Float1;
                rigidbody_.AddRelativeForce(FunctionProxy_Vector1, ForceMode.VelocityChange);
                lastSpeed = -changedSpeed;
            }
            if (hasAnimator)
            {
                controllerAnimator.SetFloat(rigidBodySpeedParamaterID, nowSpeed / 100);
            }
            positionBogie_F = Bogie_F.position;
            positionBogie_B = Bogie_B.position;
            BogieCalculateNextPos();
            //if (currentVelocity.sqrMagnitude > 0.0001f)
            //{
            //    PlayFlangeSound();
            //}
            //else
            //{
            //    stopAudioSource(ref FlangeSound_F_stop, FlangeSound_F);
            //    stopAudioSource(ref FlangeSound_B_stop, FlangeSound_B);
            //}
        }

        float initWaiting = 0;
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
            rigidbody_.AddRelativeForce(calculatedVelocity, ForceMode.VelocityChange);
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
                    //trainManager.nowSynced = false;
                    trainManager.SendCustomEvent(nameof(trainManager.ReSyncRequest));
                    RailErrorTime = 0;
                }
            }
            else
            {
                if (RailErrorTime > 0) RailErrorTime -= FixedDeltaTime * 2;
            }
        }
        //public void PlayFlangeSound()
        //{
        //    float flangeDot_F = Mathf.Abs(Vector3.Dot(currentVelocity, BogieWheel_F.transform.forward));
        //
        //    //Debug.Log("flangeDot_F" + flangeDot_F);
        //    if (currentVelocity.magnitude - flangeDot_F > 0.001f)
        //    {
        //        playAudioSource(ref FlangeSound_F_stop, FlangeSound_F, (currentVelocity.magnitude - flangeDot_F - 0.001f) * 2000);
        //    }
        //    else if (!FlangeSound_F_stop)
        //    {
        //        stopAudioSource(ref FlangeSound_F_stop, FlangeSound_F);
        //    }
        //    float flangeDot_B = Mathf.Abs(Vector3.Dot(currentVelocity, BogieWheel_B.transform.forward));
        //    if (currentVelocity.magnitude - flangeDot_B > 0.001f)
        //    {
        //        playAudioSource(ref FlangeSound_B_stop, FlangeSound_B, (currentVelocity.magnitude - flangeDot_B - 0.001f) * 2000);
        //    }
        //    else if (!FlangeSound_B_stop)
        //    {
        //        stopAudioSource(ref FlangeSound_B_stop, FlangeSound_B);
        //    }
        //}

        void playAudioSource(ref bool isStop, AudioSource audioSource, float volume)
        {
            if (isStop)
            {
                audioSource.enabled = true;
                audioSource.Play();
                isStop = false;
            }
            audioSource.volume = volume;
        }
        void stopAudioSource(ref bool isStop, AudioSource audioSource)
        {
            if (!isStop)
            {
                audioSource.Stop();
                isStop = true;
                audioSource.volume = 0;
                audioSource.enabled = false;
            }
        }





        float FconnectedPr;
        float BconnectedPr;

        float targetPressure;
        void Update()
        {
            if (!brakeUpdateBypass && brakePressure.localPosition.y == prevBrakePressure)//not interrupt
            {
                brakePressure_proxy.y = brakePressure_float;
                brakePressure.localPosition = brakePressure_proxy;
            }
        }

        public void LateUpdate()
        {
            if (!started) return;
            brakePressure_proxy = brakePressure.localPosition;
            if (isOwnerState) brakePressure_float = brakePressure_proxy.y;

            FconnectedPr = BrakeOpenF ? (ConnectedBrakePressureF == null ? 0f : ConnectedBrakePressureF.localPosition.y) : brakePressure_float;
            BconnectedPr = BrakeOpenB ? (ConnectedBrakePressureB == null ? 0f : ConnectedBrakePressureB.localPosition.y) : brakePressure_float;

            targetPressure = (FconnectedPr + brakePressure_float + BconnectedPr) / 3;

            float diffPres = (brakePressure_float - targetPressure);
            if (diffPres > 0.3f)
            {
                brakePressure_float = LinearMoveParam(brakePressure_float, 10 * Time.deltaTime,
                    targetPressure);
            }
            else
            {
                brakePressure_float = LinearMoveParam(brakePressure_float, 0.1f * Time.deltaTime,
                    targetPressure);
            }

            currentFriction = (1 / (1 + Mathf.Abs(localVelocity.z) * 10)) * static_friction + friction;
            brakeFactor = (1 - brakePressure_float) * 3.57f;// * 5/(5-((5-1.4)))
            if (brakeFactor > 1) brakeFactor = 1;
            if (brakeFactor < 0) brakeFactor = 0;
            brakeFactor *= BrakeMultiplier * (0.5f + 0.5f / (1 + Mathf.Abs(localVelocity.z) / 5));
            brakeFactor += (handBrakeState ? handBrakeForce : 0) + currentFriction;


            if (hasAnimator)
            {
                controllerAnimator.SetFloat(brakePressureParamaterID, brakePressure_float);
                handBrakeState = controllerAnimator.GetBool(handBrakeStateID);
                handBrakeForce = controllerAnimator.GetFloat(handBrakeForceID);
            }
            prevBrakePressure = brakePressure.localPosition.y;
        }

        public void setCoupler(CouplerObj couplerObj, bool F_B)
        {
            if (F_B)
            {
                Coupler_InitedF = true;
            }
            else
            {
                Coupler_InitedB = true;
            }
        }

        public Train connectedTrain_F;
        public Train connectedTrain_B;
        public void setConnectedTrain(Train connectedTrain, bool F_B)//車両連結/解結
        {
            if (F_B)
            {
                if (connectedTrain != null) ConnectedBrakePressureF = connectedTrain.brakePressure;
                else ConnectedBrakePressureF = null;
                connectedTrain_F = connectedTrain;


                if (connectedTrain_F != null && Networking.GetOwner(connectedTrain_F.gameObject) != Networking.GetOwner(gameObject))
                    Networking.SetOwner(Networking.GetOwner(gameObject), connectedTrain_F.gameObject);

            }
            else
            {
                if (connectedTrain != null) ConnectedBrakePressureB = connectedTrain.brakePressure;
                else ConnectedBrakePressureB = null;
                connectedTrain_B = connectedTrain;
                if (connectedTrain_B != null && Networking.GetOwner(connectedTrain_B.gameObject) != Networking.GetOwner(gameObject))
                    Networking.SetOwner(Networking.GetOwner(gameObject), connectedTrain_B.gameObject);
            }
            if (connectedTrain_F != null && connectedTrain_B != null)
            {
                syncInterval = 2.9f + Random.Range(0, 0.2f);
            }
            else
            {
                syncInterval = 0.2f;
            }
            nextSync = syncInterval;
            expectedSyncedPosition = syncedPosition = transform.localPosition;
            if(syncedVelocity != Vector3.zero) expectedSyncedVelocity = syncedVelocity = currentVelocity;
            if (stopSync)
            {
                expectedSyncedVelocity = syncedVelocity = Vector3.zero;
            }
            updatePredicteBezier();
        }

        public void changeBrakeValve(bool F_B)//空制弁開放/閉鎖
        {
            if (F_B)
            {
                BrakeOpenF = !BrakeOpenF;
            }
            else
            {
                BrakeOpenB = !BrakeOpenB;
            }
            needSync = true;
        }

        public void PerformHandBrake()
        {
            handBrakeState = !handBrakeState;
        }

        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            fromLastSync = 0;
            nextSync = syncInterval;

            if (Networking.LocalPlayer == player)
            {
                Debug.Log("transfering subObject owner " + GetHierarchyPath(transform));
                isDiscontinuitySync = true;

                Networking.SetOwner(player, brakePressure.gameObject);
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

        public bool CanTransferOwner;
        public void OwnerShipLinkTransfer()
        {
            CanTransferOwner = currentVelocity.sqrMagnitude < 0.1f;
        }
        public string GetHierarchyPath(Transform transform)
        {
            string text = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                text = transform.name + "/" + text;
            }

            return text;
        }

        public float LinearMoveParam(float param, float speed, float target)
        {
            if (Mathf.Abs(param - target) > speed)
            {
                if (param > target)
                {
                    param -= speed;
                }
                else
                if (param < target)
                {
                    param += speed;
                }
            }
            else
            {
                param = target;
            }
            return param;
        }

        float bodyCenterInterpole_F;


        //前ボギー フィールド ココから
        [SerializeField] private Transform Bogie_F;
        [SerializeField] private Rigidbody BogieWheel_F;
        //private Vector3 BogieAccel_F;
        [System.NonSerialized] public float onRailPoint_F;
        private Vector3 onRailPosition_F;

        public int RailID_F;
        [SerializeField] public Rail_Script BogieRail_F;
        private CinemachinePathBase BogieCinemachinePath_F;
        private float railMaxPoint_F;

        private Vector3 RailStartPoint_F;
        private Vector3 RailEnd__Point_F;

        private bool moveableRail_F;
        //private Vector3 prevRailPosition_F;
        //private Quaternion prevRailRotation_F;


        //後ボギー フィールド ココから
        [SerializeField] private Transform Bogie_B;
        [SerializeField] private Rigidbody BogieWheel_B;
        //private Vector3 BogieAccel_B;
        [System.NonSerialized] public float onRailPoint_B;
        private Vector3 onRailPosition_B;

        public int RailID_B;
        [SerializeField] public Rail_Script BogieRail_B;
        private CinemachinePathBase BogieCinemachinePath_B;
        private float railMaxPoint_B;

        private Vector3 RailStartPoint_B;
        private Vector3 RailEnd__Point_B;

        private bool moveableRail_B;
        //private Vector3 prevRailPosition_B;
        //private Quaternion prevRailRotation_B;




        [UdonSynced] public int SyncedRailID_F;//4byte
        [UdonSynced] public int SyncedRailID_B;//4byte
        [UdonSynced] public float SyncedRailPoint_F;//4byte
        [UdonSynced] public float SyncedRailPoint_B;//4byte
        [UdonSynced] public bool SyncedRailUnitOrder;//1byte

        public void BogieStart()
        {
            BogieRail_F.Start();
            onRailPoint_F = BogieRail_F.GetF(Bogie_F.position);

            copyRailProperties_F();
            onRailPosition_F = BogieCinemachinePath_F.EvaluatePosition(onRailPoint_F);


            BogieRail_B.Start();
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





        float BogieToWheelPosLengthF;
        bool tooLongDiffF;
        float BogieToWheelPosLengthB;
        bool tooLongDiffB;

        Vector3 prevpositionBogieF;
        Vector3 prevpositionBogieB;

        bool isDirty;

        Vector3 tempVector;
        bool orProxy;
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
            if (orProxy)
            {
                isDirty = true;
                onRailPoint_F = BogieCinemachinePath_F.FindClosestPoint(positionBogie_F, (int)onRailPoint_F, 1, pathResolution);
                prevpositionBogieF = positionBogie_F;
            }else if (Vector3.Distance(positionBogie_F, prevpositionBogieF) > 0.01f)
            {
                isDirty = true;
                tempVector = BogieCinemachinePath_F.EvaluateTangent(onRailPoint_F);
                float Dot = Vector3.Dot(positionBogie_F - prevpositionBogieF, tempVector.normalized);
                onRailPoint_F += Dot / tempVector.magnitude;
                if (onRailPoint_F < 0) onRailPoint_F = 0;
                if (onRailPoint_F > railMaxPoint_F) onRailPoint_F = railMaxPoint_F;
                prevpositionBogieF = positionBogie_F;
            }
            orProxy = false;
            if (moveableRail_F) orProxy = true;
            if (isDirty) orProxy = true;
            if (tooLongDiffF) orProxy = true;
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
            else if (Vector3.Distance(positionBogie_B, prevpositionBogieB) > 0.01f)
            {
                tempVector = BogieCinemachinePath_B.EvaluateTangent(onRailPoint_B);
                float Dot = Vector3.Dot(positionBogie_B - prevpositionBogieB, tempVector.normalized);
                isDirty = true;
                onRailPoint_B += Dot / tempVector.magnitude;
                if (onRailPoint_B < 0) onRailPoint_B = 0;
                if (onRailPoint_B > railMaxPoint_B) onRailPoint_B = railMaxPoint_B;
                prevpositionBogieB = positionBogie_B;
            }
            orProxy = false;
            if (moveableRail_B) orProxy = true;
            if (isDirty) orProxy = true;
            if (tooLongDiffB) orProxy = true;
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
                BogieToWheelPosLengthF = tempVector.sqrMagnitude;
                tooLongDiffF = BogieToWheelPosLengthB > 0.1f;
                prevpositionBogieF = onRailPosition_F;
            }
            else
            {
                BogieWheel_B.position = onRailPosition_B = BogieCinemachinePath_B.EvaluatePosition(onRailPoint_B);
                BogieWheel_B.rotation = BogieCinemachinePath_B.EvaluateOrientation(onRailPoint_B);
                tempVector = onRailPosition_B - positionBogie_B;
                BogieToWheelPosLengthB = tempVector.sqrMagnitude;
                tooLongDiffB = BogieToWheelPosLengthB > 0.1f;
                prevpositionBogieB = onRailPosition_B;
            }
        }

        private void tryChangeRailF()
        {
            if (BogieToWheelPosLengthF > (onRailPosition_F - RailEnd__Point_F).sqrMagnitude)
            {
                changeRailF(true);
            }
            else if (BogieToWheelPosLengthF > (onRailPosition_F - RailStartPoint_F).sqrMagnitude)
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
                if (!BogieRail_F.started) BogieRail_F.Start();
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
            if (BogieToWheelPosLengthB > (onRailPosition_B - RailEnd__Point_B).sqrMagnitude)
            {
                changeRailB(true);
            }
            else if (BogieToWheelPosLengthB > (onRailPosition_B - RailStartPoint_B).sqrMagnitude)
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
                if (!BogieRail_B.started) BogieRail_B.Start();
                BogieCinemachinePath_B = BogieRail_B.cinemachinePath;
                moveableRail_B = BogieRail_B.moveableRail;
                RailStartPoint_B = BogieRail_B.GetStartPoint();
                RailEnd__Point_B = BogieRail_B.GetEndPoint();
                railMaxPoint_B = BogieCinemachinePath_B.MaxPos;
                RailID_B = BogieRail_B.RailID;
                RailErrorTime = 0;
            }

        }
        Vector3 prevSyncedVelocity;
        Vector3 prevSyncedPosition;
        Vector3 expectedSyncedPosition;
        Vector3 expectedSyncedVelocity;
        [UdonSynced] Vector3 syncedPosition;//12byte
        [UdonSynced] Vector3 syncedVelocity;//12byte
        [UdonSynced] float syncInterval = 1;//4byte
        float nextSync = 1;

        float fromLastSync;

        public bool InitsyncRecieveMode = true;

        [UdonSynced] public bool isDiscontinuitySync;
        public void resync()
        {
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

        float uPBa;
        float uPBb;
        float uPBc;
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
                SendCustomEventDelayedFrames(nameof(resync), Random.Range(1,5));
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
    }


}
