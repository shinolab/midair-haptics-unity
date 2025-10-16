using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeHaptoObject : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public float scale = 1f;
    public HaptoController haptoController;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetHaptoObject();
        }
    }

    void SetHaptoObject()
    {
        var obj = new GameObject(mesh.name);
        obj.transform.position = transform.position;
        obj.transform.rotation = transform.rotation;
        obj.transform.parent = transform;
        obj.transform.localScale = new Vector3(scale, scale, scale);
        //var rigid = obj.AddComponent<Rigidbody>();
        //rigid.mass = 1000;
        //rigid.useGravity = false;

        var smr = obj.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mesh;
        smr.material = material;
        var hapt = obj.AddComponent<HaptoRigid>();
        hapt.skinnedMeshRenderer = smr;
        hapt.setRenderer(smr);

        haptoController.AddHaptoObject(hapt, -1);
    }
}
