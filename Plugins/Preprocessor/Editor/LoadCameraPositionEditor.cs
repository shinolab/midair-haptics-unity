using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadCameraPosition))]
public class LoadCameraPositionEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LoadCameraPosition loadCameraPosition = target as LoadCameraPosition;

        if (GUILayout.Button("Load"))
        {
            loadCameraPosition.Load();
        }
        if (GUILayout.Button("Save"))
        {
            loadCameraPosition.Save();
        }

    }

}
