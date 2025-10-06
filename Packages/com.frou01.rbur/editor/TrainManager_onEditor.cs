using frou01.RigidBodyTrain;
using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainManager_onEditor : IProcessSceneWithReport
{
    public int callbackOrder => 0;


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

        int id = 0;
        foreach (Train train in Trains_List)
        {
            train.trainManager = trainManager;
            train.railsManager = trainManager.railsManager;
            train.InitsyncRecieveMode = true;
            train.TrainID = id;
            id++;
        }
        trainManager.Trains = Trains_List.ToArray();

        //foreach (Train train in trainManager.Trains)
        //{
        //    Debug.Log(train.transform.parent.name);
        //}
    }
}
