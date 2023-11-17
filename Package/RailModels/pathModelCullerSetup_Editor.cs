

#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(pathModelCullerSetup), true), CanEditMultipleObjects]
public class pathModelCullerSetup_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        pathModelCullerSetup tiler = (pathModelCullerSetup)target;
        if (GUILayout.Button("Start"))
        {
            Debug.Log("Start");
            tiler.perform();
        }
    }
}

#endif