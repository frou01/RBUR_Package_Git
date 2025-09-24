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


    TrainManager trainManager = null;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        trainManager = null;
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
                    trainsNum++;
                }
                CountTrainOnChild(obj.transform);
            }
        }
        trainManager.Trains = new Train[trainsNum];
        id = 0;
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (trainManager != null)
            {
                if (obj.GetComponent<Train>() != null)
                {
                    trainManager.Trains[id] = obj.GetComponent<Train>();
                    trainManager.Trains[id].trainManager = trainManager;
                    trainManager.Trains[id].railsManager = trainManager.railsManager;
                    trainManager.Trains[id].InitsyncRecieveMode = true;
                    //trainManager.Trains[trainManager.id].Start();
                    obj.GetComponent<Train>().TrainID = id;
                    id++;
                }
                PickTrainOnChild(obj.transform);
            }
        }

        //foreach (Train train in trainManager.Trains)
        //{
        //    Debug.Log(train.transform.parent.name);
        //}
    }
    public int trainsNum = 0;
    public int id;
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
    public void PickTrainOnChild(Transform currentTransform)
    {
        foreach (Transform child in currentTransform)
        {
            if (child.gameObject.GetComponent<Train>() != null)
            {
                Debug.Log(child.transform.name);
                trainManager.Trains[id] = child.gameObject.GetComponent<Train>();
                trainManager.Trains[id].trainManager = trainManager;
                trainManager.Trains[id].railsManager = trainManager.railsManager;
                trainManager.Trains[id].Start();
                child.gameObject.GetComponent<Train>().TrainID = id;
                id++;
            }
            PickTrainOnChild(child);
        }
    }
}
