using frou01.RigidBodyTrain;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RailManager_onEditor : IProcessSceneWithReport
{
    public int callbackOrder => 0;


    public void OnProcessScene(Scene scene, BuildReport report)
    {
        RailsManager railsManager = null;
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            //Debug.Log(obj.transform.name);
            railsManager = obj.GetComponent<RailsManager>();
            if (railsManager != null) break;
        }
        if (railsManager == null) return;
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (railsManager != null)
            {
                if (obj.GetComponent<Rail_Script>() != null)
                {
                    railsManager.railsNum++;
                }
                railsManager.CountRailOnChild(obj.transform);
            }
        }
        railsManager.Rails = new Rail_Script[railsManager.railsNum];
        railsManager.id = 0;
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if (railsManager != null)
            {
                if (obj.GetComponent<Rail_Script>() != null)
                {
                    railsManager.Rails[railsManager.id] = obj.GetComponent<Rail_Script>();
                    obj.GetComponent<Rail_Script>().RailID = railsManager.id;
                    railsManager.id++;
                }
                railsManager.SetRailOnChild(obj.transform);
            }
        }
        foreach (Rail_Script rail in railsManager.Rails)
        {
            //Debug.Log(rail.transform.parent.name);
        }
    }
}
