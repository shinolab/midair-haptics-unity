using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEngine;
using static HandTracker;

[RequireComponent(typeof(MeshFilter))]
public class MeshBinderBone : MonoBehaviour
{
    public HandTracker handTracker;
    public bool updateMesh = true;
    [SerializeField] private MeshFilter followerMeshFilter;
    [SerializeField] private int numNeighbors = 4;

    private MeshRenderer followerMeshRenderer;
    private MeshFilter sourceMeshFilter;
    private Vector3[] sourceRestVertices;

    private List<int[]> targetVertexNeighbors;
    private Matrix4x4[] targetVertexTransforms;


    private Vector3[] vertexTarget;
    private Vector3[] vertexFollower;
    private float[] fvertexFollower;
    private int numVertexTarget = 0;
    private int numVertexFollower = 0;
    private HandTracker.Coeffs coeffsFollower = new HandTracker.Coeffs();
    private List<VertexBinding> bindings = new List<VertexBinding>();
    private Matrix4x4 transformTargetInitial;
    private Matrix4x4 transformFollowerInitial;


    private void Awake()
    {
        transformTargetInitial = float4x4.TRS(
            transform.position,
            transform.rotation,
            transform.localScale
        );

        transformFollowerInitial = float4x4.TRS(
            followerMeshFilter.transform.position,
            followerMeshFilter.transform.rotation,
            followerMeshFilter.transform.localScale
        );
    }
    public void InitBinder()
    {
        sourceMeshFilter = GetComponent<MeshFilter>();
        followerMeshRenderer = followerMeshFilter.GetComponent<MeshRenderer>();

        //followerRestVertices = followerMeshFilter.sharedMesh.vertices;

        numVertexTarget = handTracker.numVertex;
        numVertexFollower = followerMeshFilter.mesh.vertexCount;
        vertexTarget = new Vector3[numVertexTarget];
        vertexFollower = new Vector3[numVertexFollower];
        fvertexFollower = new float[numVertexFollower * 3];

        sourceRestVertices = new Vector3[numVertexTarget];
        for (int i = 0; i < numVertexTarget; i++)
        {
            sourceRestVertices[i] = transformTargetInitial.MultiplyPoint(sourceMeshFilter.sharedMesh.vertices[i]);
        }
        InitTargetVertexNeighbors();
        //MakeFollowerBindings();
        SetCoeffsFollower();
    }

