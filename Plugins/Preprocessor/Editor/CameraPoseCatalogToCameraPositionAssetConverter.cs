using System.IO;
using UnityEditor;
using UnityEngine;

public static class CameraPoseCatalogToCameraPositionAssetConverter
{
    private const string MenuPath = "Assets/RealsenseCapture/Convert To CameraPositionAsset";
    private const string DefaultPreset = "Realsense/cameraPreset/preset.json";

    [MenuItem(MenuPath, false, 2001)]
    private static void ConvertSelectedAsset()
    {
        Object selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("[CameraPoseCatalogToCameraPositionAssetConverter] No asset selected.");
            return;
        }

        SerializedObject sourceObject = new SerializedObject(selected);
        SerializedProperty snapshots = sourceObject.FindProperty("snapshots");
        if (snapshots == null || !snapshots.isArray)
        {
            Debug.LogError("[CameraPoseCatalogToCameraPositionAssetConverter] Selected asset does not have a compatible snapshots array.");
            return;
        }

        CameraPositionAsset cameraPositionAsset = ScriptableObject.CreateInstance<CameraPositionAsset>();
        cameraPositionAsset.cameras.Clear();

        for (int i = 0; i < snapshots.arraySize; i++)
        {
            SerializedProperty snapshot = snapshots.GetArrayElementAtIndex(i);
            CameraPosition cameraPosition = new CameraPosition
            {
                serialNumber = ReadString(snapshot, "serialNumber"),
                position = ReadVector3(snapshot, "localPosition", "position"),
                rotation = ReadQuaternion(snapshot, "localRotation", "rotation"),
                imageWidth = ReadInt(snapshot, "imageWidth"),
                imageHeight = ReadInt(snapshot, "imageHeight"),
                preset = ReadPreset(snapshot)
            };

            cameraPositionAsset.cameras.Add(cameraPosition);
        }

        string sourcePath = AssetDatabase.GetAssetPath(selected);
        string directory = string.IsNullOrEmpty(sourcePath) ? "Assets" : Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrEmpty(directory))
            directory = "Assets";

        string sourceName = string.IsNullOrEmpty(sourcePath) ? selected.name : Path.GetFileNameWithoutExtension(sourcePath);
        string outputName = StripSuffix(sourceName, "_CameraPoseCatalog") + "_CameraPosition";
        string outputPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, outputName + ".asset").Replace("\\", "/"));
        cameraPositionAsset.name = Path.GetFileNameWithoutExtension(outputPath);

        AssetDatabase.CreateAsset(cameraPositionAsset, outputPath);
        EditorUtility.SetDirty(cameraPositionAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = cameraPositionAsset;
        EditorGUIUtility.PingObject(cameraPositionAsset);
        Debug.Log("[CameraPoseCatalogToCameraPositionAssetConverter] Converted: " + sourcePath + " -> " + outputPath);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateConvertSelectedAsset()
    {
        Object selected = Selection.activeObject;
        if (selected == null)
            return false;

        try
        {
            SerializedObject sourceObject = new SerializedObject(selected);
            SerializedProperty snapshots = sourceObject.FindProperty("snapshots");
            return snapshots != null && snapshots.isArray;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadString(SerializedProperty parent, string primaryName, string fallbackName = null)
    {
        SerializedProperty prop = FindProperty(parent, primaryName, fallbackName);
        return prop != null ? prop.stringValue : string.Empty;
    }

    private static string ReadPreset(SerializedProperty parent)
    {
        string preset = ReadString(parent, "presetFile", "preset");
        return string.IsNullOrEmpty(preset) ? DefaultPreset : preset;
    }

    private static int ReadInt(SerializedProperty parent, string primaryName, string fallbackName = null)
    {
        SerializedProperty prop = FindProperty(parent, primaryName, fallbackName);
        return prop != null ? prop.intValue : 0;
    }

    private static Vector3 ReadVector3(SerializedProperty parent, string primaryName, string fallbackName = null)
    {
        SerializedProperty prop = FindProperty(parent, primaryName, fallbackName);
        return prop != null ? prop.vector3Value : Vector3.zero;
    }

    private static Quaternion ReadQuaternion(SerializedProperty parent, string primaryName, string fallbackName = null)
    {
        SerializedProperty prop = FindProperty(parent, primaryName, fallbackName);
        return prop != null ? prop.quaternionValue : Quaternion.identity;
    }

    private static SerializedProperty FindProperty(SerializedProperty parent, string primaryName, string fallbackName)
    {
        SerializedProperty prop = parent.FindPropertyRelative(primaryName);
        if (prop != null || string.IsNullOrEmpty(fallbackName))
            return prop;

        return parent.FindPropertyRelative(fallbackName);
    }

    private static string StripSuffix(string value, string suffix)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(suffix))
            return value;

        return value.EndsWith(suffix) ? value.Substring(0, value.Length - suffix.Length) : value;
    }
}
