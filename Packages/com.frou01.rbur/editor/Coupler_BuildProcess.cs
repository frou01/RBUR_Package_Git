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
            List<CouplerObj> Trains_List = new List<CouplerObj>();
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                Trains_List.AddRange(obj.GetComponentsInChildren<CouplerObj>(true));//collect all couplers
            }
            foreach (CouplerObj coupler in Trains_List)
            {
                coupler.onBuildProcess = true;
                coupler.Initialize();//preRuntime initialize
            }
        }
    }
}