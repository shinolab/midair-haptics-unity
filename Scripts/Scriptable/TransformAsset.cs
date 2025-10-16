using UnityEngine;

[CreateAssetMenu(fileName = "Transform", menuName = "ScriptableObjects/Transform")]
public class TransformAsset : ScriptableObject
{
    //public Transform transform;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
