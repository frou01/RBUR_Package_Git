using frou01.RigidBodyTrain;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace frou01.RBUR.editor
{
    public class Coupler_BuildProcess : IProcessSceneWithReport
    {
        public const int callOrder = -20;
        public int callbackOrder => callOrder;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            List<CouplerObj> Coupler_List = new List<CouplerObj>();
            TrainManager trainManager = null;
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                Coupler_List.AddRange(obj.GetComponentsInChildren<CouplerObj>(true));//collect all couplers
                if (obj.GetComponentInChildren<TrainManager>(true))
                {
                    trainManager = obj.GetComponent<TrainManager>();
                }
            }
            foreach (CouplerObj coupler in Coupler_List)
            {
                coupler.Initialize(trainManager);//preRuntime initialize
            }
        }
    }
}