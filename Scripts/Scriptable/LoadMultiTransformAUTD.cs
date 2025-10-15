using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LoadMultiTransformAUTD : MonoBehaviour
{
    public MultiTransformAsset tr;
    public string pathWrite  = "";
    public float scaleWrite = 100f;
    public bool visible = false;
    bool visiblePrev = false;

    public void Save()
    {
#if UNITY_EDITOR
        tr.transforms.Clear();
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            Trans trans;
            trans.position = obj.transform.position;
            trans.rotation = obj.transform.rotation;
            trans.scale = obj.transform.localScale;
            tr.transforms.Add(trans);
        }

        Debug.Log("Save transform of AUTDs");
        EditorUtility.SetDirty(tr);
        AssetDatabase.SaveAssets();
#endif
    }

    public void Load()
    {
        Debug.Log("Load transform of AUTDs");
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            Trans trans = tr.transforms[obj.ID];
            obj.transform.position = trans.position;
            obj.transform.rotation = trans.rotation;
            obj.transform.localScale = trans.scale;
        }
    }

    public void Write()
    {
        var filename = pathWrite + "/" + tr.name + ".txt";
        using (StreamWriter writer = new StreamWriter(filename, false))
        {
            foreach(var trans  in tr.transforms)
            {
                writer.WriteLine(trans.position * scaleWrite);
                writer.WriteLine(trans.rotation);
                Debug.Log(trans.position);
                Debug.Log(trans.rotation);
            }
            writer.Flush();
        }
    }

    public void WriteMatrix()
    {
        var filename = pathWrite + "/" + tr.name + ".txt";
        using (StreamWriter writer = new StreamWriter(filename, false))
        {
            foreach (var trans in tr.transforms)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(trans.position * scaleWrite, trans.rotation, Vector3.one);
                writer.Write(matrix);
            }
            writer.Flush();
        }
    }

    public void Visible()
    {
        foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
        {
            Trans trans = tr.transforms[obj.ID];
            obj.transform.position = trans.position;
            obj.transform.rotation = trans.rotation;
            obj.transform.localScale = trans.scale;
        }
    }

    private void OnValidate()
    {
        if (visible != visiblePrev)
        {
            visiblePrev = visible;
            foreach (var obj in FindObjectsOfType<AUTD3Device>(false).OrderBy(obj => obj.ID))
            {
                obj.transform.Find("Transducer").GetComponent<Renderer>().enabled = visible;
            }
        }
    }
}
