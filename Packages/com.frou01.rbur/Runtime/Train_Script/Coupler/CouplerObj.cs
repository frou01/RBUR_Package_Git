
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEditor;
using VRC.Udon.Common.Interfaces;
using TMPro;

namespace frou01.RigidBodyTrain
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CouplerObj : UdonSharpBehaviour
    {
        //[System.NonSerialized] public Rigidbody couplerRigidBody;

         public Train TrainScript;
        [System.NonSerialized] Rigidbody MotherTrain_RigidBody;
        //[System.NonSerialized] Rigidbody ConnecTrain_RigidBody;

        [System.NonSerialized] TrainManager trainManager;

        [System.NonSerialized] public Rigidbody anchorBody;
        [System.NonSerialized] public ConfigurableJoint joint;
        [System.NonSerialized] public ConfigurableJoint connectedJoint;



        //private Vector3 initialPos;
        //private Quaternion initialRotation;

        [UdonSynced(UdonSyncMode.None)] public bool Knuckle_Closed = true;//falseでナックルが開
        [UdonSynced(UdonSyncMode.None)] public byte state;//0:固定 1:閉じたら開かない 2:錠控え
        [System.NonSerialized] [UdonSynced(UdonSyncMode.None)] public int ConnectedTrainID = -1;
        [System.NonSerialized] [UdonSynced(UdonSyncMode.None)] public bool ConnectedCouplerFB;
        [System.NonSerialized] [UdonSynced(UdonSyncMode.None)] public bool crashed;



        [SerializeField] public AudioSource CouplerAudioSource;
        [SerializeField] public AudioClip connectSound;
        [SerializeField] public AudioClip missConnectSound;
        [SerializeField] public AudioClip unCoupleSound;
        [SerializeField] public AudioClip OpenSound;
        [SerializeField] public AudioClip unLockSound;
        [SerializeField] public AudioClip CloseSound;

        [SerializeField] public bool FrontOrBack;
        [SerializeField] private float disconnectForce = 1000;


        public CouplerObj connectedCoupler;

        bool started = false;

        //Rigidbody rigidbody_;
        //Vector3 InertiaTensor;
        [SerializeField] GameObject knuckleModel;
        [SerializeField] GameObject knuckleKey;
        VRCPlayerApi localPlayer;
        public void Start()
        {
            Initialize();

            if (connectedCoupler != null)
            {
                connectedCoupler.Initialize();
                knuckleClose();
                connectedCoupler.knuckleClose();
                this.setConnectedCoupler(connectedCoupler);
                connectedCoupler.setConnectedCoupler(this);
            }

            //couplerRigidBody.isKinematic = false;
            if (connectedCoupler != null) TrainScript.setConnectedTrain(connectedCoupler.TrainScript, FrontOrBack);
            else TrainScript.setConnectedTrain(null, FrontOrBack);
            TrainScript.setCoupler(this, FrontOrBack);
            started = true;
            localPlayer = Networking.LocalPlayer;
        }

        float jointLinerLimit;

        [System.NonSerialized]public Transform chachedTransform;
        Transform connectedTransform;
        public void Initialize()
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

            //rigidbody_ = GetComponent<Rigidbody>();
            //InertiaTensor = rigidbody_.inertiaTensor;
            //GetComponent<Collider>().isTrigger = true;
            //rigidbody_.inertiaTensor = InertiaTensor;
        }
        private void Update()
        {
            if (!started) return;
            if ((this.state != 0 || (crashed && overCoolTime)) && connectedCoupler != null && Networking.IsOwner(gameObject))
            {

                if (chachedTransform.InverseTransformVector(joint.currentForce).z * 100000 + connectedTransform.InverseTransformVector(connectedJoint.currentForce).z * 100000 > (crashed ? 0 : disconnectForce))
                {
                    if (this.state != 0 || crashed)
                    {
                        disConnect();
                    }
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
                if (Knuckle_Closed) CouplerAudioSource.PlayOneShot(OpenSound);
                else CouplerAudioSource.PlayOneShot(unLockSound);
                Knuckle_Closed = false;
            }
            else
            {
                CouplerAudioSource.PlayOneShot(unLockSound);
            }
            if (Networking.IsOwner(gameObject)) RequestSerialization();
            UpdateKnuckleModel();
        }
        public void knuckleClose()
        {
            state = 0;
            if (!Knuckle_Closed) CouplerAudioSource.PlayOneShot(CloseSound);
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
            if (knuckleModel == null) return;
            if (!Knuckle_Closed)
            {
                knuckleModel.transform.localRotation = Quaternion.Euler(0, 75, 0);
            }
            else
            {
                knuckleModel.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

        public void changeBrakeValve()
        {
            TrainScript.changeBrakeValve(FrontOrBack);
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
            Debug.Log(crashed);
            miss = crashed;
            this.connectedCoupler = connectingCoupler;
            if (connectingCoupler != null)
            {
                joint.connectedBody = connectingCoupler.MotherTrain_RigidBody;
                joint.connectedAnchor = connectingCoupler.joint.anchor;
                connectedJoint = connectingCoupler.joint;
                connectedTransform = connectingCoupler.chachedTransform;
                //ConnecTrain_RigidBody = connectingCoupler.TrainScript.GetComponent<Rigidbody>();
                if (TrainScript.started)
                {
                    TrainScript.setConnectedTrain(connectingCoupler.TrainScript, FrontOrBack);
                }
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
                if (TrainScript.started) TrainScript.setConnectedTrain(null, FrontOrBack);


                ConnectedTrainID = -1;
            }
            //Debug.Log(ConnectedTrainID);
            if (Networking.IsOwner(gameObject))
            {
                RequestSerialization();
            }
            if (prevID != -2 && prevID != ConnectedTrainID)
            {
                if(miss)
                {
                    CouplerAudioSource.PlayOneShot(missConnectSound);
                }
                else
                if (ConnectedTrainID == -1)
                {
                    CouplerAudioSource.PlayOneShot(unCoupleSound);
                }
                else
                {
                    CouplerAudioSource.PlayOneShot(connectSound);
                }
            }
            prevID = ConnectedTrainID;
        }

        int prevID = -2;


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
        byte prevstate = 0;//0:固定 1:閉じたら開かない 2:錠控え

        public override void OnDeserialization()
        {
            if(state != prevstate)
            {
                if(state == 2)
                {
                    CouplerAudioSource.PlayOneShot(unLockSound);
                }
                else if(state == 1 && prevstate == 0)
                {
                    CouplerAudioSource.PlayOneShot(OpenSound);
                }
            }
            if(prevKnuckle_Closed && !Knuckle_Closed)
            {
                CouplerAudioSource.PlayOneShot(CloseSound);
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
