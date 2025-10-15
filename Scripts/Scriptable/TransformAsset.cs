using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "MultiTransform", menuName = "ScriptableObjects/MultiTransform")]
public class MultiTransformAsset : ScriptableObject
{
    public List<Trans> transforms;
}

[System.Serializable]
public struct Trans
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
