using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System.Diagnostics;

[RequireComponent(typeof(MeshFilter))]
public class MeshBinder : MonoBehaviour
{
    [SerializeField] private MeshFilter followerMeshFilter;
    [SerializeField] private int numNeighbors = 4;

    private MeshFilter sourceMeshFilter;
    private Mesh followerMesh;

    private Vector3[] sourceRestVertices;
    private Vector3[] followerRestVertices;

    private List<int[]> targetVertexNeighbors;           // 各ターゲット頂点の近傍点インデックス
    private Matrix4x4[] targetVertexTransforms;          // 毎フレーム更新される変換行列

    private List<VertexBinding> followerBindings;

    void Start()
    {
        sourceMeshFilter = GetComponent<MeshFilter>();

        // Mesh準備
        sourceRestVertices = sourceMeshFilter.sharedMesh.vertices;
        followerRestVertices = followerMeshFilter.sharedMesh.vertices;
        followerMesh = Instantiate(followerMeshFilter.sharedMesh);
        followerMeshFilter.mesh = followerMesh;

        // 初期化
        InitTargetVertexNeighbors();
        InitFollowerBindings();
    }

    public void Bind()
    {
        Stopwatch sw = Stopwatch.StartNew();
        UpdateTargetVertexTransforms();
        UpdateFollowerVertices();
        sw.Stop();
        UnityEngine.Debug.Log("実行時間: " + sw.Elapsed.TotalMilliseconds + " ms");
    }

    // ターゲット各頂点に対する近傍を全探索で決定
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

    // 各追従頂点をターゲットの頂点にバインド（距離ベース）
    void InitFollowerBindings()
    {
        followerBindings = new List<VertexBinding>();

        for (int i = 0; i < followerRestVertices.Length; i++)
        {
            Vector3 followerPos = followerRestVertices[i];
            var distances = new List<(int index, float distSq)>();

            for (int j = 0; j < sourceRestVertices.Length; j++)
            {
                float d = (sourceRestVertices[j] - followerPos).sqrMagnitude;
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
                restPosition = followerRestVertices[i]
            });
        }
    }

    // 毎フレーム：ターゲット各頂点の座標変換を近傍点から推定
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

            // 相関行列 H
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

            // 反転補正
            if (Rm.Determinant() < 0)
            {
                for (int c = 0; c < 3; c++) Vt[2, c] *= -1;
                Rm = Vt.TransposeThisAndMultiply(U.Transpose());
            }

            Quaternion rot = MatrixToQuaternion(Rm);
            targetVertexTransforms[i] = Matrix4x4.TRS(centerDef, rot, Vector3.one) * Matrix4x4.Translate(-centerRest);
        }
    }

    // 各追従頂点をターゲットの変換で追従させる
    void UpdateFollowerVertices()
    {
        Vector3[] newVertices = new Vector3[followerRestVertices.Length];

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

        followerMesh.vertices = newVertices;
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

    private class VertexBinding
    {
        public int[] targetIndices;
        public float[] weights;
        public Vector3 restPosition;
    }
}
