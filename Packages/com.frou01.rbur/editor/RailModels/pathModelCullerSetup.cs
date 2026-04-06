
#if (UNITY_EDITOR)
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEngine;
using System.Linq;
using frou01.util;

public class pathModelCullerSetup : MonoBehaviour
{
    public Cinemachine.CinemachinePathBase cinemachinePath;
    [SerializeField] GameObject[] objects;
    public float oneCullerLength;
    public float cullerSize;
    public Transform rootTransform;

    public void perform()
    {
        List<GameObject> objectsList = new List<GameObject>();
        objectsList.AddRange(objects);
        List<GameObject[]> ClusteredGoList = new List<GameObject[]>();

        float pathLength = cinemachinePath.PathLength;
        Vector3[] cullerCenters = new Vector3[(int)(pathLength / oneCullerLength) + 1];
        int currentSegment = 0;
        for (float current = 0;current <= pathLength; current += oneCullerLength)
        {
            List<GameObject> oneClusterList = new List<GameObject>();
            Vector3 currentPathPos = cinemachinePath.EvaluatePositionAtUnit(current, Cinemachine.CinemachinePathBase.PositionUnits.Distance);
            foreach (GameObject go in objectsList)
            {
                if(Vector3.Distance(go.transform.position, currentPathPos) < oneCullerLength)
                {
                    Debug.Log(go.name);
                    oneClusterList.Add(go);
                }
            }
            objectsList = objectsList.Except(oneClusterList).ToList();
            ClusteredGoList.Add(oneClusterList.ToArray());
            cullerCenters[currentSegment] = cinemachinePath.EvaluatePositionAtUnit(current + oneCullerLength/2, Cinemachine.CinemachinePathBase.PositionUnits.Distance);
            currentSegment += 1;
        }
        SetUpColliderBaseCuller(ClusteredGoList, cullerCenters, rootTransform, true, false);
    }
    public static void SetUpColliderBaseCuller(List<GameObject[]> ClusteredGoList,Vector3[] cullerCenters, Transform root, bool changeRoot, bool isStatic)
    {
        GameObject[][] ClusteredGo = ClusteredGoList.ToArray();
        int clusterNum = ClusteredGo.Length;

        for (int i = 0; i < clusterNum; i++)
        {
            GameObject go = new GameObject();
            go.name = "cullCollider" + i;

            go.transform.position = cullerCenters[i];
            go.transform.parent = root;
            ColliderGameObjectCuller ClRC = go.AddUdonSharpComponent<ColliderGameObjectCuller>();
            ClRC.objects = ClusteredGo[i];
            ClRC.isStaticMode = isStatic;

            if (changeRoot) foreach (GameObject go2 in ClRC.objects)
                {
                    go2.transform.parent = go.transform;
                }
            SphereCollider sphereCollider = go.AddComponent<SphereCollider>();
            sphereCollider.radius = 1500;
            sphereCollider.isTrigger = true;
        }
    }
}

#endif