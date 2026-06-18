using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace frou01.RigidBodyTrain
{
    [DefaultExecutionOrder(-10)]
    public class Train : UdonSharpBehaviour
    {
        //PresetByBuildProcess
        [HideInInspector] public TrainManager trainManager;
        [HideInInspector] public RailsManager railsManager;
        [HideInInspector] public int TrainID;//連結他同期用


        //tips:Max 1600bit

        [SerializeField] private Vector3 CenterOfMass;

        public CouplerObj CouplerF;
        public CouplerObj CouplerB;

        //public Vector3 gravity = new Vector3(0, -9.8f, 0);

        [SerializeField] Animator controllerAnimator;

        [SerializeField] public TrainConnectionReciever[] connectionRecievers;
        [SerializeField] public GameObject[] subObjects;
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

            foreach (TrainConnectionReciever reciever in connectionRecievers)
            {
                reciever.TrainConnectionUpdate(connectedTrain, F_B);
            }
        }
        [SerializeField]Bogie_Script[] bogies;



        [HideInInspector][SerializeField] GameObject mineGameObject;
        [HideInInspector][SerializeField] Transform chacedTransform;
        Rigidbody rigidbody_;
        bool isOwnerState;
        bool started = false;
        private bool hasAnimator;
        private int rigidBodySpeedParamaterID;
        public void Start()//Call by BuildProcess
        {
            rigidbody_ = GetComponent<Rigidbody>();
            bodyCenterInterpole_F =
                Mathf.Abs(bogies[0].Bogie.localPosition.z) / Mathf.Abs(bogies[0].Bogie.localPosition.z - bogies[bogies.Length-1].Bogie.localPosition.z);
            mineGameObject = gameObject;
            chacedTransform = transform;
            hasAnimator = controllerAnimator != null;
            rigidBodySpeedParamaterID = Animator.StringToHash("RigidBodySpeed");
            FixedDeltaTime = Time.fixedDeltaTime;
            //rigidBodyMass = rigidbody_.mass;
            foreach(Bogie_Script bogie in bogies)
            {
                bogie.ParentTrain = this;
                bogie.BogieInit();
            }
            setPositionFromBogie();


            setConnectedTrain(connectedTrain_F,true);
            setConnectedTrain(connectedTrain_B,false);
        }
        private void PostStart()
        {
            if (Networking.IsOwner(mineGameObject))
            {
                releaseLock();
            }
            isOwnerState = Networking.IsOwner(mineGameObject);
            exposedOwnerState[0] = isOwnerState;
            //transform.parent = null;
        }

        private float FixedDeltaTime;
        private Vector3 currentVelocity;
        private Vector3 localVelocity;
        [SerializeField][HideInInspector]public float[] distanceErrorThreshold = new float[1];
        private float m_nowSpeed;
        [HideInInspector] public float[] Rigidbody_Speed_LocalZ = new float[1];
        [HideInInspector] public bool[] exposedOwnerState = new bool[1];
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
            if (isOwnerState)
            {
            }
            else
            {
                onRemote();
            }
            if (hasAnimator)
            {
                controllerAnimator.SetFloat(rigidBodySpeedParamaterID, m_nowSpeed / 100);
            }
            distanceErrorThreshold[0] = FixedDeltaTime * (1 + Mathf.Abs(m_nowSpeed));
            Rigidbody_Speed_LocalZ[0] = m_nowSpeed;
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
        }

        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
            if (Networking.LocalPlayer == player)
            {
                isDiscontinuitySync = true;
                Debug.Log("transfering subObject owner " + GetHierarchyPath(transform));
                Networking.SetOwner(player, CouplerF.gameObject);
                Networking.SetOwner(player, CouplerB.gameObject);
                foreach (GameObject subobject in subObjects)
                {
                    Networking.SetOwner(player, subobject);
                }
                if (connectedTrain_F != null)
                    Networking.SetOwner(player, connectedTrain_F.gameObject);
                if (connectedTrain_B != null)
                    Networking.SetOwner(player, connectedTrain_B.gameObject);

                rigidbody_.isKinematic = false;
                rigidbody_.centerOfMass = CenterOfMass;
                rigidbody_.ResetInertiaTensor();
            }
            isOwnerState = Networking.IsOwner(mineGameObject);
            exposedOwnerState[0] = isOwnerState;
            if (isOwnerState)
            {
                rigidbody_.useGravity = true;
            }
            else
            {
                rigidbody_.useGravity = false;
            }
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

        public void setPositionFromBogie()
        {
            foreach (Bogie_Script bogie in bogies)
            {
                bogie.ApplyRailPointToTransform();
            }



            Vector3 forward = bogies[0].UnderRail.position - bogies[bogies.Length-1].UnderRail.position;
            if (forward.sqrMagnitude > 0.001)
            {
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.up = Vector3.up;
                gameObject.transform.forward = forward;
                rigidbody_.rotation = gameObject.transform.rotation;
            }


            gameObject.transform.position = bogies[0].UnderRail.position * (1 - bodyCenterInterpole_F) + bogies[bogies.Length - 1].UnderRail.position * bodyCenterInterpole_F;

            rigidbody_.position = (gameObject.transform.position);

        }

        [HideInInspector] public bool InitsyncRecieveMode = true;
        [System.NonSerialized] public System.DateTime LastSent_Resync = System.DateTime.MinValue;

        [HideInInspector] [UdonSynced] private bool isDiscontinuitySync;
        public void resync()
        {
            isOwnerState = Networking.IsOwner(mineGameObject);
            exposedOwnerState[0] = isOwnerState;
            isDiscontinuitySync = true;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            isOwnerState = false;
            if (!isDiscontinuitySync)
            {
            }
            else if (InitsyncRecieveMode)
            {
                Debug.Log("Init Sync");



                InitsyncRecieveMode = false;

                Debug.Log("trainID " + TrainID);

                setPositionFromBogie();

                rigidbody_.isKinematic = true;
                SendCustomEventDelayedSeconds(nameof(releaseLock), 0.4f);

                Debug.Log("position " + transform.localPosition);
            }

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
            rigidbody_.velocity = Vector3.zero;
            rigidbody_.isKinematic = false;
            rigidbody_.centerOfMass = CenterOfMass;
            rigidbody_.ResetInertiaTensor();
        }

        private Vector3 drawingPos;
        private Quaternion drawingRot;
        private Vector3 drawingStart;
        private Vector3 drawingEnd;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawSphere(transform.TransformPoint(CenterOfMass), 0.3f);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Gizmos.DrawSphere(transform.TransformPoint(CenterOfMass), 0.3f);
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            Gizmos.color = new Color(1f, 0f, 0f, 1f);
            if(connectedTrain_F != null) drawKnucle(connectedTrain_F,CouplerF);
            Gizmos.color = new Color(0f, 0f, 1f, 1f);
            if (connectedTrain_B != null) drawKnucle(connectedTrain_B,CouplerB);
        }

        void drawKnucle(Train connectedTrain, CouplerObj ConnectingCoupler)
        {
            Vector3 drawingPos;
            Quaternion drawingRot;
            Vector3 drawingStart;
            Vector3 drawingEnd;
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
