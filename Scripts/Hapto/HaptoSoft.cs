using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Obi;

public class HaptoSoft : HaptoObject
{
    bool firstFixed = true;
    ObiSoftbody softbody;
    List<List<int>> ids;
    List<List<float>> weights;
    List<float> weightsVertex;
    int[] idsH;
    float[] weightsH;
    float[] weightsVertexH;
    float[] particleForceF;
    [System.NonSerialized] public SkinnedMeshRenderer rendererTarget;

    Vector3[] verticesTarget;
    Vector3[] diffPrev;
    Mesh meshTarget;
    int numParticle;
    ObiSolver solver;
    float ratio = 1f;

    public bool applyForceToSoftbody = true;
    public GameObject skinnedMesh;
    public GameObject softMesh;
    public float coefficientK = 30f;
    public float coefficientD = 30f;
    public int numParticleConnected = 4;
    public bool useWeightVertex = false;
    public float coeffPointForce = 1f;
    public float maxForce = 1e10f;
    public float ratioTouched = 1f;
    public float pow = 0f;
    protected float ratioTouchedVal = 1f;
    protected float[] transformTarget;

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

    public override void SetObject(int _id, int maxNumPoint, int maxNumCluster)
    {
        SetWeights();
        base.SetObject(_id, maxNumPoint, maxNumCluster);
    }

    public override void SendObject()
    {
        //base.SendObject();
        float[] _centerBound = new float[3];
        for (int i = 0; i < 3; i++) _centerBound[i] = centerBound[i];
        CollisionDetector.setObjectParticle(numVertex, numFaceCollision, trianglesCollision, radiusBound,
            _centerBound, weightsVertexH, weightsH, idsH, numParticle, numParticleConnected);
    }

    void SetWeights()
    {
        var meshTmp = softbody.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        numVertex = meshTmp.vertices.Length;
        numParticle = softbody.particleCount;
        transformTarget = new float[12];

        weights = new List<List<float>>();
        ids = new List<List<int>>();
        diffPrev = new Vector3[numVertex];

        weightsVertexH = new float[numVertex];
        weightsH = new float[numVertex * numParticleConnected];
        idsH = new int[numVertex * numParticleConnected];

        particleForceF = new float[3 * numParticle];


        var verticesTmp = meshTmp.vertices;
        for (int i = 0; i < numVertex; i++)
        {
            Vector3 vert = softMesh.transform.TransformPoint(verticesTmp[i]);
            var list = new List<float>();
            for (int j = 0; j < numParticle; j++)
            {
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
                               //Debug.Log(i + ": " +  kv.Key);
            }
            for (int j = 0; j < numParticleConnected; j++)
            {
                tmpw[j] /= sum;
            }
            weights.Add(tmpw);
            ids.Add(tmpi);

            for (int j = 0; j < numParticleConnected; j++)
            {
                idsH[i * numParticleConnected + j] = tmpi[j];
                weightsH[i * numParticleConnected + j] = tmpw[j];
            }
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
        }

        float meanArea = sumArea * 3.0f / numVertex;
        for (int i = 0; i < numVertex; i++)
        {
            weightsVertex.Add(areas[i] / meanArea);
            weightsVertexH[i] = areas[i] / meanArea;
            //Debug.Log(i + ": " + weightsVertex[i]);
        }

    }

    public override void ApplyForce()
    {
        rendererTarget.BakeMesh(meshTarget, true);
        verticesTarget = meshTarget.vertices;
        sumDeformation = 0;


        if (GetComponent<Rigidbody>()) base.ApplyForce();

        Vector3 posP, posC;
        //float maxv = 0;
        if (numPointInObject > 0)
            ratioTouchedVal = ratioTouched;

        for (int i = 0; i < numVertex; i+=1)
        {
            //posP = vertices[i];
            posP = renderer.transform.TransformPoint(vertices[i]);

            posC = skinnedMesh.transform.TransformPoint(verticesTarget[i]);
            //Debug.DrawLine(posP, posC);
            //Debug.DrawLine(Vector3.zero, posC);

            Vector3 posDiff = posC - posP;
            Vector3 velocityDiff = (posDiff - diffPrev[i]) / Time.fixedDeltaTime;

            if (firstFixed)
            {
                velocityDiff = new Vector3(0, 0, 0);
                firstFixed = false;
            }
            Vector4 force = (coefficientK * posDiff + coefficientD * velocityDiff);
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

            if (force.magnitude > maxForce)
                force *= maxForce / force.magnitude;

            if (detection)
            {
                var ex = (Vector4)externalForce[i];
                if (ex.magnitude > 0)
                {
                    ratioTouchedVal = ratioTouched;
                    force *= ratioTouchedVal;
                    force += (Vector4)externalForce[i] * coeffPointForce;
                }
            }

            for (int j = 0; j < numParticleConnected; j++)
            {
                //solver.externalForces[softbody.GetParticleRuntimeIndex(ids[i][j])] += weights[i][j] * force;
                solver.externalForces[softbody.GetParticleRuntimeIndex(ids[i][j])] += weights[i][j] * (Vector4)solver.transform.InverseTransformDirection(force);

                //var p = solver.renderablePositions[softbody.GetParticleRuntimeIndex(ids[i][j])];
                //Debug.DrawLine(posP, p);
            }
            diffPrev[i] = posDiff;

        }

        for (int i = 0; i < softbody.particleCount; i++)
        {
            solver.externalForces[softbody.GetParticleRuntimeIndex(i)] *= ratio;
        }

        ratio = ratio * 1.55f;
        if (ratio > 1) ratio = 1;
        ratioTouchedVal *= 1.05f;
        if (ratioTouchedVal > 1) ratioTouchedVal = 1;

        //for (int j = 0; j < numParticle; j++)
        //{
        //    Vector3 pos = solver.renderablePositions[softbody.GetParticleRuntimeIndex(j)];
        //    Debug.DrawLine(Vector3.zero, pos);
        //}
    }

