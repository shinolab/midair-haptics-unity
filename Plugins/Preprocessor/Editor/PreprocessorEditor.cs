using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Preprocessor), true)]

public class PreprocessorEditor : Editor
{
    private bool foldTrim, foldColor, foldErode, foldSample, foldSdf;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        foldTrim = EditorGUILayout.Foldout(foldTrim, "Truncate");
        serializedObject.FindProperty("truncate").boolValue = EditorGUILayout.ToggleLeft("", serializedObject.FindProperty("truncate").boolValue);
        EditorGUILayout.EndHorizontal();
        if (foldTrim)
        {
            EditorGUI.indentLevel++;
            //    serializedObject.FindProperty("minRegion").vector3Value = EditorGUILayout.Vector3Field("minRegion", serializedObject.FindProperty("minRegion").vector3Value);
            //    serializedObject.FindProperty("maxRegion").vector3Value = EditorGUILayout.Vector3Field("maxRegion", serializedObject.FindProperty("maxRegion").vector3Value);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minRegion"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRegion"));
            EditorGUI.indentLevel--;
        }


        EditorGUILayout.BeginHorizontal();
        foldColor = EditorGUILayout.Foldout(foldColor, "Color Trim");
        serializedObject.FindProperty("colorTrim").boolValue = EditorGUILayout.ToggleLeft("", serializedObject.FindProperty("colorTrim").boolValue);
        EditorGUILayout.EndHorizontal();
        if (foldColor)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HSVW").FindPropertyRelative("H"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HSVW").FindPropertyRelative("S"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HSVW").FindPropertyRelative("V"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HSVW").FindPropertyRelative("width"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.BeginHorizontal();
        foldErode = EditorGUILayout.Foldout(foldErode, "Erode");
        serializedObject.FindProperty("erode").boolValue = EditorGUILayout.ToggleLeft("", serializedObject.FindProperty("erode").boolValue);
        EditorGUILayout.EndHorizontal();
        if (foldErode)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numErosion"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.BeginHorizontal();
        foldSample = EditorGUILayout.Foldout(foldSample, "Voxel Sampling");
        serializedObject.FindProperty("sampleCellMean").boolValue = EditorGUILayout.ToggleLeft("", serializedObject.FindProperty("sampleCellMean").boolValue);
        EditorGUILayout.EndHorizontal();
        if (foldSample)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numGrid"));
            EditorGUI.indentLevel--;
        }


        EditorGUILayout.BeginHorizontal();
        foldSdf = EditorGUILayout.Foldout(foldSdf, "SDF");
        serializedObject.FindProperty("sdf").boolValue = EditorGUILayout.ToggleLeft("", serializedObject.FindProperty("sdf").boolValue);
        EditorGUILayout.EndHorizontal();
        if (foldSdf)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minVal"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numGridSdf"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minRegionSdf"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRegionSdf"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
