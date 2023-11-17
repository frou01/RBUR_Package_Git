

#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(railModelTiler_Batcher), true), CanEditMultipleObjects]
public class railModelTiler_Batcher_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        railModelTiler_Batcher batcher = (railModelTiler_Batcher)target;
        if (GUILayout.Button("Batching"))
        {
            batcher.startBatch();
        }

        if (GUILayout.Button("Cancel"))
        {
            batcher.cancelBatch();
        }
    }
}
#endif
