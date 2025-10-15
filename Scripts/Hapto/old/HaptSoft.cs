using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Obi;

#if UNITY_EDITOR
[CustomEditor(typeof(HaptObject))]
#endif

public class HaptSoft : HaptObject
{
    protected bool firstFixed = true;
    protected ObiSoftbody softbody;
    protected SkinnedMeshRenderer rendererTarget;
    protected List<List<int>> ids;
    protected List<List<float>> weights;
    protected List<float> weightsVertex;

    protected Vector3[] verticesTarget;
    protected Vector3[] diffPrev;
    protected Mesh meshTarget;
    protected int numParticle;
    protected ObiSolver solver;
    protected float ratio = 0.001f;

    public GameObject skinnedMesh;
    public GameObject softMesh;
    public float coefficientK = 30f;
    public float coefficeintD = 30f;
    public int numParticleConnected = 4;
    public bool useWeightVertex = false;
    public float maxForce = 1e10f;
    public float ratioTouched = 1f;
    protected float ratioTouchedVal = 1f;


    protected override void Awake()
    {
        base.Awake();
        rendererTarget = skinnedMesh.GetComponent<SkinnedMeshRenderer>();
        softbody = softMesh.GetComponent<ObiSoftbody>();
        renderer = softMesh.GetComponent<SkinnedMeshRenderer>();
        meshTarget = new Mesh();
    }

    private void Start()
    {
        solver = softbody.solver;
    }

    public override void setObject(int _id, int maxNumPoint)
    {
        setWeights();
        base.setObject(_id, maxNumPoint);
    }


    void setWeights()
    {
        var meshTmp = softbody.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        numVertex = meshTmp.vertices.Length;
        numParticle = softbody.particleCount;

        weights = new List<List<float>>();
        ids = new List<List<int>>();
        diffPrev = new Vector3[numVertex];

        var verticesTmp = meshTmp.vertices;
        for (int i = 0; i < numVertex; i++)
        {
            Vector3 vert = softMesh.transform.TransformPoint(verticesTmp[i]);
            var list = new List<float>();
            for (int j = 0; j < numParticle; j++)
            {
                //Vector3 pos = solver.renderablePositions[softbody.GetParticleRuntimeIndex(j)];
                Vector3 pos = solver.transform.TransformPoint(solver.renderablePositions[softbody.GetParticleRuntimeIndex(j)]);
                float dist = (pos - vert).magnitude;
                list.Add(dist);
            }
            var sorted = list.Select((x, i) => new KeyValuePair<float, int>(x, i)).OrderBy(x => x.Key);

            List<float> tmpw = new List<float>();
            List<int> tmpi = new List<int>();
            float sum = 0;
            foreach (KeyValuePair<float, int> kv in sorted.Take(numParticleConnected))
            {
                tmpw.Add(1.0f / kv.Key);
                tmpi.Add(kv.Value);
                sum += 1.0f / kv.Key;
            }
            for (int j = 0; j < numParticleConnected; j++)
            {
                tmpw[j] /= sum;
            }
            weights.Add(tmpw);
            ids.Add(tmpi);
        }

        weightsVertex = new List<float>();
        triangles = meshTmp.triangles;
        numFace = triangles.Length / 3;
        List<float> areas = new List<float>();
        float sumArea = 0;
        for (int i = 0; i < numVertex; i++) areas.Add(0);
        for (int i = 0; i < numFace; i++)
        {
            var v0 = verticesTmp[triangles[3 * i]];
            var v1 = verticesTmp[triangles[3 * i + 1]];
            var v2 = verticesTmp[triangles[3 * i + 2]];
            float area = Vector3.Cross((v1 - v0), (v2 - v0)).magnitude / 2;
            areas[triangles[3 * i]] += area;
            areas[triangles[3 * i + 1]] += area;
            areas[triangles[3 * i + 2]] += area;
            sumArea += area;
            //Debug.Log(area);
        }

        float meanArea = sumArea * 3.0f / numVertex;
        for (int i = 0; i < numVertex; i++)
        {
            weightsVertex.Add(areas[i] / meanArea);
            //Debug.Log(i + ": " + weightsVertex[i]);
        }

    }

    public override void applyForce()
    {
        rendererTarget.BakeMesh(meshTarget);
        verticesTarget = meshTarget.vertices;
        sumDeformation = 0;

        if (GetComponent<Rigidbody>()) base.applyForce();

        Vector3 posP, posC;

        //float maxv = 0;
        if (numPointInObject > 0)
            ratioTouchedVal = ratioTouched;

        for (int i = 0; i < numVertex; i+=1)
        {
            posP = vertices[i];
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

            force *= ratioTouchedVal;

            if (force.magnitude > coefficientK * posDiff.magnitude * 10)
                force *= coefficientK * posDiff.magnitude * 10 / force.magnitude;

            if (force.magnitude > maxForce)
                force *= maxForce / force.magnitude;

            if (applyforce)
            {
                var ex = (Vector4)externalForce[i];
                if (ex.magnitude > 0)
                    force = (Vector4)externalForce[i];
            }

            for (int j = 0; j < numParticleConnected; j++)
            {
                solver.externalForces[softbody.GetParticleRuntimeIndex(ids[i][j])] += weights[i][j] * force;
            }
            diffPrev[i] = posDiff;

        }

        for (int i = 0; i < softbody.particleCount; i++)
        {
            solver.externalForces[softbody.GetParticleRuntimeIndex(i)] *= ratio;
        }

        ratio = ratio * 1.25f;
        if (ratio > 1) ratio = 1;
        //ratioTouchedVal *= 1.05f;
        //if (ratioTouchedVal > 1) ratioTouchedVal = 1;
    }


}