    public override void ApplyForceGPU(float coeffPointForceBase)
    {
        //DrawLineBetweenVertexAndParticle();
        //return;

        rendererTarget.BakeMesh(meshTarget, true);
        verticesTarget = meshTarget.vertices;
        for (int j = 0; j < numVertex; j++)
        {
            //verticesTarget[j] = rendererTarget.transform.TransformPoint(verticesTarget[j]);
            verticesTargetF[3 * j] = verticesTarget[j].x;
            verticesTargetF[3 * j + 1] = verticesTarget[j].y;
            verticesTargetF[3 * j + 2] = verticesTarget[j].z;
        }

        var mat = skinnedMesh.transform.localToWorldMatrix;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                transformTarget[r * 4 + c] = mat[r, c];
            }
        }

        if (numPointInObject > 0)
            ratioTouchedVal = ratioTouched;

        int maxNum = 1;
        if (maxNum < numPointInObject) maxNum = numPointInObject;

        //for (int i = 0; i < numClusterIn; i++)
        //{
        //    //Debug.Log(i + ": " + indexStartCluster[i + 1]);
        //    if (maxNum < indexStartCluster[i + 1]) maxNum = indexStartCluster[i+1];
        //}


        CollisionDetector.sendVerticesTarget(id, verticesTargetF);
        CollisionDetector.sendTransformRigidBody(id, transformTarget);
        CollisionDetector.transformVerticesTarget(id);

        float ratioExternalForce = coeffPointForceBase * coeffPointForce / Mathf.Pow(maxNum, pow);
        if (!applyForceToSoftbody) ratioExternalForce = 0;
        CollisionDetector.getParticleForce(id, particleForceF, ratioTouchedVal, ratioExternalForce, coefficientK, coefficientD, maxForce, Time.fixedDeltaTime);



        for (int i = 0; i < softbody.particleCount; i++)
        {
            var force = new Vector4(particleForceF[3 * i], particleForceF[3 * i + 1], particleForceF[3 * i + 2], 0) * ratio;
            //solver.externalForces[softbody.GetParticleRuntimeIndex(i)] += force;
            solver.externalForces[softbody.GetParticleRuntimeIndex(i)] += (Vector4)solver.transform.InverseTransformDirection(force);
            //Debug.DrawLine(solver.renderablePositions[softbody.GetParticleRuntimeIndex(i)], new Vector3(0, 0, 0));
        }


        ratio = ratio * 1.25f;
        if (ratio > 1) ratio = 1;
        ratioTouchedVal *= 1.05f;
        if (ratioTouchedVal > 1) ratioTouchedVal = 1;

        base.ApplyForceGPU(coeffPointForceBase * coeffPointForce / Mathf.Pow(maxNum, pow));

    }

    public void DrawLineBetweenVertexAndParticle()
    {
        //rendererTarget.BakeMesh(meshTarget);
        for (int i = 0; i < numVertex; i += 1)
        {
            var posP = vertices[i];
            for (int j = 0; j < numParticleConnected; j++)
            {

                var p = solver.transform.TransformPoint(solver.renderablePositions[softbody.GetParticleRuntimeIndex(ids[i][j])]);
                //Debug.DrawLine(softMesh.transform.TransformPoint(posP), p);
                //Debug.DrawLine(p, p+new Vector3(0,0.1f,0));
            }

        }
     }


}
