using Cinemachine;
using frou01.RigidBodyTrain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Cinemachine.CinemachinePathBase;

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
            //全Cinemachineを探索、コライダーを設置する
            //最初にCinemachineをrailColliderMaxLengthに収まるよう分割する数を設定
            CinemachinePathBase cinemachinePath = rail.cinemachinePath;
            float pathLength = cinemachinePath.PathLength;
            int ColliderDivisionNum = 1 + (int)(pathLength / railsManager.railColliderMaxLength);
            for (int currentColliderDivision = 0; currentColliderDivision < ColliderDivisionNum; currentColliderDivision++)
            {
                //CinemachinePathをさらに内部で細分化し、MeshColliderとして踏面を生成する。
                //Edge = 0は別で計算
                //参考 https://qiita.com/notargs/items/64a3de46c48e3cff176a
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();

                float Edge_dist = 0;
                float t;
                Vector3 PositionAtT;
                Quaternion OrientationAtT;
                Vector3 newPosX;
                Vector3 newNegX;

                Edge_dist = pathLength * currentColliderDivision / ColliderDivisionNum;
                t = cinemachinePath.ToNativePathUnits(Edge_dist, PositionUnits.Distance);
                PositionAtT = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits);
                OrientationAtT = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized;
                newPosX = PositionAtT + (OrientationAtT * Vector3.right);//Global
                newNegX = PositionAtT - (OrientationAtT * Vector3.right);
                newPosX = rail.transform.InverseTransformPoint(newPosX);//Local
                newNegX = rail.transform.InverseTransformPoint(newNegX);
                vertices.Add(newPosX);
                vertices.Add(newNegX);


                for (int currentEdge = 0; currentEdge < railsManager.railFaceMaxDivide; currentEdge++)
                {
                    Edge_dist = 
                        Mathf.LerpUnclamped(
                            pathLength * currentColliderDivision / ColliderDivisionNum, 
                            pathLength * (currentColliderDivision + 1) / ColliderDivisionNum,
                            (currentEdge + 1)/railsManager.railFaceMaxDivide);
                    t = cinemachinePath.ToNativePathUnits(Edge_dist, PositionUnits.Distance);
                    PositionAtT = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits);
                    OrientationAtT = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized;

                    newPosX = PositionAtT + (OrientationAtT * Vector3.right);//Global
                    newNegX = PositionAtT - (OrientationAtT * Vector3.right);
                    newPosX = rail.transform.InverseTransformPoint(newPosX);//Local
                    newNegX = rail.transform.InverseTransformPoint(newNegX);
                    vertices.Add(newPosX);
                    vertices.Add(newNegX);

                    triangles.Add(currentEdge * 2);
                    triangles.Add(currentEdge * 2 + 1);
                    triangles.Add(currentEdge * 2 + 3);
                    triangles.Add(currentEdge * 2 + 3);
                    triangles.Add(currentEdge * 2 + 2);
                    triangles.Add(currentEdge * 2 + 0);
                }
                //LayerMask.NameToLayerでレイヤーを設定
            }
        }
    }
}
