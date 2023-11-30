using frou01.RigidBodyTrain;
using System.Collections;
using System.Collections.Generic;
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
            if(trainManager != null) break;
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
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (trainManager != null)
            {
                if (obj.GetComponent<Train>() != null)
                {
                    trainManager.trainsNum++;
                }
                trainManager.CountTrainOnChild(obj.transform);
            }
        }
        trainManager.Trains = new Train[trainManager.trainsNum];
        trainManager.BogieRailID = new int[trainManager.trainsNum * 2];
        trainManager.BogieOnRailPosition = new float[trainManager.trainsNum * 2];
        trainManager.id = 0;
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (trainManager != null)
            {
                if (obj.GetComponent<Train>() != null)
                {
                    trainManager.Trains[trainManager.id] = obj.GetComponent<Train>();
                    trainManager.Trains[trainManager.id].trainManager = trainManager;
                    trainManager.Trains[trainManager.id].railsManager = trainManager.railsManager;
                    trainManager.Trains[trainManager.id].InitsyncRecieveMode = true;
                    //trainManager.Trains[trainManager.id].Start();
                    obj.GetComponent<Train>().TrainID = trainManager.id;
                    trainManager.id++;
                }
                trainManager.PickTrainOnChild(obj.transform);
            }
        }

        foreach (Train train in trainManager.Trains)
        {
            //Debug.Log(train.transform.parent.name);
        }
    }
}
