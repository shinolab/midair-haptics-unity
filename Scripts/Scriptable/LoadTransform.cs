using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LoadTransform : MonoBehaviour
{
    public TransformAsset tr;

    public void Save()
    {
#if UNITY_EDITOR
        Debug.Log("Save transform");
        tr.position = transform.position;
        tr.rotation = transform.rotation;
        tr.scale = transform.localScale;
        EditorUtility.SetDirty(tr);
        AssetDatabase.SaveAssets();
#endif
    }

    public void Load()
    {
        Debug.Log("Load transform");
        transform.position = tr.position;
        transform.rotation = tr.rotation;
        transform.localScale = tr.scale;
    }
}
