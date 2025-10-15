using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Obi;

public class HaptoRigid : HaptoObject
{
    public SkinnedMeshRenderer skinnedMeshRenderer;

    protected override void Awake()
    {
        base.Awake();
        setRenderer(skinnedMeshRenderer);
        //renderer = skinnedMeshRenderer;
    }

    private void Start()
    {
        renderer = skinnedMeshRenderer;
    }

    public void setRenderer(SkinnedMeshRenderer smr)
    {
        renderer = smr;
    }

    public override void SetObject(int _id, int maxNumPoint, int maxNumCluster)
    {
        base.SetObject(_id, maxNumPoint, maxNumCluster);
    }

    public override void SendObject()
    {
        base.SendObject();
    }

    public override void ApplyForce()
    {
        base.ApplyForce();
    }

    public override void ApplyForceGPU(float coeffPointForceBase)
    {
        base.ApplyForceGPU(coeffPointForceBase);
    }
}
