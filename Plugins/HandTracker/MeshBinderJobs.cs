using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using System.Diagnostics;

[RequireComponent(typeof(MeshFilter))]
public class MeshBinderJobs : MonoBehaviour
{
    [SerializeField] private MeshFilter followerMeshFilter;
    [SerializeField] private int numNeighbors = 4;

    private MeshFilter sourceMeshFilter;
    private Mesh followerMesh;

    private Vector3[] sourceRestVertices;
    private Vector3[] followerRestVertices;

    private List<int[]> targetVertexNeighbors;
    private Matrix4x4[] targetVertexTransforms;

    private NativeArray<VertexBinding> nativeBindings;
    private NativeArray<Matrix4x4> nativeTargetTransforms;
    private NativeArray<Vector3> nativeOutput;

    void Start()
    {
        sourceMeshFilter = GetComponent<MeshFilter>();
        sourceRestVertices = sourceMeshFilter.sharedMesh.vertices;
        followerRestVertices = followerMeshFilter.sharedMesh.vertices;

        followerMesh = Instantiate(followerMeshFilter.sharedMesh);
        followerMeshFilter.mesh = followerMesh;

        InitTargetVertexNeighbors();
        InitFollowerBindings();

        nativeTargetTransforms = new NativeArray<Matrix4x4>(sourceRestVertices.Length, Allocator.Persistent);
        nativeOutput = new NativeArray<Vector3>(followerRestVertices.Length, Allocator.Persistent);
    }

    void OnDisable()
    {
        if (nativeBindings.IsCreated) nativeBindings.Dispose();
        if (nativeTargetTransforms.IsCreated) nativeTargetTransforms.Dispose();
        if (nativeOutput.IsCreated) nativeOutput.Dispose();
    }

    public void Bind()
    {
        Stopwatch sw = Stopwatch.StartNew();
        UpdateTargetVertexTransforms();
        sw.Stop();
        UnityEngine.Debug.Log("é¿çséûä‘1: " + sw.Elapsed.TotalMilliseconds + " ms");

        sw = Stopwatch.StartNew();
        nativeTargetTransforms.CopyFrom(targetVertexTransforms);
        UpdateFollowerVertices_Job();
        sw.Stop();
        UnityEngine.Debug.Log("é¿çséûä‘2: " + sw.Elapsed.TotalMilliseconds + " ms");
    }

    void InitTargetVertexNeighbors()
    {
        int n = sourceRestVertices.Length;
        targetVertexNeighbors = new List<int[]>(n);

        for (int i = 0; i < n; i++)
        {
            Vector3 vi = sourceRestVertices[i];
            var distances = new List<(int, float)>();
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                float dist = (sourceRestVertices[j] - vi).sqrMagnitude;
                distances.Add((j, dist));
            }
            var nearest = distances.OrderBy(d => d.Item2).Take(numNeighbors).Select(x => x.Item1).ToArray();
            targetVertexNeighbors.Add(nearest);
        }
        targetVertexTransforms = new Matrix4x4[n];
    }

    void InitFollowerBindings()
    {
        nativeBindings = new NativeArray<VertexBinding>(followerRestVertices.Length, Allocator.Persistent);

        for (int i = 0; i < followerRestVertices.Length; i++)
        {
            Vector3 followerPos = followerRestVertices[i];
            var distances = new List<(int, float)>();

            for (int j = 0; j < sourceRestVertices.Length; j++)
            {
                float d = (sourceRestVertices[j] - followerPos).sqrMagnitude;
                distances.Add((j, d));
            }

            var nearest = distances.OrderBy(x => x.Item2).Take(4).ToArray();
            int[] indices = nearest.Select(x => x.Item1).ToArray();
            float[] weights = nearest.Select(x => 1f / (x.Item2 + 1e-5f)).ToArray();
            float sum = weights.Sum();
            for (int k = 0; k < weights.Length; k++) weights[k] /= sum;

            nativeBindings[i] = new VertexBinding
            {
                index0 = indices[0],
                index1 = indices[1],
                index2 = indices[2],
                index3 = indices[3],
                weight0 = weights[0],
                weight1 = weights[1],
                weight2 = weights[2],
                weight3 = weights[3],
                restPosition = followerRestVertices[i]
            };
        }
    }

    void UpdateTargetVertexTransforms()
    {
        Vector3[] deformed = sourceMeshFilter.mesh.vertices;
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

    void UpdateFollowerVertices_Job()
    {
        var job = new FollowerUpdateJob
        {
            bindings = nativeBindings,
            targetTransforms = nativeTargetTransforms,
            newVertices = nativeOutput
        };

        var handle = job.Schedule(nativeBindings.Length, 64);
        handle.Complete();

        followerMesh.vertices = nativeOutput.ToArray();
        followerMesh.RecalculateNormals();
        followerMesh.RecalculateBounds();
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

    [BurstCompile]
    private struct FollowerUpdateJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<VertexBinding> bindings;
        [ReadOnly] public NativeArray<Matrix4x4> targetTransforms;
        [WriteOnly] public NativeArray<Vector3> newVertices;

        public void Execute(int index)
        {
            var b = bindings[index];
            var p = b.restPosition;
            Vector3 result =
                targetTransforms[b.index0].MultiplyPoint3x4(p) * b.weight0 +
                targetTransforms[b.index1].MultiplyPoint3x4(p) * b.weight1 +
                targetTransforms[b.index2].MultiplyPoint3x4(p) * b.weight2 +
                targetTransforms[b.index3].MultiplyPoint3x4(p) * b.weight3;
            newVertices[index] = result;
        }
    }

    [BurstCompile]
    public struct VertexBinding
    {
        public int index0, index1, index2, index3;
        public float weight0, weight1, weight2, weight3;
        public float3 restPosition;
    }
}
