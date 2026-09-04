
#if (UNITY_EDITOR)
using Cinemachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using UnityEngine.Serialization;
using static Cinemachine.CinemachinePathBase;
using static Cinemachine.CinemachineSmoothPath;

[ExecuteAlways]
public class railModelTiler : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    public Cinemachine.CinemachinePathBase cinemachinePath;
    [SerializeField] GameObject meshrendererObjectPrefab;
    [HideInInspector][SerializeField] public GameObject exportedModel {  get; private set; } = null;
    [SerializeField] float modelLength;
    [SerializeField] float TilingStart;
    [SerializeField] float TilingEnd;

    [SerializeField] bool isZinverted;
    [SerializeField] bool ignoreRoll;
    [SerializeField] bool ignorePitch;
    [SerializeField] bool UseColliderBaseCuller;
    [SerializeField] float disbaleInstancedThreshold = 0.001f;
    [SerializeField] float cutterOffset = -0.001f;

    [SerializeField] public string saveFolder;

    [SerializeField] int veticesTransformSteps = 20;

    [SerializeField] float cutStep = 0;
    float objectAlignScaling = 1;

    float genObjectDistance;
    Mesh instancedMesh;
    GameObject copied;
    List<GameObject> copies = new List<GameObject>();
    List<GameObject> gened = new List<GameObject>();
    int VerticesId;


    Vector3[] originVertices;

    Vector3[] transformedVertices;

    Vector3[] originNormals;
    Vector3[] transformedNormals;
    bool needNewObject;
    bool transforming;
    public bool started;
    bool ReplaceMesh;

    private void OnValidate()
    {
        if(meshrendererObjectPrefab != null && PrefabUtility.GetPrefabAssetType(meshrendererObjectPrefab) == PrefabAssetType.NotAPrefab)
        {
            meshrendererObjectPrefab = null;
        }
    }

    int gameObjID = 0;

    public void selectFolder()
    {
        string newSaveFolder = EditorUtility.SaveFolderPanel("Save Folder", saveFolder.Length < 0 ? Application.dataPath : (saveFolder), string.Empty);
        
        if(!String.IsNullOrEmpty(newSaveFolder))
        {
            saveFolder = newSaveFolder;
            //Debug.Log(saveFolder);
            saveFolder = saveFolder.Remove(0, Application.dataPath.Length - 6);
            //Debug.Log(saveFolder);
        }
    }

    public void setEndFromPath()
    {

        TilingEnd = cinemachinePath.PathLength;
    }
    public void startTiling()
    {
        genObjectDistance = TilingStart;
        started = true;
        needNewObject = true;
        ReplaceMesh = false;

        if (TilingEnd == 0)
        {
            setEndFromPath();
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
        genObjectDistance = TilingStart;
        TilingEnd = cinemachinePath.PathLength;
        started = true;
        needNewObject = true;
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
        genObjectDistance = TilingStart;
        started = false;
        needNewObject = false;
        transforming = false;
        EditorUtility.ClearProgressBar();
    }


    private Vector3 GetGlobalPathPositionOnPath_FromDistance(float generatingDistance)
    {
        float generatingPathUnit = cinemachinePath.ToNativePathUnits(generatingDistance, PositionUnits.Distance);
        return GetGlobalPathPositionOnPath_FromPathUnit(generatingDistance, generatingPathUnit);
    }
    private Vector3 GetGlobalPathPositionOnPath_FromPathUnit(float generatingDistance, float generatingPathUnit)
    {
        if (generatingDistance > cinemachinePath.PathLength)
        {
            return cinemachinePath.EvaluatePositionAtUnit(generatingPathUnit, PositionUnits.PathUnits) +
                cinemachinePath.EvaluateTangentAtUnit(generatingPathUnit, PositionUnits.PathUnits).normalized * (generatingDistance - cinemachinePath.PathLength);
        }
        else if (generatingDistance < 0)
        {
            return cinemachinePath.EvaluatePositionAtUnit(generatingPathUnit, PositionUnits.PathUnits)
                + cinemachinePath.EvaluateTangentAtUnit(generatingPathUnit, PositionUnits.PathUnits).normalized * (generatingDistance);
        }
        else
        {
            return cinemachinePath.EvaluatePositionAtUnit(generatingPathUnit, PositionUnits.PathUnits);
        }
    }

    public void setUpNewObject()
    {
        copied = (GameObject)PrefabUtility.InstantiatePrefab(meshrendererObjectPrefab);

        float ObjectDistance = this.genObjectDistance + (isZinverted ? +modelLength : 0);
        float remainLength = TilingEnd - ObjectDistance;
        float genPathUnit = cinemachinePath.ToNativePathUnits(ObjectDistance, PositionUnits.Distance);

        Quaternion rotation;
        getRotationOnT(genPathUnit, out rotation, Quaternion.identity);
        copied.transform.rotation = rotation;
        copied.transform.position = GetGlobalPathPositionOnPath_FromPathUnit(ObjectDistance, genPathUnit) + rotation * this.generationOffset;
        copied.transform.SetParent(this.root != null ? this.root : cinemachinePath.transform);

        instancedMesh = Instantiate(copied.GetComponent<MeshFilter>().sharedMesh);
        VerticesId = 0;
        originVertices = instancedMesh.vertices;
        originNormals = instancedMesh.normals;
        transformedVertices = new Vector3[instancedMesh.vertices.Length];
        transformedNormals = new Vector3[instancedMesh.vertices.Length];

        objectAlignScaling = 1;
        if (ObjectDistance + modelLength >= TilingEnd)
        {

            if (cutStep > 0)
            {
                //Debug.Log("rem " + remainLength);
                //Debug.Log("round " + (Mathf.Round(remainLength / cutStep) * cutStep));
                objectAlignScaling = remainLength / Mathf.Round(remainLength / cutStep) * cutStep;
                Debug.Log("scale " + objectAlignScaling);
                Vector3 scaler = new Vector3(1, 1, objectAlignScaling);
                originVertices = originVertices.Select(
                    vertex => {
                        vertex.Scale(scaler);
                        return vertex;
                    }
                    ).ToArray();
                if (Mathf.Abs(Mathf.Round(remainLength / cutStep) * cutStep - modelLength) > disbaleInstancedThreshold)
                    ReplaceMesh = true;
            }
            else
            {
                ReplaceMesh = true;
            }
        }
        if (objectAlignScaling <= 0)
        {
            this.genObjectDistance = TilingEnd;
            Destroy(copied);
            return;
        }
        needNewObject = false;
        transforming = true;

    }

    public void transformVetices()
    {
        for(int stepsCnt = 0; stepsCnt < veticesTransformSteps; stepsCnt++)
        {
            if (VerticesId < originVertices.Length)
            {
                float trnsfVtxDistance = this.genObjectDistance + (isZinverted ? +modelLength * objectAlignScaling : 0) + originVertices[VerticesId].z;
                float t = cinemachinePath.ToNativePathUnits(trnsfVtxDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
                Vector3 originPos = new Vector3(0, 0, originVertices[VerticesId].z);
                Vector3 offset = copied.transform.InverseTransformPoint(GetGlobalPathPositionOnPath_FromPathUnit(trnsfVtxDistance,t)) - originPos;
                if (!ReplaceMesh && offset.sqrMagnitude > disbaleInstancedThreshold) ReplaceMesh = true;
                Quaternion rotation;
                getRotationOnT(t, out rotation, Quaternion.Inverse(copied.transform.rotation));
                transformedVertices[VerticesId] = originPos + rotation * this.generationOffset +  rotation * (originVertices[VerticesId] - originPos) + offset;
                transformedNormals[VerticesId] = rotation * originNormals[VerticesId];
                VerticesId++;
            }
            else
            {
                transforming = false;
            }
        }
        EditorUtility.DisplayProgressBar("RailModelTiler", "Vetices Transforming...", (genObjectDistance + modelLength * VerticesId / originVertices.Length) / TilingEnd);
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
            instancedMesh.SetNormals(transformedNormals);
            if (genObjectDistance + (isZinverted ? +modelLength : modelLength) > TilingEnd)
            {
                for (int subMeshID = 0; subMeshID < instancedMesh.subMeshCount; subMeshID++)
                {
                    //Debug.Log("Cutting" + subMeshID);
                    string progressName = "Cuting Poly... material " + subMeshID;
                    int CheckedPoliesCnt = 0;
                    int[] OriginPolies = instancedMesh.GetTriangles(subMeshID);
                    int[] CheckedPolies = new int[OriginPolies.Length];
                    for (int PoliesId = 0; PoliesId < OriginPolies.Length; PoliesId += 3)
                    {
                        bool check = false;
                        for (int triangleID = 0; triangleID < 3; triangleID++)
                        {
                            if ((genObjectDistance + (isZinverted ? +modelLength : 0) + originVertices[OriginPolies[PoliesId + triangleID]].z) < TilingEnd + cutterOffset)
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
            instancedMesh.RecalculateTangents();
            instancedMesh.name = copied.name;
            copied.GetComponent<MeshFilter>().sharedMesh = instancedMesh;
            gameObjID++;
            copied.name += gameObjID;
            ReplaceMesh = false;

            copies.Add(copied);
        }
        else
        {
            float t = cinemachinePath.ToNativePathUnits(genObjectDistance + (isZinverted ? 0 : +modelLength), PositionUnits.Distance);
            Quaternion rotation;
            getRotationOnT(t, out rotation, Quaternion.identity);
            if (isZinverted)
            {
                copied.transform.forward = copied.transform.position - (GetGlobalPathPositionOnPath_FromPathUnit(genObjectDistance + (isZinverted ? 0 : +modelLength), t) + rotation * this.generationOffset);
            }
            else
            {
                copied.transform.forward = GetGlobalPathPositionOnPath_FromPathUnit(genObjectDistance + (isZinverted ? 0 : +modelLength), t) + rotation * this.generationOffset - copied.transform.position;
            }

            Vector3 fitScale = copied.transform.localScale;
            fitScale.z = (GetGlobalPathPositionOnPath_FromPathUnit(genObjectDistance + (isZinverted ? 0 : +modelLength), t) + rotation * this.generationOffset - copied.transform.position).magnitude / modelLength;
            copied.transform.localScale = fitScale;

            copied.name += "instanced";
        }
        gened.Add(copied);
        needNewObject = true;
        transFormChildObject(this,copied.transform , copied.transform);

        if (root == null)
        {
            copied.transform.parent = cinemachinePath.transform;
        }
        else
        {
            copied.transform.parent = root;
        }
        this.genObjectDistance += modelLength;


        if (this.genObjectDistance >= TilingEnd)
        {
            if (UseColliderBaseCuller)
            {
                Vector3[] cullerCenters;
                List<GameObject[]> ClusteredGoList = new List<GameObject[]>();
                pathModelCullerSetup.ObjectClustering(cinemachinePath, 500, gened, out cullerCenters, ref ClusteredGoList);
                pathModelCullerSetup.SetUpColliderBaseCuller(ClusteredGoList, cullerCenters, this.root != null ? this.root : cinemachinePath.transform,true,true);
            }

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

                exportedModel = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
                //ModelExporter.ExportObjects(filePath, exporter);

                for (int index = 0; index < exporter.Length; index++)
                {
                    DestroyImmediate(exporter[index]);
                }
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
        MethodInfo exportObjectMethod = typeof(ModelExporter).GetMethod("ExportObjects", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(string), typeof(UnityEngine.Object[]), optionsInterfaceType, closedType }, null);
        exportObjectMethod.Invoke(null, new object[] { filePath, Objects, optionsInstance, null });

        AssetDatabase.StartAssetEditing();
        try
        {
            AssetImporter importer = AssetImporter.GetAtPath(filePath);
            if (importer is ModelImporter)
            {
                ((ModelImporter)importer).materialImportMode = ModelImporterMaterialImportMode.None;
                AssetDatabase.ImportAsset(filePath);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    protected static void transFormChildObject(railModelTiler tilerInstance, Transform transformBasis, Transform currentTransform)
    {
        foreach (Transform child in currentTransform)
        {
            Vector3 localPos = transformBasis.transform.InverseTransformPoint(child.position);
            float generatingDistance = tilerInstance.genObjectDistance + (tilerInstance.isZinverted ? +tilerInstance.modelLength : 0) + localPos.z;
            float t = tilerInstance.cinemachinePath.ToNativePathUnits(generatingDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
            Vector3 originPos = new Vector3(0, 0, localPos.z);
            Vector3 offset = transformBasis.transform.InverseTransformPoint(tilerInstance.cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits)) - originPos;
            if (generatingDistance > tilerInstance.cinemachinePath.PathLength)
            {
                offset = transformBasis.transform.InverseTransformPoint
                    (
                    tilerInstance.cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits) +
                    tilerInstance.cinemachinePath.EvaluateTangentAtUnit(t,PositionUnits.PathUnits).normalized
                    * (generatingDistance - tilerInstance.cinemachinePath.PathLength)
                    )
                    - originPos;
            }
            Quaternion childLocatedRotation = tilerInstance.cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits);

            if (tilerInstance.ignoreRoll || tilerInstance.ignorePitch)
            {
                Vector3 eulered = childLocatedRotation.eulerAngles;
                if (tilerInstance.ignoreRoll) eulered.z = 0;
                if (tilerInstance.ignorePitch) eulered.x = 0; ;
                childLocatedRotation = Quaternion.Euler(eulered.x, eulered.y, eulered.z);
            }
            Quaternion inversedChildRotation = Quaternion.Inverse(transformBasis.transform.rotation);
            Quaternion rotation = inversedChildRotation * childLocatedRotation;

            child.position = transformBasis.transform.TransformPoint(originPos + rotation * (localPos - originPos) + offset + childLocatedRotation * tilerInstance.generationOffset);
            child.rotation = childLocatedRotation;
            if (PrefabUtility.GetPrefabInstanceHandle(child) != tilerInstance.meshrendererObjectPrefab) continue;
            transFormChildObject(tilerInstance, transformBasis,child);
        }
    }

    public void Update()
    {
        if (started)
        {
            if (genObjectDistance < TilingEnd)
            {
                if (needNewObject) setUpNewObject();
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
                //ShutDown
                started = false;
                if (this.genObjectDistance >= TilingEnd)
                {
                    EditorUtility.ClearProgressBar();

                    string filePath = Path.Combine(saveFolder, meshrendererObjectPrefab.name + FBXID + "Compressing.fbx");
                    AssetDatabase.ImportAsset(filePath);
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
                    else if (copies.Count == 1)
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

        Vector3 PositionAtT;
        Quaternion OrientationAtT;
        PositionAtT = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.PathUnits);
        OrientationAtT = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized;
        Gizmos.DrawLine(
            PositionAtT + OrientationAtT * (-Vector3.right + this.generationOffset),
            PositionAtT + OrientationAtT * (Vector3.right + this.generationOffset));
        Gizmos.DrawLine(
            PositionAtT + OrientationAtT * (-Vector3.up + this.generationOffset),
            PositionAtT + OrientationAtT * (Vector3.up + this.generationOffset));
        Gizmos.color = new Color(1f, 0, 0f, 1f);
        t = cinemachinePath.ToNativePathUnits(TilingEnd, PositionUnits.Distance);
        PositionAtT = GetGlobalPathPositionOnPath_FromPathUnit(TilingEnd, t);
        OrientationAtT = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized;
        Gizmos.DrawLine(
            PositionAtT + OrientationAtT * (-Vector3.right + this.generationOffset),
            PositionAtT + OrientationAtT * (Vector3.right + this.generationOffset));
        Gizmos.DrawLine(
            PositionAtT + OrientationAtT * (-Vector3.up + this.generationOffset),
            PositionAtT + OrientationAtT * (Vector3.up + this.generationOffset));

        if (modelLength > 2)
        {
            for (float genDist = TilingStart + modelLength; genDist < TilingEnd;)
            {
                t = cinemachinePath.ToNativePathUnits(genDist, PositionUnits.Distance);
                Gizmos.color = new Color(1f, 1f, 0f, 1f);
                PositionAtT = GetGlobalPathPositionOnPath_FromPathUnit(genDist,t);
                OrientationAtT = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.PathUnits).normalized;
                Gizmos.DrawLine(
                    PositionAtT + OrientationAtT * (-Vector3.right + this.generationOffset),
                    PositionAtT + OrientationAtT * (Vector3.right + this.generationOffset));
                Gizmos.DrawLine(
                    PositionAtT + OrientationAtT * (-Vector3.up + this.generationOffset),
                    PositionAtT + OrientationAtT * (Vector3.up + this.generationOffset));
                genDist += modelLength;
            }
        }

    }

    public Transform root;
    [FormerlySerializedAs("offset")]
    public Vector3 generationOffset;
    public void moveCinemachine()
    {
        applyMoveToCinemachine(root);
    }
    public void applyMoveToCinemachine(Transform currentTransform)
    {
        //Debug.Log("on " + currentTransform.name);
        foreach (Transform child in currentTransform)
        {
            CinemachineSmoothPath childCinemachine;
            if ((childCinemachine = child.GetComponent<CinemachineSmoothPath>()) != null)
            {
                for (int id = 0;id< childCinemachine.m_Waypoints.Length;id ++)
                {
                    Waypoint wp = new CinemachineSmoothPath.Waypoint();
                    wp.position = childCinemachine.m_Waypoints[id].position + generationOffset;
                    childCinemachine.m_Waypoints[id] = wp;
                }
                childCinemachine.EvaluatePosition(0);
            }
            applyMoveToCinemachine(child);
        }
    }

}

#endif