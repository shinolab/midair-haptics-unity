using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class LoadCameraPosition : MonoBehaviour
{
    public CameraPositionAsset cameraPositionAsset;
    public GameObject device;
    public bool loadOnStart = false;

    private void Start()
    {
        if (loadOnStart)
        {
            Load();
        }
    }   

    public void Save()
    {
#if UNITY_EDITOR
        cameraPositionAsset.cameras.Clear();
        foreach (var obj in FindObjectsOfType<RS_Device>(false))
        {
            CameraPosition cp;
            cp.serialNumber = obj.serialNumber;
            cp.position = obj.transform.localPosition;
            cp.rotation = obj.transform.localRotation;
            cp.imageWidth = obj.imageWidth;
            cp.imageHeight = obj.imageHeight;
            cp.preset = obj.preset;
            cameraPositionAsset.cameras.Add(cp);
        }

        Debug.Log("Save camera positions");
        EditorUtility.SetDirty(cameraPositionAsset);
        AssetDatabase.SaveAssets();
#endif
    }

    public void Load()
    {
        Debug.Log("Load camera positions");
        List<string> serials = new List<string>();
        var devices = FindObjectsOfType<RS_Device>(false);
        foreach (var dev in devices)
            serials.Add(dev.serialNumber);

        foreach (var cp in cameraPositionAsset.cameras)
        {
            var index = serials.IndexOf(cp.serialNumber);
            if (index < 0)
            {
                var dev = Instantiate(device);
                dev.transform.parent = this.transform;
                dev.GetComponent<RS_Device>().serialNumber = cp.serialNumber;
                dev.GetComponent<RS_Device>().imageWidth = cp.imageWidth;
                dev.GetComponent<RS_Device>().imageHeight = cp.imageHeight;
                dev.GetComponent<RS_Device>().preset = cp.preset;
                dev.transform.localPosition = cp.position;
                dev.transform.localRotation = cp.rotation;
            }
            else
            {
                devices[index].imageWidth = cp.imageWidth;
                devices[index].imageHeight = cp.imageHeight;
                devices[index].preset = cp.preset;
                devices[index].transform.localPosition = cp.position;
                devices[index].transform.localRotation = cp.rotation;
            }
        }
    }
}
