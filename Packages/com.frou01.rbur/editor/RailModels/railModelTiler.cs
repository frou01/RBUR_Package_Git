
#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using static Cinemachine.CinemachinePathBase;
using System.Threading.Tasks;
using Cinemachine;
using static Cinemachine.CinemachineSmoothPath;
using UnityEditor.Formats.Fbx.Exporter;
using System;
using System.Reflection;
using UdonSharpEditor;

[ExecuteAlways]
public class railModelTiler : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    public Cinemachine.CinemachinePathBase cinemachinePath;
    [SerializeField] GameObject meshrendererObjectPrefab;
    [SerializeField] float modelLength;
    [SerializeField] float TilingStart;
    [SerializeField] float TilingEnd;

    [SerializeField] bool isZinverted;
    [SerializeField] bool ignoreRoll;
    [SerializeField] bool ignorePitch;
    [SerializeField] bool UseColliderBaseCuller;
    [SerializeField] float disbaleInstancedThreshold = 0.001f;

    [SerializeField] public string saveFolder;

    [SerializeField] int veticesTransformSteps = 20;
    float generatingDistance;
    Mesh instancedMesh;
    GameObject copied;
    List<GameObject> copies = new List<GameObject>();
    List<GameObject> gened = new List<GameObject>();
    int VerticesId;


    Vector3[] originVertices;

    Vector3[] transformedVertices;
    bool setUp;
    bool transforming;
    public bool started;
    bool ReplaceMesh;


    void Start()
    {

    }

    int gameObjID = 0;

    public void selectFolder()
    {
        saveFolder = EditorUtility.OpenFolderPanel("Save Folder", Application.dataPath, string.Empty);
        saveFolder = saveFolder.Remove(0, Application.dataPath.Length-6);
    }
    public void startTiling()
    {
        generatingDistance = TilingStart;
        started = true;
        setUp = true;
        ReplaceMesh = false;

        if (TilingEnd == 0)
        {
            TilingEnd = cinemachinePath.PathLength;
        }

        gameObjID = 0;
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            cancelTiling();
            selectFolder();
        }
        copies.Clear();
        gened.Clear();
    }
    public void startTilingAll()
    {
        TilingStart = 0;
        generatingDistance = TilingStart;
        TilingEnd = cinemachinePath.PathLength;
        started = true;
        setUp = true;
        ReplaceMesh = false;

        gameObjID = 0;
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            cancelTiling();
            selectFolder();
        }
        copies.Clear();
        gened.Clear();
    }
    public void cancelTiling()
    {
        generatingDistance = TilingStart;
        started = false;
        setUp = false;
        transforming = false;
        EditorUtility.ClearProgressBar();
    }

    public void setUpNewObject()
    {
        float generatingDistance = this.generatingDistance + (isZinverted ? +modelLength : 0);
        copied = (GameObject)PrefabUtility.InstantiatePrefab(meshrendererObjectPrefab);
        copied.transform.SetParent(this.transform);
        float t = cinemachinePath.ToNativePathUnits(generatingDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
        copied.transform.position = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits);
        if (generatingDistance > cinemachinePath.PathLength)
        {
            copied.transform.position += cinemachinePath.EvaluateTangentAtUnit(t, PositionUnits.PathUnits).normalized * (generatingDistance - cinemachinePath.PathLength);
        }
        Quaternion rotation;
        getRotationOnT(t, out rotation, Quaternion.identity);
        copied.transform.rotation = rotation;
        copied.transform.position = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + copied.transform.rotation * this.offset;
        instancedMesh = Instantiate(copied.GetComponent<MeshFilter>().sharedMesh);
        VerticesId = 0;
        originVertices = instancedMesh.vertices;
        transformedVertices = new Vector3[instancedMesh.vertices.Length];
        setUp = false;
        transforming = true;
    }

    public void transformVetices()
    {
        for(int stepsCnt = 0; stepsCnt < veticesTransformSteps; stepsCnt++)
        {
            if (VerticesId < originVertices.Length)
            {
                float generatingDistance = this.generatingDistance + (isZinverted ? +modelLength : 0) + originVertices[VerticesId].z;
                float t = cinemachinePath.ToNativePathUnits(generatingDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
                Vector3 originPos = new Vector3(0, 0, originVertices[VerticesId].z);
                Vector3 offset = copied.transform.InverseTransformPoint(cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits)) - originPos;
                if (generatingDistance > cinemachinePath.PathLength)
                {
                    ReplaceMesh = true;
                    offset = copied.transform.InverseTransformPoint(cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateTangentAtUnit(t, PositionUnits.PathUnits).normalized * (generatingDistance - cinemachinePath.PathLength)) - originPos;
                }
                if (generatingDistance > TilingEnd)
                {
                    ReplaceMesh = true;
                }
                if (!ReplaceMesh && offset.sqrMagnitude > disbaleInstancedThreshold) ReplaceMesh = true;
                Quaternion rotation;
                getRotationOnT(t, out rotation, Quaternion.Inverse(copied.transform.rotation));
                transformedVertices[VerticesId] = originPos + rotation * this.offset +  rotation * (originVertices[VerticesId] - originPos) + offset;
                VerticesId++;
            }
            else
            {
                transforming = false;
            }
        }
        EditorUtility.DisplayProgressBar("RailModelTiler", "Vetices Transforming...", (generatingDistance + modelLength * VerticesId / originVertices.Length) / TilingEnd);
    }

    public void getRotationOnT(float t, out Quaternion rotation, Quaternion Rotationoffset)
    {
        if (ignoreRoll || ignorePitch)
        {
            rotation = (cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits)).normalized;
            Vector3 EulerRailRotation = rotation.eulerAngles;
            if (ignoreRoll) EulerRailRotation.z = 0;
            else
            {
            }
            if (ignorePitch) EulerRailRotation.x = 0;
            rotation.eulerAngles = EulerRailRotation;
            rotation = Rotationoffset * rotation;
        }
        else
        {
            rotation = Rotationoffset * (cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits)).normalized;
        }
    }

    int FBXID = 0;
    public void saveObject()
    {
        if (ReplaceMesh)
        {
            instancedMesh.SetVertices(transformedVertices);
            if (generatingDistance + (isZinverted ? +modelLength : modelLength) > TilingEnd)
            {
                for (int subMeshID = 0; subMeshID < instancedMesh.subMeshCount; subMeshID++)
                {
                    Debug.Log("Cutting" + subMeshID);
                    string progressName = "Cuting Poly... material " + subMeshID;
                    int CheckedPoliesCnt = 0;
                    int[] OriginPolies = instancedMesh.GetTriangles(subMeshID);
                    int[] CheckedPolies = new int[OriginPolies.Length];
                    for (int PoliesId = 0; PoliesId < OriginPolies.Length; PoliesId += 3)
                    {
                        bool check = false;
                        for (int triangleID = 0; triangleID < 3; triangleID++)
                        {
                            if ((generatingDistance + (isZinverted ? +modelLength : 0) + originVertices[OriginPolies[PoliesId + triangleID]].z) < TilingEnd)
                            {
                                check |= true;
                            }

                        }

                        if (check)
                        {
                            for (int triangleID = 0; triangleID < 3; triangleID++)
                            {
                                CheckedPolies[CheckedPoliesCnt + triangleID] = OriginPolies[PoliesId + triangleID];
                            }
                            CheckedPoliesCnt += 3;
                        }
                        EditorUtility.DisplayProgressBar("RailModelTiler", progressName, (float)PoliesId / OriginPolies.Length);
                    }
                    CheckedPolies = CheckedPolies.Take(CheckedPoliesCnt).ToArray();
                    instancedMesh.SetTriangles(CheckedPolies, subMeshID, true);
                }
            }
            instancedMesh.RecalculateBounds();
            instancedMesh.RecalculateNormals();
            instancedMesh.RecalculateTangents();
            instancedMesh.name = copied.name;
            //string meshAssetPath = Path.Combine(saveFolder, instancedMesh.name + gameObjID + ".asset");
            //while (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(meshAssetPath)))
            //{
            //    gameObjID++;
            //    copied.name = meshrendererObjectPrefab.name + gameObjID;
            //    meshAssetPath = Path.Combine(saveFolder, instancedMesh.name + gameObjID + ".asset");
            //}
            //
            //instancedMesh.MarkModified();
            //AssetDatabase.CreateAsset(instancedMesh, meshAssetPath);
            //AssetDatabase.ImportAsset(meshAssetPath, ImportAssetOptions.DontDownloadFromCacheServer);
            //copied.GetComponent<MeshFilter>().mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            //copied.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            copied.GetComponent<MeshFilter>().sharedMesh = instancedMesh;
            gameObjID++;
            copied.name += gameObjID;
            ReplaceMesh = false;

            copies.Add(copied);
        }
        else
        {
            float t = cinemachinePath.ToNativePathUnits(generatingDistance + (isZinverted ? 0 : +modelLength), PositionUnits.Distance);
            Quaternion rotation;
            getRotationOnT(t, out rotation, Quaternion.identity);
            if (isZinverted)
            {
                copied.transform.forward = copied.transform.position - (cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + rotation * this.offset);
            }
            else
            {
                copied.transform.forward = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + rotation * this.offset - copied.transform.position;
            }
            if (isZinverted)
            {
                Vector3 fitScale = copied.transform.localScale;
                fitScale.z = (copied.transform.position - (cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + rotation * this.offset)).magnitude /modelLength;
                copied.transform.localScale = fitScale;
            }
            else
            {
                Vector3 fitScale = copied.transform.localScale;
                fitScale.z = (cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + rotation * this.offset - copied.transform.position).magnitude / modelLength;
                copied.transform.localScale = fitScale;
            }
            copied.name += "instanced";
        }
        gened.Add(copied);
        setUp = true;
        transFormChildObject(copied.transform);

        if (root == null)
        {
            copied.transform.parent = cinemachinePath.transform;
        }
        else
        {
            copied.transform.parent = root;
        }
        this.generatingDistance += modelLength;


        if (this.generatingDistance >= TilingEnd)
        {
            if(UseColliderBaseCuller) SetUpColliderBaseCuller(modelLength,gened, cinemachinePath.transform,true,true);

            if (copies.Count > 0)
            {
                FBXID = 0;
                string filePath = Path.Combine(saveFolder, meshrendererObjectPrefab.name + FBXID + "Compressing.fbx");
                while (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filePath)))
                {
                    FBXID++;
                    filePath = Path.Combine(saveFolder, meshrendererObjectPrefab.name + FBXID + "Compressing.fbx");
                }

                GameObject[] exporter = new GameObject[copies.Count];

                for (int index = 0; index < exporter.Length; index++)
                {
                    exporter[index] = Instantiate(copies[index]);
                    exporter[index].name = copies[index].name;
                }
                ExportsBinaryFBX(filePath, exporter);
                //ModelExporter.ExportObjects(filePath, exporter);

                for (int index = 0; index < exporter.Length; index++)
                {
                    DestroyImmediate(exporter[index]);
                }
            }
        }
    }
    public static void SetUpColliderBaseCuller(float genDist,List<GameObject> gened,Transform root,bool changeRoot,bool isStatic)
    {
        int clusterSegmentLength = (int)(500 / genDist);
        int clusterNum = gened.Count / clusterSegmentLength;
        if (clusterNum > 0 && gened.Count % clusterSegmentLength != 0) clusterNum += 1;
        if (clusterNum > 0)
        {
            GameObject[][] ClusteredGo = new GameObject[clusterNum][];
            for (int i = 0; i < clusterNum; i++)
            {
                if (i >= gened.Count / clusterSegmentLength)
                {
                    ClusteredGo[i] = new GameObject[gened.Count % clusterSegmentLength];
                }
                else
                {
                    ClusteredGo[i] = new GameObject[clusterSegmentLength];
                }
            }
            for (int i = 0; i < gened.Count; i++)
            {
                if (i / clusterSegmentLength < clusterNum)
                {
                    ClusteredGo[i / clusterSegmentLength][i % clusterSegmentLength] = gened[i];
                    StaticEditorFlags staticMode = GameObjectUtility.GetStaticEditorFlags(gened[i]);
                    staticMode = staticMode & ~StaticEditorFlags.BatchingStatic;
                    GameObjectUtility.SetStaticEditorFlags(gened[i], staticMode);
                }
            }

            for (int i = 0; i < clusterNum; i++)
            {
                GameObject go = new GameObject();
                go.name = "cullCollider" + i;
                if (i >= gened.Count / clusterSegmentLength)
                {
                    go.transform.position = ClusteredGo[i][gened.Count % clusterSegmentLength / 2].transform.position;
                }
                else
                {
                    go.transform.position = ClusteredGo[i][clusterSegmentLength / 2].transform.position;
                }
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
    private static void ExportsBinaryFBX(string filePath, UnityEngine.Object[] Objects)
    {
        // Find relevant internal types in Unity.Formats.Fbx.Editor assembly
        Type[] types = AppDomain.CurrentDomain.GetAssemblies().First(x => x.FullName == "Unity.Formats.Fbx.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").GetTypes();
        Type optionsInterfaceType = types.First(x => x.Name == "IExportOptions");
        Type exportDataInterfaceType = types.First(x => x.Name == "IExportData");
        Type optionsType = types.First(x => x.Name == "ExportOptionsSettingsSerializeBase");

        // Instantiate a settings object instance
        MethodInfo optionsProperty = typeof(ModelExporter).GetProperty("DefaultOptions", BindingFlags.Static | BindingFlags.NonPublic).GetGetMethod(true);
        object optionsInstance = optionsProperty.Invoke(null, null);

        // Change the export setting from ASCII to binary
        FieldInfo exportFormatField = optionsType.GetField("exportFormat", BindingFlags.Instance | BindingFlags.NonPublic);
        exportFormatField.SetValue(optionsInstance, 1);

        Type optionsPositionType = types.First(x => x.Name == "ExportModelSettingsSerialize");
        FieldInfo exportPositionField = optionsPositionType.GetField("objectPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        exportPositionField.SetValue(optionsInstance, 1);

        Type openedType = typeof(Dictionary<,>);


        Type closedType =
            openedType.MakeGenericType(typeof(GameObject), exportDataInterfaceType);

        // Invoke the ExportObject method with the settings param
        MethodInfo exportObjectMethod = typeof(ModelExporter).GetMethod("ExportObjects", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(string), typeof(UnityEngine.Object[]), optionsInterfaceType , closedType }, null);
        exportObjectMethod.Invoke(null, new object[] { filePath, Objects, optionsInstance,null });
    }

    public void transFormChildObject(Transform currentTransform)
    {
        foreach (Transform child in currentTransform)
        {
            Vector3 localPos = copied.transform.InverseTransformPoint(child.position);
            float generatingDistance = this.generatingDistance + (isZinverted ? +modelLength : 0) + localPos.z;
            float t = cinemachinePath.ToNativePathUnits(generatingDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
            Vector3 originPos = new Vector3(0, 0, localPos.z);
            Vector3 offset = copied.transform.InverseTransformPoint(cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits)) - originPos;
            if (generatingDistance > cinemachinePath.PathLength)
            {
                offset = copied.transform.InverseTransformPoint(cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateTangentAtUnit(t, PositionUnits.PathUnits).normalized * (generatingDistance - cinemachinePath.PathLength)) - originPos;
            }
            Quaternion childLocatedRotation = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits);

            if (ignoreRoll || ignorePitch)
            {
                Vector3 eulered = childLocatedRotation.eulerAngles;
                if (ignoreRoll) eulered.z = 0;
                if (ignorePitch) eulered.x = 0; ;
                childLocatedRotation = Quaternion.Euler(eulered.x, eulered.y, eulered.z);
            }
            Quaternion inversedCopiedRotation = Quaternion.Inverse(copied.transform.rotation);
            Quaternion rotation = inversedCopiedRotation * childLocatedRotation;

            child.position = copied.transform.TransformPoint(originPos + rotation * (localPos - originPos) + offset + rotation * this.offset);
            child.rotation = childLocatedRotation;
            if (PrefabUtility.GetPrefabInstanceHandle(child) != meshrendererObjectPrefab) continue;
            transFormChildObject(child);
        }
    }

    public void Update()
    {
        if (started && generatingDistance < TilingEnd)
        {
            if (setUp) setUpNewObject();
            else
            {
                if (transforming) transformVetices();
                else
                {
                    saveObject();
                }
            }

        }
        else
        {
            if (started)
            {
                started = false;
                if (this.generatingDistance >= TilingEnd)
                {
                    EditorUtility.ClearProgressBar();

                    string filePath = Path.Combine(saveFolder, meshrendererObjectPrefab.name + FBXID + "Compressing.fbx");
                    GameObject loadedFBX = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);

                    if (copies.Count > 1)
                    {
                        int cnt = 0;
                        foreach (GameObject go in copies)
                        {
                            go.GetComponent<MeshFilter>().mesh = loadedFBX.transform.GetChild(cnt).gameObject.GetComponent<MeshFilter>().sharedMesh;
                            cnt++;
                        }
                    }
                    else if(copies.Count == 1)
                    {
                        copies[0].GetComponent<MeshFilter>().mesh = loadedFBX.GetComponent<MeshFilter>().sharedMesh;
                    }
                }
            }
        }
    }
    public void OnDrawGizmos()
    {
        // Your gizmo drawing thing goes here if required...

        // Ensure continuous Update calls.
        if (!Application.isPlaying && started)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
    }
    public void OnDrawGizmosSelected()
    {
        if (cinemachinePath == null) return;
        Gizmos.color = new Color(0f, 0, 1f, 1f);
        float t = cinemachinePath.ToNativePathUnits(TilingStart, PositionUnits.Distance);

        Gizmos.DrawLine(
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (-Vector3.right + this.offset),
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (Vector3.right + this.offset));
        Gizmos.DrawLine(
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (-Vector3.up + this.offset),
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (Vector3.up + this.offset));
        Gizmos.color = new Color(1f, 0, 0f, 1f);
        t = cinemachinePath.ToNativePathUnits(TilingEnd, PositionUnits.Distance);
        Gizmos.DrawLine(
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (-Vector3.right + this.offset),
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (Vector3.right + this.offset));
        Gizmos.DrawLine(
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (-Vector3.up + this.offset),
            cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (Vector3.up + this.offset));

        if (modelLength > 2)
        {
            for (float genDist = TilingStart + modelLength; genDist < TilingEnd;)
            {
                t = cinemachinePath.ToNativePathUnits(genDist, PositionUnits.Distance);
                Gizmos.color = new Color(1f, 1f, 0f, 1f);
                Gizmos.DrawLine(
                    cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (-Vector3.right + this.offset),
                    cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (Vector3.right + this.offset));
                Gizmos.DrawLine(
                    cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (-Vector3.up + this.offset),
                    cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) + cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized * (Vector3.up + this.offset));
                genDist += modelLength;
            }
        }

    }

    public Transform root;
    public Vector3 offset;
    public void moveCinemachine()
    {
        applyMoveToCinemachine(root);
    }
    public void applyMoveToCinemachine(Transform currentTransform)
    {
        Debug.Log("on " + currentTransform.name);
        foreach (Transform child in currentTransform)
        {
            CinemachineSmoothPath childCinemachine;
            if ((childCinemachine = child.GetComponent<CinemachineSmoothPath>()) != null)
            {
                for (int id = 0;id< childCinemachine.m_Waypoints.Length;id ++)
                {
                    Waypoint wp = new CinemachineSmoothPath.Waypoint();
                    wp.position = childCinemachine.m_Waypoints[id].position + offset;
                    childCinemachine.m_Waypoints[id] = wp;
                }
                childCinemachine.EvaluatePosition(0);
            }
            applyMoveToCinemachine(child);
        }
    }

}

#endif