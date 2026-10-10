

#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(railModelLocator), true), CanEditMultipleObjects]
public class railModelLocator_Editor : Editor
{
    // Start is called before the first frame update
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        railModelLocator tiler = (railModelLocator)target;
        if (GUILayout.Button("TilingRails"))
        {
            Debug.Log("TilingStart");
            tiler.startTiling();
        }

        if (GUILayout.Button("Cancel"))
        {
            Debug.Log("TilingCancel");
            tiler.cancelTiling();
        }
    }
}
#endif