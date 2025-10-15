using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadMultiTransformAUTD))]
public class LoadMultiTransformAUTDEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LoadMultiTransformAUTD loadTransform = target as LoadMultiTransformAUTD;

        if (GUILayout.Button("Load"))
        {
            loadTransform.Load();
        }
        if (GUILayout.Button("Save"))
        {
            loadTransform.Save();
        }
        if (GUILayout.Button("Write"))
        {
            loadTransform.Write();
        }
        if (GUILayout.Button("WriteMatrix"))
        {
            loadTransform.WriteMatrix();
        }
    }

}