    void SetCoeffsFollower()
    {
        float[] beta = new float[handTracker.numBeta];
        float[] pose = new float[handTracker.degFreeJoint];
        float[] trans = new float[3];
        Array.Copy(handTracker.beta, beta, beta.Length); //all 0
        Array.Copy(handTracker.pose, pose, pose.Length); //almost all 0
        Array.Copy(handTracker.trans, trans, trans.Length);

        handTracker.CalcVertexGlobal(handTracker.coeffs, vertexTarget, beta, pose, trans);
        UpdateTargetVertexTransforms(vertexTarget);


        //UnityEngine.Debug.Log(transformInitial);
        for (int i = 0; i < numVertexFollower; i++)
        {
            vertexFollower[i] = transformFollowerInitial.MultiplyPoint(followerMeshFilter.mesh.vertices[i]);
            //UnityEngine.Debug.Log(followerMeshFilter.mesh.vertices[i] + "        " + vertexFollower[i]);
        }
        MakeFollowerBindings(bindings, vertexTarget, vertexFollower);

        coeffsFollower.v_template = new DenseMatrix(numVertexFollower, 3);
        coeffsFollower.weights = new DenseMatrix(numVertexFollower, handTracker.numJoint);
        coeffsFollower.kintree_table = (int[,])handTracker.coeffs.kintree_table.Clone();
        coeffsFollower.posedirs = new DenseMatrix[handTracker.degElemJoint];
        coeffsFollower.shapedirs = new DenseMatrix[handTracker.numBeta];
        for (int i = 0; i < handTracker.degElemJoint; i++)
        {
            coeffsFollower.posedirs[i] = new DenseMatrix(numVertexFollower, 3);
        }
        for (int i = 0; i < handTracker.numBeta; i++)
        {
            coeffsFollower.shapedirs[i] = new DenseMatrix(numVertexFollower, 3);
        }

        //for (int i = 0; i < numVertexTarget; i++)
        //{
        //    Vector3 v = transformTargetInitial.inverse.MultiplyPoint(vertexTarget[i]);
        //    Vector3 v0 = new Vector3( -v.x, v.y, v.z);
        //    Vector3 v1 = new Vector3(handTracker.coeffs.v_template[i, 0], handTracker.coeffs.v_template[i, 1], handTracker.coeffs.v_template[i, 2]) * handTracker.scaleUnity;
        //    UnityEngine.Debug.DrawLine(Vector3.zero, v0, Color.blue, 10000000);
        //    UnityEngine.Debug.DrawLine(Vector3.zero, v1, Color.red, 10000000);

        //}

        for (int i = 0; i < numVertexFollower; i++)
        {
            Vector3 v = transformTargetInitial.inverse.MultiplyPoint(vertexFollower[i]);
            coeffsFollower.v_template[i, 0] = -v.x / handTracker.scaleUnity;
            coeffsFollower.v_template[i, 1] = v.y / handTracker.scaleUnity; ;
            coeffsFollower.v_template[i, 2] = v.z / handTracker.scaleUnity; ;
        }

        SetWeights(coeffsFollower.weights, handTracker.coeffs.weights, bindings);

        for (int i = 0; i < handTracker.degFreeJoint; i++) pose[i] = 0;
        for (int i = 0; i < 3; i++) trans[i] = 0;
        SetPosedirs(coeffsFollower.posedirs, beta, pose, trans);
        SetShapedirs(coeffsFollower.shapedirs, beta, pose, trans);

        float[] posedirs = new float[handTracker.degElemJoint * numVertexFollower * 3];
        float[] shapedirs = new float[handTracker.numBeta * numVertexFollower * 3];
        for (int i = 0; i < handTracker.degElemJoint; i++) {
            Array.Copy(coeffsFollower.posedirs[i].ToColumnMajorArray(), 0, posedirs, i * numVertexFollower * 3, numVertexFollower * 3);
        }
        for (int i = 0; i < handTracker.numBeta; i++)
        {
            Array.Copy(coeffsFollower.shapedirs[i].ToColumnMajorArray(), 0, shapedirs, i * numVertexFollower * 3, numVertexFollower * 3);
        }
        HandTracker.setEdirsHR(coeffsFollower.v_template.ToColumnMajorArray(),
            coeffsFollower.weights.ToColumnMajorArray(),
            posedirs, shapedirs, numVertexFollower);



        //UpdateFollowerVertices(vertexFollower, bindings);

        //followerMeshFilter.mesh.SetVertices(vertexFollower);
        //followerMeshFilter.mesh.RecalculateNormals();
        //followerMeshFilter.mesh.RecalculateBounds();

        //for (int i = 0; i < numVertexTarget; i++)
        //{
        //    UnityEngine.Debug.Log("matrix " + i + ": " + targetVertexTransforms[i]);
        //}

        //for (int i = 0; i < handTracker.numVertex; i++)
        //{
        //    UnityEngine.Debug.DrawLine(Vector3.zero, vertexTarget[i], Color.blue, 10000000);
        //}
        //for (int i = 0; i < numVertexFollower; i++)
        //{
        //    UnityEngine.Debug.DrawLine(Vector3.zero, vertexFollower[i], Color.red, 10000000);
        //    //UnityEngine.Debug.Log(vertexFollower[i]);
        //}
    }


    void SetWeights(DenseMatrix weightsFollower, DenseMatrix weightsTarget, List<VertexBinding> followerBindings)
    {
        for (int i = 0; i < numVertexFollower; i++)
        {
            var binding = followerBindings[i];

            for (int k = 0; k < handTracker.numJoint; k++)
            {
                float weight = 0;
                for (int j = 0; j < binding.targetIndices.Length; j++)
                {
                    int idx = binding.targetIndices[j];
                    float w = binding.weights[j];
                    weight += w * weightsTarget[idx, k];
                }
                weightsFollower[i, k] = weight;
            }
        }
    }

