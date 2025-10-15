using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Obi;

#if UNITY_EDITOR
[CustomEditor(typeof(HaptObject))]
#endif

public class HaptCloth : HaptObject
{
    bool firstFixed = true;
    ObiCloth cloth;
    SkinnedMeshRenderer rendererTarget;
    //List<List<int>> ids;
    //List<List<float>> weights;
    List<float> weightsVertex;

    Vector3[] verticesTarget;
    Vector3[] diffPrev;
    Mesh meshTarget;
    int numParticle;
    ObiSolver solver;
    float ratio = 0.001f;

    public GameObject skinnedMesh;
    public GameObject clothMesh;
    public float coefficientK = 30f;
    public float coefficeintD = 30f;
    //public int numParticleConnected = 4;
    public bool useWeightVertex = false;


    protected override void Awake()
    {
        base.Awake();
        rendererTarget = skinnedMesh.GetComponent<SkinnedMeshRenderer>();
        cloth = clothMesh.GetComponent<ObiCloth>();
        filter = clothMesh.GetComponent<MeshFilter>();
        meshTarget = new Mesh();
    }

    private void Start()
    {
        solver = cloth.solver;
    }

    public override void setObject(int _id, int maxNumPoint)
    {
        setWeights();
        //base.setObject(_id, maxNumPoint);
    }


    void setWeights()
    {
        numParticle = cloth.particleCount;
        numVertex = numParticle;
        diffPrev = new Vector3[numVertex];

        //weights = new List<List<float>>();
        //ids = new List<List<int>>();

        //var verticesTmp = meshTmp.vertices;
        //for (int i = 0; i < numVertex; i++)
        //{
        //    Vector3 vert = softMesh.transform.TransformPoint(verticesTmp[i]);
        //    var list = new List<float>();
        //    for (int j = 0; j < numParticle; j++)
        //    {
        //        Vector3 pos = solver.renderablePositions[softbody.GetParticleRuntimeIndex(j)];
        //        float dist = (pos - vert).magnitude;
        //        list.Add(dist);
        //    }
        //    var sorted = list.Select((x, i) => new KeyValuePair<float, int>(x, i)).OrderBy(x => x.Key);

        //    List<float> tmpw = new List<float>();
        //    List<int> tmpi = new List<int>();
        //    float sum = 0;
        //    foreach (KeyValuePair<float, int> kv in sorted.Take(numParticleConnected))
        //    {
        //        tmpw.Add(1.0f / kv.Key);
        //        tmpi.Add(kv.Value);
        //        sum += 1.0f / kv.Key;
        //    }
        //    for (int j = 0; j < numParticleConnected; j++)
        //    {
        //        tmpw[j] /= sum;
        //    }
        //    weights.Add(tmpw);
        //    ids.Add(tmpi);
        //}

        ////weightsVertex = new List<float>();
        ////triangles = meshTmp.triangles;
        ////numFace = triangles.Length / 3;
        ////List<float> areas = new List<float>();
        ////float sumArea = 0;
        ////for (int i = 0; i < numVertex; i++) areas.Add(0);
        ////for (int i = 0; i < numFace; i++)
        ////{
        ////    var v0 = verticesTmp[triangles[3 * i]];
        ////    var v1 = verticesTmp[triangles[3 * i + 1]];
        ////    var v2 = verticesTmp[triangles[3 * i + 2]];
        ////    float area = Vector3.Cross((v1 - v0), (v2 - v0)).magnitude / 2;
        ////    areas[triangles[3 * i]] += area;
        ////    areas[triangles[3 * i + 1]] += area;
        ////    areas[triangles[3 * i + 2]] += area;
        ////    sumArea += area;
        ////    //Debug.Log(area);
        ////}

        ////float meanArea = sumArea * 3.0f / numVertex;
        ////for (int i = 0; i < numVertex; i++)
        ////{
        ////    weightsVertex.Add(areas[i] / meanArea);
        ////    //Debug.Log(i + ": " + weightsVertex[i]);
        ////}

    }

    public override void applyForce()
    {
        rendererTarget.BakeMesh(meshTarget);
        verticesTarget = meshTarget.vertices;
        sumDeformation = 0;

        if (GetComponent<Rigidbody>()) base.applyForce();
        Vector3 posP, posC;

        for (int i = 0; i < cloth.particleCount; i += 1)
        {
            //posP = vertices[i];
            posP = solver.renderablePositions[cloth.GetParticleRuntimeIndex(i)];
            posC = skinnedMesh.transform.TransformPoint(verticesTarget[i]);

            Vector3 posDiff = posC - posP;
            Vector3 velocityDiff = (posDiff - diffPrev[i]) / Time.fixedDeltaTime;

            if (firstFixed)
            {
                velocityDiff = new Vector3(0, 0, 0);
                firstFixed = false;
            }
            Vector4 force = (coefficientK * posDiff + coefficeintD * velocityDiff);
            if (useWeightVertex)
            {
                force = force * weightsVertex[i];
                sumDeformation += posDiff.magnitude * weightsVertex[i];
            }
            else
            {
                sumDeformation += posDiff.magnitude;
            }

            if (force.magnitude > coefficientK * posDiff.magnitude * 10)
                force *= coefficientK * posDiff.magnitude * 10 / force.magnitude;

            Vector4 forceEx = Vector4.zero;// externalForce[i];
            force += forceEx;

            solver.externalForces[cloth.GetParticleRuntimeIndex(i)] += ratio * force;
            //solver.externalForces[cloth.GetParticleRuntimeIndex(i)] += new Vector4(0, 0, 0, 1);

            diffPrev[i] = posDiff;

            //if (i < 3)
            //{
            //    //Debug.Log(i + ": " + posP[0] + ", " + posP[1] + ", " + posP[2]);
            //    //Debug.Log(i + ": " + posC[0] + ", " + posC[1] + ", " + posC[2]);
            //    var f = solver.externalForces[cloth.GetParticleRuntimeIndex(i)];
            //    //Debug.Log(i + ": " + f[0] + ", " + f[1] + ", " + f[2] + ", " + f[3]);
            //    Debug.Log(i + ": " + f.magnitude + " ratio: " + ratio);
            //}

            //if (forceEx.magnitude > maxv)
            //    maxv = forceEx.magnitude;
        }
        ratio = ratio  * 1.05f;
        if (ratio > 1) ratio = 1;
    }

}
