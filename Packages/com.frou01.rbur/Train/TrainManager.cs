
using UdonSharp;
using Unity.Collections;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    public class TrainManager : UdonSharpBehaviour
    {
        public Train[] Trains;

        public int pathRes;

        public RailsManager railsManager;

        [System.NonSerialized] public int trainsNum = 0;
        [System.NonSerialized] public int id;


        [UdonSynced(UdonSyncMode.None)] public int[] BogieRailID;//偶数：前 奇数：後

        [UdonSynced(UdonSyncMode.None)] public float[] BogieOnRailPosition;//偶数：前 奇数：後


        //private bool Started = false;

        //[System.NonSerialized] public bool nowSynced = true;
        //private bool applyFlag = false;
        //private bool updatedFlag = false;

        public void Start()
        {
            if (Trains == null)
            {
                CountTrainOnChild(transform);
                Trains = new Train[trainsNum];
                BogieRailID = new int[trainsNum * 2];
                BogieOnRailPosition = new float[trainsNum * 2];
                id = 0;
                PickTrainOnChild(transform);
            }
            else
            {
                foreach (Train train in Trains)
                {
                    train.Start();
                }
            }

            //foreach (Train train in Trains)
            //{
            //    if(train.transform.parent != null) Debug.Log(train.transform.parent.name);
            //}

            //Started = true;
            //nowSynced = false;
            //if (!Networking.IsOwner(gameObject))
            //{
            //    SendCustomEventDelayedSeconds("ReSyncRequest", 10);
            //}

        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if(player == Networking.LocalPlayer)
            {
                SendCustomEventDelayedSeconds(nameof(TrainManager.ReSyncRequest), 10);
            }
        }

        //public override void OnPreSerialization()
        //{
        //    Debug.Log("Update");
        //    foreach (Train train in Trains)
        //    {
        //        BogieRailID[train.TrainID * 2] = train.BogieRail_F.RailID;
        //        BogieRailID[train.TrainID * 2 + 1] = train.BogieRail_B.RailID;
        //
        //        BogieOnRailPosition[train.TrainID * 2] = train.onRailPoint_F;
        //        BogieOnRailPosition[train.TrainID * 2 + 1] = train.onRailPoint_B;
        //    }
        //    SendCustomNetworkEvent(NetworkEventTarget.All, "DataUpdated");
        //    Debug.Log("Send");
        //}
        //public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        //{
        //    Debug.Log("OutDated");
        //}
        //public override void OnDeserialization()
        //{
        //    //受信する。初回はTrainを初期位置へ移動し始動する。
        //
        //
        //    Debug.Log("Recieve");
        //    if (!Started)
        //    {
        //        Start();
        //    }
        //
        //    applyFlag = true;
        //}
        //public void DataUpdated()
        //{
        //    updatedFlag = true;
        //}
        //void FixedUpdate()
        //{
        //    if (nowSynced) updatedFlag = false;
        //    if (!applyFlag || !updatedFlag || nowSynced) return;
        //    Debug.Log("Apply");
        //    foreach (Train train in Trains)
        //    {
        //        if (!Networking.IsOwner(train.gameObject))
        //        {
        //            //Debug.Log(train.transform.parent.name);
        //            train.BogieRail_F = railsManager.Rails[BogieRailID[train.TrainID * 2]];
        //            train.BogieRail_B = railsManager.Rails[BogieRailID[train.TrainID * 2 + 1]];
        //            train.copyRailProperties_F();
        //            train.copyRailProperties_B();
        //    
        //            train.onRailPoint_F = BogieOnRailPosition[train.TrainID * 2];
        //            train.onRailPoint_B = BogieOnRailPosition[train.TrainID * 2 + 1];
        //    
        //    
        //            train.setPositionFromBogie();
        //            train.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        //        }
        //    }
        //    applyFlag = false;
        //    updatedFlag = false;
        //    nowSynced = true;
        //}

        public void ReSyncRequest()
        {
            foreach (Train train in Trains)
            {
                if (!Networking.IsOwner(train.gameObject))
                {
                    //Debug.Log("FirstSync");
                    train.InitsyncRecieveMode = true;
                    train.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(train.resync));
                }
            }
            //RequestSerialization();
        }


        [RecursiveMethod]
        public void CountTrainOnChild(Transform currentTransform)
        {
            foreach (Transform child in currentTransform)
            {
                if (child.gameObject.GetComponent<Train>() != null)
                {
                    trainsNum++;
                }
                CountTrainOnChild(child);
            }
        }
        [RecursiveMethod]
        public void PickTrainOnChild(Transform currentTransform)
        {
            foreach (Transform child in currentTransform)
            {
                if (child.gameObject.GetComponent<Train>() != null)
                {
                    Debug.Log(child.transform.name);
                    Trains[id] = child.gameObject.GetComponent<Train>();
                    Trains[id].trainManager = this;
                    Trains[id].railsManager = railsManager;
                    Trains[id].Start();
                    child.gameObject.GetComponent<Train>().TrainID = id;
                    id++;
                }
                PickTrainOnChild(child);
            }
        }
    }
}