    void SetPosedirs(DenseMatrix[] posedirs, float[] beta, float[] pose, float[] trans)
    {
        float[] coeffPose = new float[handTracker.degElemJoint];
        for (int ipose = 0; ipose < handTracker.degElemJoint; ipose++)
        {
            coeffPose[ipose] = 1;
            handTracker.CalcVertexGlobalForShapedirs(handTracker.coeffs, vertexTarget, beta, pose, trans, coeffPose);
            UpdateTargetVertexTransforms(vertexTarget);
            UpdateFollowerVertices(vertexFollower, bindings);
            for (int i = 0; i < numVertexFollower; i++)
            {
                //Vector3 v = transformTargetInitial.inverse.MultiplyPoint(vertexFollower[i]);
                Vector3 v = vertexFollower[i];
                posedirs[ipose][i, 0] = -v.x / handTracker.scaleUnity;
                posedirs[ipose][i, 1] = v.y / handTracker.scaleUnity;
                posedirs[ipose][i, 2] = v.z / handTracker.scaleUnity;
                //UnityEngine.Debug.DrawLine(Vector3.zero, v, Color.blue, 10000000);
            }
            posedirs[ipose] = posedirs[ipose] - coeffsFollower.v_template;
            coeffPose[ipose] = 0;
        }
    }

    void SetShapedirs(DenseMatrix[] shapedirs, float[] beta, float[] pose, float[] trans)
    {
        //for (int ibeta = 0; ibeta < 1; ibeta++)
        for (int ibeta = 0; ibeta < handTracker.numBeta; ibeta++)
            {
            beta[ibeta] = 1;
            handTracker.CalcVertexGlobal(handTracker.coeffs, vertexTarget, beta, pose, trans);
            UpdateTargetVertexTransforms(vertexTarget);
            UpdateFollowerVertices(vertexFollower, bindings);
            for (int i = 0; i < numVertexFollower; i++)
            {
                //Vector3 v = transformTargetInitial.inverse.MultiplyPoint(vertexFollower[i]);
                Vector3 v = vertexFollower[i];
                shapedirs[ibeta][i, 0] = -v.x / handTracker.scaleUnity;
                shapedirs[ibeta][i, 1] = v.y / handTracker.scaleUnity;
                shapedirs[ibeta][i, 2] = v.z / handTracker.scaleUnity;
                //UnityEngine.Debug.DrawLine(Vector3.zero, v, Color.blue, 10000000);
            }
            shapedirs[ibeta] = shapedirs[ibeta] - coeffsFollower.v_template;
            beta[ibeta] = 0;
        }


        //for (int i = 0; i < handTracker.numVertex; i++)
        //{
        //    UnityEngine.Debug.DrawLine(Vector3.zero, vertexTarget[i], Color.blue, 10000000);
        //}
        //for (int i = 0; i < numVertexFollower; i++)
        //{
        //    UnityEngine.Debug.DrawLine(Vector3.zero, vertexFollower[i], Color.red, 10000000);
        //    //UnityEngine.Debug.Log(vertexFollower[i]);coeffsFollower.
        //}

        //for (int i = 0; i < handTracker.numVertex; i++)
        //{
        //    Vector3 offset = new Vector3(0, 1f, -1f);
        //    var v = new Vector3(handTracker.coeffs.v_template[i, 0], handTracker.coeffs.v_template[i, 1], handTracker.coeffs.v_template[i, 2]) * handTracker.scaleUnity;
        //    UnityEngine.Debug.DrawLine(Vector3.zero, v + offset, Color.white, 10000000);

        //    var vv = v + new Vector3(handTracker.coeffs.shapedirs[0][i, 0], handTracker.coeffs.shapedirs[0][i, 1], handTracker.coeffs.shapedirs[0][i, 2]) * handTracker.scaleUnity;
        //    UnityEngine.Debug.DrawLine(Vector3.zero, vv + offset, Color.yellow, 10000000);
        //}
        //for (int i = 0; i < numVertexFollower; i++)
        //{
        //    Vector3 offset = new Vector3(0, 1f, 1f);
        //    var v = new Vector3(coeffsFollower.v_template[i, 0], coeffsFollower.v_template[i, 1], coeffsFollower.v_template[i, 2]) * handTracker.scaleUnity;
        //    UnityEngine.Debug.DrawLine(Vector3.zero, v + offset, Color.red, 10000000);

        //    var vv = v + new Vector3(shapedirs[0][i, 0], shapedirs[0][i, 1], shapedirs[0][i, 2]) * handTracker.scaleUnity;
        //    UnityEngine.Debug.DrawLine(Vector3.zero, vv + offset, Color.blue, 10000000);
        //    //UnityEngine.Debug.Log(vertexFollower[i]);
        //}

    }
    
    public Vector3[] getVertex()
    {
        return vertexFollower;
    }

