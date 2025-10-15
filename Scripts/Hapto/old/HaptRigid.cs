using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Obi;

#if UNITY_EDITOR
[CustomEditor(typeof(HaptObject))]
#endif

public class HaptRigid : HaptObject
{
    public SkinnedMeshRenderer skinnedMeshRenderer;

    protected override void Awake()
    {
        base.Awake();
        renderer = skinnedMeshRenderer;
    }

    public override void setObject(int _id, int maxNumPoint)
    {
        base.setObject(_id, maxNumPoint);
    }

    public override void applyForce()
    {
        base.applyForce();
    }
}
