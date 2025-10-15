using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AUTDController), true)]

public class AUTDControllerEditor : Editor
{
    private bool foldout, foldout1;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        foldout = EditorGUILayout.Foldout(foldout, "LoadCsv");
        serializedObject.FindProperty("loadCsv").boolValue = EditorGUILayout.ToggleLeft("", serializedObject.FindProperty("loadCsv").boolValue);
        EditorGUILayout.EndHorizontal();
        if (foldout)
        {
            serializedObject.FindProperty("pathCsv").stringValue = EditorGUILayout.TextField("Path", serializedObject.FindProperty("pathCsv").stringValue);
        }

         serializedObject.ApplyModifiedProperties();
    }
}