    public void Bind(bool prediction)
    {
        //handTracker.SetParameter();


        ////for (int i = 0; i < handTracker.numBeta; i++) handTracker.beta[i] = 0;
        ////for (int i = 3; i < handTracker.degFreeJoint; i++) handTracker.pose[i] = 0;
        //Stopwatch sw = Stopwatch.StartNew();
        //var v = new Vector3[numVertexTarget];
        //var rotMats = handTracker.CalcVertexGlobal(handTracker.coeffs, v, handTracker.beta, handTracker.pose, handTracker.trans);
        //handTracker.CalcVertexGlobal(coeffsFollower, rotMats, vertexFollower, handTracker.beta, handTracker.pose, handTracker.trans);
        //sw.Stop();
        //UnityEngine.Debug.Log("実行時間: " + sw.Elapsed.TotalMilliseconds + " ms");
        ////for (int i = 0; i < v.Length; i++)
        ////{
        ////    //UnityEngine.Debug.DrawLine(Vector3.zero, v[i], Color.red);
        ////    //Debug.Log(new Vector3(vertexBind[3 * i], vertexBind[3 * i + 1], vertexBind[3 * i + 2]));
        ////}

        if (prediction)
            HandTracker.getVertexPredictedHR(fvertexFollower);
        else
            HandTracker.getVertexHR(fvertexFollower);
        for (int i = 0; i < numVertexFollower; i++)
        {
            vertexFollower[i] = new Vector3(-fvertexFollower[3 * i], fvertexFollower[3 * i + 1], fvertexFollower[3 * i + 2]) * handTracker.scaleUnity;
            //UnityEngine.Debug.DrawLine(vertexFollower[i], Vector3.zero);
            //UnityEngine.Debug.Log(i + ": " + vertexFollower[i]);
        }

        if (updateMesh)
        {
            //if (followerMeshRenderer.enabled == false) followerMeshRenderer.enabled = true;
            followerMeshFilter.mesh.SetVertices(vertexFollower);
            followerMeshFilter.mesh.RecalculateNormals();
            followerMeshFilter.mesh.RecalculateBounds();
        }
        else
        {
            //if (followerMeshRenderer.enabled == true) followerMeshRenderer.enabled = false;
        }
     }


    public static Vector3[] MakeVertexNormal(Vector3[] vertices, int[] face)
    {
        int vertexCount = vertices.Length;
        Vector3[] normals = new Vector3[vertexCount];

        for (int i = 0; i < face.Length; i += 3)
        {
            int i0 = face[i];
            int i1 = face[i + 1];
            int i2 = face[i + 2];

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 faceNormal = Vector3.Cross(edge1, edge2).normalized;

            normals[i0] += faceNormal;
            normals[i1] += faceNormal;
            normals[i2] += faceNormal;
        }

        for (int i = 0; i < vertexCount; i++)
        {
            normals[i] = normals[i].normalized;
        }
        return normals;
    }

