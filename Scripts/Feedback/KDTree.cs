using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace KDTree
{

	public class Node
	{
		public Vector3 position;
		public int axis;
		public Node[] next = new Node[2];
	}

	class CompX : IComparer<Vector3>
	{
		public int Compare(Vector3 a, Vector3 b)
		{
			return a.x.CompareTo(b.x);
		}
	}

	class CompY : IComparer<Vector3>
	{
		public int Compare(Vector3 a, Vector3 b)
		{
			return a.y.CompareTo(b.y);
		}
	}

	public class Tree
	{
		CompX compX = new CompX();
		CompY compY = new CompY();
		public int size = 0;
		public Node root = null;

		public Tree() { }

		public Tree(List<Vector3> list)
		{
			MakeTree(list);
		}

		public void MakeTree(List<Vector3> list)
        {
			size = list.Count;
			root = BuildRecursive(list, 0, size - 1, 0);
		}

		Node BuildRecursive(List<Vector3> list, int l, int r, int depth)
		{
			if (r < l)
				return null;

			int axis = depth % 2;
			int mid = (l + r) / 2;

			//Debug.Log(l + ", " + r);
			if (axis == 0)
				list.Sort(l, r - l + 1, compX);
			else
				list.Sort(l, r - l + 1, compY);

			Node node = new Node();
			node.position = list[mid];
			node.axis = axis;

			node.next[0] = BuildRecursive(list, l, mid - 1, depth + 1);
            node.next[1] = BuildRecursive(list, mid + 1, r, depth + 1);

            return node;
		}

		public Vector3 NearestNeighbor(Vector2 point)
		{
			Vector3 minPos = new Vector3();
			float minDist = float.MaxValue;
			SearchRecursive(point, root, ref minPos, ref minDist);
			return minPos;
		}




		void SearchRecursive(Vector2 point, Node node, ref Vector3 minPos, ref float minDist)
		{
			if (node == null)
				return;

			var train = node.position;

            //float dist = Vector3.Distance(point, train);
            float dist = Vector2.Distance(point, (Vector2)train);
            if (dist < minDist)
			{
				minDist = dist;
				minPos = train;
			}

			int axis = node.axis;
			int dir = point[axis] < train[axis] ? 0 : 1;

			SearchRecursive(point, node.next[dir], ref minPos, ref minDist);

            float diff = Mathf.Abs(point[axis] - train[axis]);
            if (diff < minDist)
                SearchRecursive(point, node.next[1 - dir], ref minPos, ref minDist);
        }
	}
}
