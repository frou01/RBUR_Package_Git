using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using static Cinemachine.CinemachinePathBase;
using System.Threading.Tasks;

#if (UNITY_EDITOR) 
[ExecuteAlways]
public class railModelLocator : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    Cinemachine.CinemachinePathBase cinemachinePath;
    [SerializeField] GameObject objectPrefab;
    [SerializeField] float modelLength;
    [SerializeField] float TilingStart;
    [SerializeField] float TilingEnd;

    [SerializeField] bool isZinverted;
    [SerializeField] bool ignoreRoll;
    [SerializeField] bool ignorePitch;
    
    
    float generatingDistance;
    GameObject copied;
    List<GameObject> gened = new List<GameObject>();



    bool setUp;
    bool started;

    void Start()
    {

    }

    int gameObjID = 0;

    public void selectFolder()
    {
    }
    public void startTiling()
    {
        generatingDistance = TilingStart;
        started = true;
        setUp = true;

        if(TilingEnd == 0)
        {
            TilingEnd = cinemachinePath.PathLength;
        }

        gameObjID = 0;
        gened.Clear();
    }
    public void cancelTiling()
    {
        generatingDistance = TilingStart;
        started = false;
        setUp = false;
        EditorUtility.ClearProgressBar();
    }
    public void setUpNewObject()
    {
        float generatingDistance = this.generatingDistance + (isZinverted ? +modelLength : 0);
        copied = (GameObject)PrefabUtility.InstantiatePrefab(objectPrefab);
        copied.transform.SetParent(this.transform);
        float t = cinemachinePath.StandardizeUnit(generatingDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
        copied.transform.position = cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.Distance);
        if (generatingDistance > cinemachinePath.PathLength)
        {
            copied.transform.position += cinemachinePath.EvaluateTangentAtUnit(t, PositionUnits.Distance).normalized * (generatingDistance - cinemachinePath.PathLength);
        }
        if (ignoreRoll || ignorePitch)
        {
            Vector3 fwd = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.Distance) * Vector3.forward;
            if(ignoreRoll)copied.transform.up = Vector3.up;
            else
            {
                Vector3 rgt = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.Distance) * Vector3.right;
                copied.transform.right = rgt;
            }
            if (ignorePitch) fwd.y = 0;
            copied.transform.forward = fwd;
        }
        else
        {
            copied.transform.rotation = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.Distance);
        }
        inversedCopiedRotation = Quaternion.Inverse(copied.transform.rotation);
        copied.name += gameObjID;
        setUp = false;
        gened.Add(copied);
        EditorUtility.DisplayProgressBar("RailModelTiler", "Object Copy and Transforming...", (generatingDistance + modelLength) / TilingEnd);
    }
    Quaternion inversedCopiedRotation;
    public void transformVetices()
    {
        
    }
    public void saveObject()
    {
        gameObjID++;
        setUp = true;
        transFormChildObject(copied.transform);
        this.generatingDistance += modelLength;


        if (this.generatingDistance >= TilingEnd)
        {
            railModelTiler.SetUpColliderBaseCuller(modelLength,gened, cinemachinePath.transform, false,false);
            EditorUtility.ClearProgressBar();
        }
    }

    public void transFormChildObject(Transform currentTransform)
    {
        foreach (Transform child in currentTransform)
        {
            Vector3 localPos = copied.transform.InverseTransformPoint(child.position);
            float tilingDistance = (generatingDistance + (isZinverted ? +modelLength : 0) + localPos.z);
            float t = cinemachinePath.StandardizeUnit(tilingDistance, PositionUnits.Distance);//z座標を元にレール座標を取得
            Vector3 originPos = new Vector3(0, 0, localPos.z);
            Vector3 offset = copied.transform.InverseTransformPoint(cinemachinePath.EvaluatePositionAtUnit(t, PositionUnits.Distance)) - originPos;
            if (tilingDistance > cinemachinePath.PathLength)
            {
                offset += cinemachinePath.EvaluateTangentAtUnit(t, PositionUnits.Distance).normalized * (tilingDistance - cinemachinePath.PathLength);
            }
            Quaternion childLocatedRotation = cinemachinePath.EvaluateOrientationAtUnit(t, PositionUnits.Distance);

            if (ignoreRoll || ignorePitch)
            {
                Vector3 eulered = childLocatedRotation.eulerAngles;
                if (ignoreRoll) eulered.z = 0;
                if (ignorePitch) eulered.x = 0; ;
                childLocatedRotation = Quaternion.Euler(eulered.x, eulered.y, eulered.z);
            }
            Quaternion rotation = inversedCopiedRotation * childLocatedRotation;

            child.position = copied.transform.TransformPoint(originPos + rotation * (localPos - originPos) + offset);
            child.rotation = childLocatedRotation;
            if (PrefabUtility.GetPrefabInstanceHandle(child) != objectPrefab) continue;
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
                saveObject();
            }

        }
        else
        {
            started = false;
        }
    }
    public void OnDrawGizmos()
    {
        // Your gizmo drawing thing goes here if required...

        // Ensure continuous Update calls.
        if (!Application.isPlaying && started && generatingDistance < TilingEnd)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
    }
}

#endif