using Cinemachine;
using frou01.RigidBodyTrain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Cinemachine.CinemachinePathBase;
using static UnityEngine.ParticleSystem;

public class RailManager_onEditor : IProcessSceneWithReport
{
    public int callbackOrder => 0;


    BlockingCollection<GeneratedColliderData> colliderDatas;
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

        //LayerMask.NameToLayerでレイヤーを取得しておく
        //Name = RBUR_RailAndWheel;
        int ColliderLayer = LayerMask.NameToLayer("RBUR_RailAndWheel");
        if (ColliderLayer == -1)
        {
            ColliderLayer = 0;//見つからなければDefault
        }
        colliderDatas = new BlockingCollection<GeneratedColliderData>();
        //全レールにコライダーを設置する
        List<Task> Tasks = new List<Task>();//終了待機用タスク（走り切る前に他に移られちゃ困る）
        foreach (Rail_Script rail in railsManager.Rails)
        {
            Debug.Log("ColliderGeneration" + rail.name);
            //直列でやるの現実的じゃないのでスレッドを立てる

            float pathLength = rail.cinemachinePath.PathLength;
            GameObject targetObject = rail.gameObject;
            Task generatorTask = new Task(() => { genRailCollider(rail.cinemachinePath, targetObject, pathLength, railsManager, ColliderLayer); });
            generatorTask.Start();
            Tasks.Add(generatorTask);
        }
        foreach (Task generatorTask in Tasks)
        {
            generatorTask.Wait();//終了を待機する
        }
        foreach (GeneratedColliderData ColliderElements in colliderDatas)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = ColliderElements.generatedVertices;
            mesh.triangles = ColliderElements.generatedPolygon;
            mesh.RecalculateNormals();

            GameObject colliderObject = new GameObject(ColliderElements.parentObject.name + " collider " + ColliderElements.DivisionID);
            colliderObject.transform.SetParent(ColliderElements.parentObject.transform, false);
            colliderObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            colliderObject.layer = ColliderLayer;
        }
    }

    struct GeneratedColliderData
    {
        public GameObject parentObject;
        public Vector3[] generatedVertices;
        public int[] generatedPolygon;
        public int DivisionID;
    }

    private void genRailCollider(CinemachinePathBase RailCinemachinePath, GameObject TargetObject,float pathLength, RailsManager railsManager,int ColliderLayer)
    {
        Debug.Log("Generate new Rails Collider task");
        //最初にCinemachineをrailColliderMaxLengthに収まるよう分割する数を設定
        int ColliderDivisionNum = 1 + (int)(pathLength / railsManager.railColliderMaxLength);
        Debug.Log("ColliderDivisionNum " + ColliderDivisionNum);
        //CinemachinePathをさらに内部で細分化し、MeshColliderとして踏面を生成する。
        //Edge = 0は別で計算
        //参考 https://qiita.com/notargs/items/64a3de46c48e3cff176a

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        float Edge_dist;
        float t;
        Vector3 PositionAtT;
        Quaternion OrientationAtT;
        Vector3 newPosX;
        Vector3 newNegX;

        for (float currentColliderDivision = 0; currentColliderDivision < ColliderDivisionNum; currentColliderDivision++)
        {

            Edge_dist =
                Mathf.LerpUnclamped(
                    pathLength * (currentColliderDivision / ColliderDivisionNum),
                    pathLength * ((currentColliderDivision + 1) / ColliderDivisionNum),
                    0 / railsManager.railFaceMaxDivide);
            t = RailCinemachinePath.ToNativePathUnits(Edge_dist, PositionUnits.Distance);
            PositionAtT = RailCinemachinePath.EvaluateLocalPosition(t);
            OrientationAtT = RailCinemachinePath.EvaluateLocalOrientation(t).normalized;
            newPosX = PositionAtT + (OrientationAtT * Vector3.right);//Local
            newNegX = PositionAtT - (OrientationAtT * Vector3.right);
            vertices.Add(newPosX);
            vertices.Add(newNegX);


            for (float currentEdge = 0; currentEdge < railsManager.railFaceMaxDivide; currentEdge++)
            {
                Edge_dist =
                    Mathf.LerpUnclamped(
                        pathLength * (currentColliderDivision / ColliderDivisionNum),
                        pathLength * ((currentColliderDivision + 1) / ColliderDivisionNum),
                        (currentEdge + 1) / railsManager.railFaceMaxDivide);
                t = RailCinemachinePath.ToNativePathUnits(Edge_dist, PositionUnits.Distance);
                PositionAtT = RailCinemachinePath.EvaluateLocalPosition(t);
                OrientationAtT = RailCinemachinePath.EvaluateLocalOrientation(t).normalized;

                newPosX = PositionAtT + (OrientationAtT * Vector3.right);//Local
                newNegX = PositionAtT - (OrientationAtT * Vector3.right);
                vertices.Add(newPosX);
                vertices.Add(newNegX);

                triangles.Add((int)currentEdge * 2);
                triangles.Add((int)currentEdge * 2 + 1);
                triangles.Add((int)currentEdge * 2 + 3);
                triangles.Add((int)currentEdge * 2 + 3);
                triangles.Add((int)currentEdge * 2 + 2);
                triangles.Add((int)currentEdge * 2 + 0);
                Debug.Log(Edge_dist);
            }

            GeneratedColliderData newColliderData = new GeneratedColliderData();
            newColliderData.parentObject = TargetObject;
            newColliderData.generatedVertices = vertices.ToArray();
            newColliderData.generatedPolygon = triangles.ToArray();
            vertices.Clear();
            triangles.Clear();
            newColliderData.DivisionID = (int)currentColliderDivision;
            colliderDatas.Add(newColliderData);
            Debug.Log("Generated an collider division");
        }
    }
}
