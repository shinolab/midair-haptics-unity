using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KDTree;

public class MakeRandomDot: MonoBehaviour
{
    public int numA = 1000;
    public int numB = 100;
    public float radius = 10f;
    public float radiusGizmo = 0.1f;
    List<Vector3> A = new List<Vector3>();
    List<Vector3> A2 = new List<Vector3>();
    List<Vector3> B = new List<Vector3>();
    List<Vector3> C = new List<Vector3>();
    KDTree.Tree tree = null;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < numA; i++)
        {
            float r = Random.Range(0f, radius);
            float theta = Random.Range(0f, 2 * Mathf.PI);
            A.Add(new Vector3(r * Mathf.Sin(theta), 0, r * Mathf.Cos(theta)));
            A2.Add(new Vector3(A[i].x, A[i].z, 0));
        }

        for (int i = 0; i < numB; i++)
        {
            float r = Random.Range(0f, radius);
            float theta = Random.Range(0f, 2 * Mathf.PI);
            B.Add(new Vector3(r * Mathf.Sin(theta), 0, r * Mathf.Cos(theta)));
        }

        tree = new KDTree.Tree(A2);
        Node node = tree.root;

        for (int i = 0; i < numB; i++)
        {
            var tmp = tree.NearestNeighbor(new Vector2(B[i].x, B[i].z));
            C.Add(new Vector3(tmp.x, 0, tmp.y));
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < A.Count; i++)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(A[i], radiusGizmo);
        }

        for (int i = 0; i < B.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(B[i], radiusGizmo);
            Debug.DrawLine(B[i], C[i], Color.blue);
        }

        //if (tree != null)
        //    PrintTree();

    }
    public void PrintTree()
    {
        int count = 1;
        PrintNode(tree.root, ref count);
    }

    public void PrintNode(Node node, ref int num)
    {
        //Debug.Log(num + ": " + node.position);
        for (int i = 0; i < 2; i++)
        {
            if (node.next[i] != null)
            {
                num++;
                Debug.DrawLine(new Vector3(node.position.x, 0, node.position.y)
                	, new Vector3(node.next[i].position.x, 0, node.next[i].position.y));
                PrintNode(node.next[i], ref num);
            }
        }
    }
}
