using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadTransform))]//拡張するクラスを指定
public class LoadTransformEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LoadTransform loadTransform = target as LoadTransform;

        //PublicMethodを実行する用のボタン
        if (GUILayout.Button("Load"))
        {
            loadTransform.Load();
        }
        if (GUILayout.Button("Save"))
        {
            loadTransform.Save();
        }

    }

}
