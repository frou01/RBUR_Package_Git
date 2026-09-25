

#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(railModelTiler), true), CanEditMultipleObjects]
public class railModelTiler_Editor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        railModelTiler tiler = (railModelTiler)target;
        if (GUILayout.Button("TilingRails"))
        {
            Debug.Log("TilingStart");
            tiler.startTiling();
        }
        if (GUILayout.Button("SetEndFromPath"))
        {
            Debug.Log("SetEndFromPath");
            tiler.setEndFromPath();
        }
        if (GUILayout.Button("TilingRailAll"))
        {
            Debug.Log("TilingStartAll");
            tiler.startTilingAll();
        }

        if (GUILayout.Button("Cancel"))
        {
            Debug.Log("TilingCancel");
            tiler.cancelTiling();
        }
        if (GUILayout.Button("SelectFolder"))
        {
            tiler.selectFolder();
        }
        if (GUILayout.Button("Offset CinemachinePath"))
        {
            tiler.moveCinemachine();
        }
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField("Exported FBX",tiler.exportedModel, typeof(GameObject), false);
        EditorGUI.EndDisabledGroup();
    }
}
#endif