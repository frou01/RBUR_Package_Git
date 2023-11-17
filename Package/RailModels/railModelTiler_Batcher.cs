using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if (UNITY_EDITOR)
[ExecuteAlways]
public class railModelTiler_Batcher : MonoBehaviour
{
    [SerializeField] railModelTiler railModelTiler;
    [SerializeField] GameObject batchingRoot;
    [SerializeField] CinemachinePathBase[] batchingObject;
    int current;
    bool batching;
    // Start is called before the first frame update
    void Start()
    {
    }

    internal void startBatch()
    {
        if(batchingRoot) batchingObject = batchingRoot.GetComponentsInChildren<CinemachinePathBase>();
        batching = batchingObject != null;
    }

    internal void cancelBatch()
    {
        batching = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(batchingObject != null && current >= batchingObject.Length)
        {
            current = 0;
            batching = false;
        }
        if (batching && !railModelTiler.started)
        {
            Debug.Log("batch progress" + current);
            railModelTiler.cinemachinePath = batchingObject[current];
            railModelTiler.startTilingAll();
            current++;
        }
        if (!batchingRoot && batchingObject == null)
        {
            batching = false;
        }
    }
    public void OnDrawGizmos()
    {
        // Your gizmo drawing thing goes here if required...

        // Ensure continuous Update calls.
        if (!Application.isPlaying && batching)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
    }
}

#endif