    void InitTargetVertexNeighbors()
    {
        int n = sourceRestVertices.Length;
        targetVertexNeighbors = new List<int[]>(n);

        for (int i = 0; i < n; i++)
        {
            Vector3 vi = sourceRestVertices[i];
            var distances = new List<(int index, float distSq)>();
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                float dist = (sourceRestVertices[j] - vi).sqrMagnitude;
                distances.Add((j, dist));
            }

            var nearest = distances.OrderBy(d => d.distSq).Take(numNeighbors).Select(x => x.index).ToArray();
            targetVertexNeighbors.Add(nearest);
        }
        targetVertexTransforms = new Matrix4x4[n];
    }

     void MakeFollowerBindings(List<VertexBinding> followerBindings, Vector3[] target, Vector3[] follower)
    {
        int[] trianglesFollower = followerMeshFilter.mesh.triangles;
        var normalsFollower = MakeVertexNormal(follower, trianglesFollower);
        int[] trianglesTarget = sourceMeshFilter.mesh.triangles;
        var normalsTarget = MakeVertexNormal(target, trianglesTarget);
        //for (int i = 0; i < normalsFollower.Length; i++)
        //{
        //    UnityEngine.Debug.DrawLine(follower[i], follower[i] + 0.2f * normalsFollower[i], Color.red, 10000000);
        //}
        //for (int i = 0; i < normalsTarget.Length; i++)
        //{
        //    UnityEngine.Debug.DrawLine(target[i], target[i] + 0.2f * normalsTarget[i], Color.blue ,10000000);
        //}

        float threshold = 0.2f;

        followerBindings.Clear();

        for (int i = 0; i < follower.Length; i++)
        {
            Vector3 followerPos = follower[i];
            Vector3 followerNormal = normalsFollower[i];
            var distances = new List<(int index, float distSq)>();

            for (int j = 0; j < target.Length; j++)
            {
                float d = (target[j] - followerPos).sqrMagnitude;
                if (Vector3.Dot(normalsTarget[j], followerNormal) > threshold) 
                    distances.Add((j, d));
            }

            var nearest = distances.OrderBy(x => x.distSq).Take(numNeighbors).ToArray();
            int[] indices = nearest.Select(x => x.index).ToArray();
            float[] weights = nearest.Select(x => 1f / (x.distSq + 1e-5f)).ToArray();
            float sum = weights.Sum();
            for (int k = 0; k < weights.Length; k++) weights[k] /= sum;

            followerBindings.Add(new VertexBinding
            {
                targetIndices = indices,
                weights = weights,
                restPosition = follower[i]
            });
        }
    }

    void UpdateTargetVertexTransforms(Vector3[] deformed)
    {
        //Vector3[] deformed = sourceMeshFilter.mesh.vertices;

        for (int i = 0; i < sourceRestVertices.Length; i++)
        {
            Vector3 centerRest = sourceRestVertices[i];
            Vector3 centerDef = deformed[i];

            int[] neighbors = targetVertexNeighbors[i];
            int n = neighbors.Length;

            if (n < 3)
            {
                targetVertexTransforms[i] = Matrix4x4.TRS(centerDef, Quaternion.identity, Vector3.one);
                continue;
            }

            var H = DenseMatrix.Create(3, 3, 0);
            for (int j = 0; j < n; j++)
            {
                Vector3 xr = sourceRestVertices[neighbors[j]] - centerRest;
                Vector3 yd = deformed[neighbors[j]] - centerDef;

                var xVec = DenseVector.OfArray(new float[] { xr.x, xr.y, xr.z });
                var yVec = DenseVector.OfArray(new float[] { yd.x, yd.y, yd.z });

                H += (DenseMatrix)(xVec.ToColumnMatrix() * yVec.ToRowMatrix());
            }

            var svd = H.Svd();
            var U = svd.U;
            var Vt = svd.VT;
            var Rm = Vt.TransposeThisAndMultiply(U.Transpose());

            if (Rm.Determinant() < 0)
            {
                for (int c = 0; c < 3; c++) Vt[2, c] *= -1;
                Rm = Vt.TransposeThisAndMultiply(U.Transpose());
            }

            Quaternion rot = MatrixToQuaternion(Rm);
            targetVertexTransforms[i] = Matrix4x4.TRS(centerDef, rot, Vector3.one) * Matrix4x4.Translate(-centerRest);
        }
    }

    void UpdateFollowerVertices(Vector3[] newVertices, List<VertexBinding> followerBindings)
    {
        //Vector3[] newVertices = new Vector3[followerRestVertices.Length];

        for (int i = 0; i < newVertices.Length; i++)
        {
            var binding = followerBindings[i];
            Vector3 vtx = Vector3.zero;

            for (int j = 0; j < binding.targetIndices.Length; j++)
            {
                int idx = binding.targetIndices[j];
                float w = binding.weights[j];
                vtx += targetVertexTransforms[idx].MultiplyPoint3x4(binding.restPosition) * w;
            }

            newVertices[i] = vtx;
        }

        //followerMesh.vertices = newVertices;
        //followerMesh.RecalculateNormals();
        //followerMesh.RecalculateBounds();
    }

    private Quaternion MatrixToQuaternion(Matrix<float> m)
    {
        Matrix4x4 mat = new Matrix4x4();
        mat.SetColumn(0, new Vector4(m[0, 0], m[1, 0], m[2, 0], 0));
        mat.SetColumn(1, new Vector4(m[0, 1], m[1, 1], m[2, 1], 0));
        mat.SetColumn(2, new Vector4(m[0, 2], m[1, 2], m[2, 2], 0));
        mat.SetColumn(3, new Vector4(0, 0, 0, 1));
        return mat.rotation;
    }

    private class VertexBinding
    {
        public int[] targetIndices;
        public float[] weights;
        public Vector3 restPosition;
    }
}
