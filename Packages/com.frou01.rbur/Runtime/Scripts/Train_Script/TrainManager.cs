
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace frou01.RigidBodyTrain
{
    public class TrainManager : UdonSharpBehaviour
    {
        public Train[] Trains;

        public int pathRes;

        public RailsManager railsManager;



        //private bool Started = false;

        //[System.NonSerialized] public bool nowSynced = true;
        //private bool applyFlag = false;
        //private bool updatedFlag = false;

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
                DateTime nowtime = Networking.GetNetworkDateTime();
                if (!Networking.IsOwner(train.gameObject))
                {
                    //Debug.Log("FirstSync");
                    Debug.Log("Time FromSync : " + (nowtime - train.LastSent_Resync).TotalSeconds);
                    if ((nowtime - train.LastSent_Resync).TotalSeconds > 10)
                    {
                        train.LastSent_Resync = nowtime;
                        train.InitsyncRecieveMode = true;
                        train.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(train.resync));
                    }
                }
            }
            //RequestSerialization();
        }
    }
}
