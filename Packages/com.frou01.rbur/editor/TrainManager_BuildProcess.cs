using frou01.RigidBodyTrain;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace frou01.RBUR.editor
{
    public class TrainManager_BuildProcess : IProcessSceneWithReport
    {
        public const int callOrderOffset = +1;
        public int callbackOrder => Coupler_BuildProcess.callOrder + callOrderOffset;//Process After Coupler


        public void OnProcessScene(Scene scene, BuildReport report)
        {
            TrainManager trainManager = null;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                trainManager = obj.GetComponent<TrainManager>();
                if (trainManager != null) break;
            }
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                RailsManager railsManager = obj.GetComponent<RailsManager>();
                if (railsManager != null)
                {
                    trainManager.railsManager = railsManager;
                    break;
                }
            }
            if (trainManager == null) return;

            List<Train> Trains_List = new List<Train>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                Trains_List.AddRange(obj.GetComponentsInChildren<Train>(true));
            }

            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                foreach (AbstractBrake brake in obj.GetComponentsInChildren<AbstractBrake>(true))
                {
                    if (!brake.connectionTags.Contains("Brake"))
                    {
                        //Debug.Log("setting brake tag " + brake.name);
                        brake.connectionTags = brake.connectionTags.Append("Brake").ToArray();
                    }
                }
            }

            int id = 0;
            foreach (Train train in Trains_List)
            {
                //Setup Reference
                train.trainManager = trainManager;
                train.railsManager = trainManager.railsManager;
                train.InitsyncRecieveMode = true;
                train.TrainID = id;
                List<GameObject> trainSubObjects = train.subObjects.ToList();

                foreach (TrainConnectionReciever connectionReciever in train.GetComponentsInChildren<TrainConnectionReciever>(true))
                {
                    if (!train.connectionRecievers.Contains(connectionReciever))
                    {
                        train.connectionRecievers = train.connectionRecievers.AddItem(connectionReciever).ToArray();
                    }
                    if (trainSubObjects.Contains(connectionReciever.gameObject)) trainSubObjects.Add(connectionReciever.gameObject);
                }

                foreach (AbstractBrake brakeModule in train.GetComponentsInChildren<AbstractBrake>(true))
                {
                    brakeModule.SetUpOnBuildProcess(train);
                }

                foreach (BrakeConnectorValve brakeConnectorValve in train.GetComponentsInChildren<BrakeConnectorValve>(true))
                {
                    brakeConnectorValve.SetUpOnBuildProcess(train);
                }


                id++;
            }

            id = 0;
            foreach (Train train in Trains_List)
            {
                //Setup Connection
                setUpConnectedCoupler(train, train.CouplerF, train.connectedTrain_F);
                setUpConnectedCoupler(train, train.CouplerB, train.connectedTrain_B);
                id++;
            }
            trainManager.Trains = Trains_List.ToArray();
            //foreach (Train train in trainManager.Trains)
            //{
            //    Debug.Log(train.transform.parent.name);
            //}

        }

        static void setUpConnectedCoupler(Train ConnectingTrain, CouplerObj ConnectingCoupler, Train connectedTrain)
        {
            if (connectedTrain == null)
            {
                ConnectingCoupler.setConnectedCoupler(null);
                return;
            }

            if (Vector3.Dot(connectedTrain.transform.forward, ConnectingTrain.transform.position - connectedTrain.transform.position) > 0)
            {
                connectedTrain.connectedTrain_F = ConnectingTrain;
                connectedTrain.CouplerF.setConnectedCoupler(ConnectingCoupler);

                ConnectingCoupler.setConnectedCoupler(connectedTrain.CouplerF);
            }
            else
            {
                connectedTrain.connectedTrain_B = ConnectingTrain;
                connectedTrain.CouplerB.setConnectedCoupler(ConnectingCoupler);

                ConnectingCoupler.setConnectedCoupler(connectedTrain.CouplerB);
            }
        }
    }
}