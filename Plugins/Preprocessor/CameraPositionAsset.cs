using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "CameraPosition", menuName = "ScriptableObjects/CameraPosition")]
public class CameraPositionAsset : ScriptableObject
{
    public List<CameraPosition> cameras = new List<CameraPosition>();
}

[System.Serializable]
public struct CameraPosition
{
    public string serialNumber;
    public Vector3 position;
    public Quaternion rotation;
    public int imageWidth;
    public int imageHeight;
    public string preset;
}
