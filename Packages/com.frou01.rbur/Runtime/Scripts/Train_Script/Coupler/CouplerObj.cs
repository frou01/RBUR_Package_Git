
using frou01.util;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CouplerObj : UdonSharpBehaviour
    {
        //[SerializeField][HideInInspector] public Rigidbody couplerRigidBody;

        [Tooltip("this object attached Train. auto assing by BuildProcess")]public Train TrainScript;
        [SerializeField][HideInInspector] Rigidbody MotherTrain_RigidBody;
        //[SerializeField][HideInInspector] Rigidbody ConnecTrain_RigidBody;

        [SerializeField][HideInInspector] TrainManager trainManager;

        [SerializeField][HideInInspector] public Rigidbody anchorBody;
        [SerializeField][HideInInspector] public ConfigurableJoint joint;
        [SerializeField][HideInInspector] public ConfigurableJoint connectedJoint;



        //private Vector3 initialPos;
        //private Quaternion initialRotation;

        [UdonSynced(UdonSyncMode.None)] public bool Knuckle_Closed = true;//falseでナックルが開
        [Tooltip("coupler state: 0.Closed, 1.Open(Connection waiting), 2.Unlock(Disconnection Waiting)")]
        [UdonSynced(UdonSyncMode.None)] public byte state;
        [SerializeField][HideInInspector] [UdonSynced(UdonSyncMode.None)] public int ConnectedTrainID = -1;
        [SerializeField][HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool ConnectedCouplerFB;
        [SerializeField][HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool crashed;



        [SerializeField] public AudioSource CouplerAudioSource;
        [SerializeField] public AudioClip connectSound;
        [SerializeField] public AudioClip missConnectSound;
        [SerializeField] public AudioClip unCoupleSound;
        [SerializeField] public AudioClip OpenSound;
        [SerializeField] public AudioClip unLockSound;
        [SerializeField] public AudioClip CloseSound;

        [Tooltip("is this front(+Z)?")][SerializeField] public bool FrontOrBack;
        [Tooltip("disconnection threshold force")][SerializeField] private float disconnectForce = 1000;


        [SerializeField][HideInInspector] CouplerObj connectedCoupler;

        bool started = false;

        //Rigidbody rigidbody_;
        //Vector3 InertiaTensor;
        [SerializeField] GameObject knuckleModel;
        [SerializeField] SyncedObjectToggle ObjectToggle_Knuckle;
        [SerializeField] GameObject knuckleKey;
        [SerializeField] SyncedObjectSwitch ObjectSwitch_Key;

        [Tooltip("Auto Assign by BuildProcess")]
        public AbstractBrake BrakeModule;
        public void Start()
        {
            Initialize();

            //couplerRigidBody.isKinematic = false;
            started = true;

            TrainConnectionReciever foundModule = (TrainScript.GetConnectionRecieverByTag("Brake"));
            if (foundModule) BrakeModule = ((AbstractBrake)foundModule);
        }

        float jointLinerLimit;

        [SerializeField][HideInInspector]public Transform chachedTransform;
        [SerializeField][HideInInspector] Transform connectedTransform;
        public void Initialize()//CallFromBuildProcess
        {
            //initialPos = transform.localPosition;
            //initialRotation = transform.localRotation;

            if (TrainScript == null)
            {
                TrainScript = transform.parent.gameObject.GetComponent<Train>();
            }
            if (trainManager == null) trainManager = TrainScript.trainManager;

            MotherTrain_RigidBody = TrainScript.GetComponent<Rigidbody>();

            //couplerRigidBody = GetComponent<Rigidbody>();
            joint = TrainScript.GetComponents<ConfigurableJoint>()[FrontOrBack ? 0:1];
            joint.anchor = this.transform.localPosition;
            jointLinerLimit = joint.linearLimit.limit;
            anchorBody = transform.Find("anchorBody").gameObject.GetComponent<Rigidbody>();
            anchorBody.transform.localPosition = Vector3.zero;
            chachedTransform = transform;
            trainManager = TrainScript.trainManager;
            UpdateKnuckleModel();

            //rigidbody_ = GetComponent<Rigidbody>();
            //InertiaTensor = rigidbody_.inertiaTensor;
            //GetComponent<Collider>().isTrigger = true;
            //rigidbody_.inertiaTensor = InertiaTensor;
        }
        private void Update()
        {
            if (!started) return;
            if ((this.state != 0 || (crashed && overCoolTime)) && connectedCoupler != null && connectedTransform != null && Networking.IsOwner(gameObject))
            {

                if (chachedTransform.InverseTransformVector(joint.currentForce).z + connectedTransform.InverseTransformVector(connectedJoint.currentForce).z > (crashed ? 0 : disconnectForce))
                {
                    disConnect();
                }
            }
            ;
        }

        
        public override void OnOwnershipTransferred(VRC.SDKBase.VRCPlayerApi player)
        {
        }

        public void couplerUnlock()
        {
            Debug.Log("tryCouplerOpen");
            state = 2;
            knuckleOpen();
        }
        public void couplerLock()
        {
            knuckleClose();
        }

        private void knuckleOpen()
        {
            if (state == 0) return;
            if (connectedCoupler == null)
            {
                if (state == 2) state = 1;
                if (Knuckle_Closed) if(CouplerAudioSource) CouplerAudioSource.PlayOneShot(OpenSound);
                else if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(unLockSound);
                Knuckle_Closed = false;
            }
            else
            {
                if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(unLockSound);
            }
            if (Networking.IsOwner(gameObject)) RequestSerialization();
            UpdateKnuckleModel();
        }
        public void knuckleClose()
        {
            state = 0;
            if (!Knuckle_Closed) if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(CloseSound);
            Knuckle_Closed = true;
            if (Networking.IsOwner(gameObject)) RequestSerialization();
            UpdateKnuckleModel();
        }
        public void reLockCoupler()
        {
            if (state == 2)
            {
                state = 0;
                Knuckle_Closed = true;
                if (Networking.IsOwner(gameObject)) RequestSerialization();
            }
            UpdateKnuckleModel();
        }

        public void UpdateKnuckleModel()
        {
            if (knuckleModel)
            {

                if (!Knuckle_Closed)
                {
                    knuckleModel.transform.localRotation = Quaternion.Euler(0, 75, 0);
                }
                else
                {
                    knuckleModel.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }
            if(ObjectToggle_Knuckle)
            {
                ObjectToggle_Knuckle.setState(Knuckle_Closed);
            }
            if (ObjectSwitch_Key)
            {
                ObjectSwitch_Key.setState(state);
            }
        }


        private void disConnect()
        {
            CouplerObj disConnecting = connectedCoupler;
            setConnectedCoupler(null);
            if (disConnecting != null)
            {
                disConnecting.setConnectedCoupler(null);
            }
            disConnecting.knuckleOpen();
            this.knuckleOpen();
        }


        private void OnTriggerEnter(Collider other)
        {
            if (Networking.IsOwner(gameObject))
            {
                if (connectedCoupler == null && other.gameObject.GetComponent<CouplerObj>() != null)
                {
                    connect(other);
                }
            }
        }

        private void connect(Collider other)
        {
            //Debug.Log(this.gameObject + " tryConnect To " + other);
            CouplerObj connectingCoupler = other.gameObject.GetComponent<CouplerObj>();
            //Debug.Log(connectingCoupler);
            if (connectingCoupler == null) return;

            if (this.Knuckle_Closed && connectingCoupler.Knuckle_Closed)
            {
                this.crashed = true;
                connectingCoupler.crashed = true;
            }

            //connectSound.Play();
            this.setConnectedCoupler(connectingCoupler);
            connectingCoupler.setConnectedCoupler(this);
            knuckleClose();
            connectingCoupler.knuckleClose();
            overCoolTime = false;
            if (crashed) SendCustomEventDelayedSeconds(nameof(applyCrash), Time.fixedDeltaTime * 2);
        }

        bool overCoolTime = false;
        public void applyCrash()
        {
            overCoolTime = true;
        }

        bool miss;
        public void setConnectedCoupler(CouplerObj connectingCoupler)
        {

            //連結したら連結した連結器の"車両ID,連結器前後"を取る。nullは-1
            //マスターの場合のみ上記を同期
            //Debug.Log(crashed);
            miss = crashed;
            this.connectedCoupler = connectingCoupler;
            if (connectingCoupler != null)
            {
                joint.connectedBody = connectingCoupler.MotherTrain_RigidBody;
                joint.connectedAnchor = connectingCoupler.joint.anchor;
                connectedJoint = connectingCoupler.joint;
                connectedTransform = connectingCoupler.chachedTransform;
                //ConnecTrain_RigidBody = connectingCoupler.TrainScript.GetComponent<Rigidbody>();

                TrainScript.setConnectedTrain(connectingCoupler.TrainScript, FrontOrBack);
                ConnectedTrainID = connectingCoupler.TrainScript.TrainID;
                ConnectedCouplerFB = connectingCoupler.FrontOrBack;
            }
            else
            {
                crashed = false;
                joint.connectedBody = anchorBody;
                joint.connectedAnchor = Vector3.zero;
                connectedJoint = null;
                connectedTransform = null;
                //ConnecTrain_RigidBody = null;
                TrainScript.setConnectedTrain(null, FrontOrBack);


                ConnectedTrainID = -1;
            }
            //Debug.Log(ConnectedTrainID);
            if (Networking.IsOwner(gameObject))
            {
                RequestSerialization();
            }
            if (prevID != ConnectedTrainID)
            {
                if(miss)
                {
                    if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(missConnectSound);
                }
                else
                if (ConnectedTrainID == -1)
                {
                    if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(unCoupleSound);
                }
                else
                {
                    if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(connectSound);
                }
            }
            prevID = ConnectedTrainID;
        }

        int prevID = -1;


        public override void OnPreSerialization()
        {
            if (connectedCoupler != null)
            {
                ConnectedTrainID = connectedCoupler.TrainScript.TrainID;
                ConnectedCouplerFB = connectedCoupler.FrontOrBack;
            }
            else
            {
                ConnectedTrainID = -1;
            }
        }
        bool prevKnuckle_Closed = true;//falseでナックルが開
        byte prevstate = 0;

        public override void OnDeserialization()
        {
            if(state != prevstate)
            {
                if(state == 2)
                {
                    if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(unLockSound);
                }
                else if(state == 1 && prevstate == 0)
                {
                    if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(OpenSound);
                }
            }
            if(prevKnuckle_Closed && !Knuckle_Closed)
            {
                if (CouplerAudioSource) CouplerAudioSource.PlayOneShot(CloseSound);
            }
            //Debug.Log(ConnectedTrainID);
            if (ConnectedTrainID != -1)
            {
                Debug.Log(trainManager.Trains[ConnectedTrainID].name);
                if (ConnectedCouplerFB)
                    setConnectedCoupler(trainManager.Trains[ConnectedTrainID].CouplerF);
                else
                    setConnectedCoupler(trainManager.Trains[ConnectedTrainID].CouplerB);

            }
            else
            {
                setConnectedCoupler(null);
            }
            UpdateKnuckleModel();
        }

    }

}
