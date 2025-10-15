using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Obi;


public class HaptObject : MonoBehaviour
{
    new Rigidbody? rigidbody = null;

    public bool applyforce = true;
    public float radiusBound = 0.45f;
    public Vector3 centerBound = new Vector3(0, 0, 0);
    public int startFaceCollision = -1;
    public int endFaceCollision = -1;

    [System.NonSerialized] public int id;  // for Collision Ditection

    [System.NonSerialized] public int numFace;
    [System.NonSerialized] public int numFaceCollision;
    [System.NonSerialized] public int[] triangles;
    [System.NonSerialized] public int[] trianglesCollision;

    [System.NonSerialized] public int numVertex;
    [System.NonSerialized] public Vector3[] vertices;
    [System.NonSerialized] public float[] verticesF;

    [System.NonSerialized] public Vector3[] externalForce;
    [System.NonSerialized] public float[] externalForceF;
    [System.NonSerialized] public float ampForce;

    [System.NonSerialized] public new SkinnedMeshRenderer renderer = null;
    [System.NonSerialized] public new MeshFilter filter = null;
    [System.NonSerialized] public HaptReaction reaction = null;

    [System.NonSerialized] public float[] transformRigid;

    [System.NonSerialized] public float[] pointInObject;
    [System.NonSerialized] public float[] pointNearObject;
    [System.NonSerialized] public int numPointInObject;
    [System.NonSerialized] public int numPointNearObject;
    [System.NonSerialized] public int[] indexStartCluster;
    [System.NonSerialized] public int[] indexStartClusterNear;
    [System.NonSerialized] public int numCluster;
    [System.NonSerialized] public int numClusterNear;
    //[System.NonSerialized] public List<List<Vector3>> clusters;

    [System.NonSerialized] public float[] centroids;
    [System.NonSerialized] public float[] centroidsNear;
    [System.NonSerialized] public float[] directions;
    [System.NonSerialized] public float[] meanForce;
    [System.NonSerialized] public float[] sumForce;
    [System.NonSerialized] public int[] indexFaces;
    [System.NonSerialized] public int numTouch;
    [System.NonSerialized] public float sumDeformation = 0;
    [System.NonSerialized] public HaptFeedback feedback = null;


    protected virtual void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        reaction = GetComponent<HaptReaction>();
        feedback = GetComponent<HaptFeedback>();
    }

    public virtual void setObject(int _id, int maxNumPoint)
    {
        id = _id;
        var mesh = new Mesh();
        if (renderer != null)
            mesh = renderer.sharedMesh;
        else if (filter != null)
            mesh = filter.mesh;

        triangles = mesh.triangles;
        numVertex = mesh.vertices.Length;
        numFace = triangles.Length / 3;

        vertices = new Vector3[numVertex];
        verticesF = new float[3 * numVertex];
        externalForce = new Vector3[numVertex];
        externalForceF = new float[3 * numVertex];

        transformRigid = new float[12];
        pointInObject = new float[maxNumPoint * 3];
        pointNearObject = new float[maxNumPoint * 3];
        indexStartCluster = new int[100];
        indexStartClusterNear = new int[100];
        numPointInObject = 0;
        if (startFaceCollision >= 0)
        {
            int start = startFaceCollision;
            int end = endFaceCollision;
            if (end < 0)
                end = numFace;
            numFaceCollision = end - start;
            trianglesCollision = new int[3 * numFaceCollision];
            int index = 0;
            for (int i = 3 * start; i < 3 * end; i++)
            {
                trianglesCollision[index++] = triangles[i];
            }
        }
        else
        {
            numFaceCollision = numFace;
            trianglesCollision = mesh.triangles;
        }

        float[] _centerBound = new float[3];
        for (int i = 0; i < 3; i++) _centerBound[i] = centerBound[i];

        if(applyforce)
            CollisionDetection.setObject(numVertex, numFaceCollision, trianglesCollision, radiusBound, _centerBound);

        centroids = new float[300];
        centroidsNear = new float[300];
        directions = new float[300];
        meanForce = new float[300];
        sumForce = new float[300];
        indexFaces = new int[300];
    }


    public virtual void applyForce()
    {
        applyForceRigid();
    }

    public void applyForceRigid()
    {
        if (rigidbody == null) return;
        for (int i = 0; i < numVertex; i++)
        {
            var posP = vertices[i];
            rigidbody.AddForceAtPosition(externalForce[i], posP);

        }
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + centerBound, radiusBound);
    }
}
