using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System.Collections.Generic;
using UnityEngine;

public class FbvEllipse : HaptoFeedbackAUTD
{
    public struct Ellipse
    {
        public Vector3 center;
        public Vector3 axisA;
        public Vector3 axisB;
        public Vector3 axisC;
        public float amp;
    }

    public float ratioAmpFeedback = 2f;
    public float ratioSizeEllipse = 1.5f;

    public bool nearestNeighbor = false;
    protected List<KDTree.Tree> trees = new List<KDTree.Tree>();
    //protected Vector3 axisCprev;



    protected void ellipseRegression(ref List<Ellipse> ellipses, HaptoObject hapt)
    {
        for (int icluster = 0; icluster < hapt.numClusterIn; icluster++)
        {
            int start = hapt.indexStartCluster[icluster];
            int end = hapt.indexStartCluster[icluster + 1];
            int np = end - start;

            if (np < 3) continue;
            DenseMatrix X = new DenseMatrix(3, np);

            int index = start * 3;
            for (int i = 0; i < np; i++)
            {
                X[0, i] = hapt.pointInObject[index++];
                X[1, i] = hapt.pointInObject[index++];
                X[2, i] = hapt.pointInObject[index++];
            }

            var xmean = X.RowSums() / np;

            var Xbar = new DenseMatrix(3, np);
            for (int i = 0; i < np; i++)
            {
                Xbar.SetColumn(i, X.Column(i) - xmean);
            }

            var XX = Xbar * Xbar.Transpose();


            var evd = XX.Evd();

            var R = evd.EigenVectors;
            Vector3[] Ru = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Ru[i][j] = R[j, i];
                }
            }
            var e = evd.EigenValues;


            int ia, ib, ic;
            if (e[0].Real >= e[1].Real)
            {
                if (e[1].Real >= e[2].Real)
                {
                    ia = 0; ib = 1; ic = 2;
                }
                else
                {
                    if (e[0].Real >= e[2].Real)
                    {
                        ia = 0; ib = 2; ic = 1;
                    }
                    else
                    {
                        ia = 2; ib = 0; ic = 1;
                    }
                }
            }
            else
            {
                if (e[0].Real >= e[2].Real)
                {
                    ia = 1; ib = 0; ic = 2;
                }
                else
                {
                    if (e[1].Real >= e[2].Real)
                    {
                        ia = 1; ib = 2; ic = 0;
                    }
                    else
                    {
                        ia = 2; ib = 1; ic = 0;
                    }
                }
            }
            //
            float scaleEllipse = ratioSizeEllipse;
            Vector3 vecA = Ru[ia].normalized * Mathf.Sqrt((float)(e[ia].Real) / np) * scaleEllipse;
            Vector3 vecB = Ru[ib].normalized * Mathf.Sqrt((float)(e[ib].Real) / np) * scaleEllipse;
            Vector3 vecC = Vector3.Cross(vecA, vecB).normalized;


            Ellipse ellipse;
            var force = new Vector3(hapt.sumForce[3 * icluster], hapt.sumForce[3 * icluster + 1], hapt.sumForce[3 * icluster + 2]);

            ellipse.center = new Vector3(xmean[0], xmean[1], xmean[2]);// * scaleUnity;
            ellipse.axisA = vecA;
            ellipse.axisB = vecB;
            if (Vector3.Dot(force, vecC) > 0)
                ellipse.axisC = vecC;
            else
            {
                ellipse.axisC = -vecC;
                ellipse.axisA = -vecA;
            }

            ellipse.amp = Mathf.Sqrt(force.magnitude * ratioAmpFeedback);
            ellipses.Add(ellipse);
            Debug.DrawLine(ellipse.center, ellipse.center + vecC);

            //Debug.DrawLine(ellipse.center, ellipse.center + force * 10, Color.green);
            //Debug.DrawLine(ellipse.center, ellipse.center + ellipse.axisC, Color.green);
            //Debug.DrawLine(ellipse.center, ellipse.center + ellipse.axisA.normalized, Color.red);
            //Debug.DrawLine(ellipse.center, ellipse.center + ellipse.axisB.normalized, Color.blue);
            //Debug.DrawLine(ellipse.center, ellipse.center + Vector3.Cross(ellipse.axisA.normalized,ellipse.axisB.normalized), Color.green);
            //int num = 50;
            //for (int i = 0; i < num; i++)
            //{
            //    float angle = 2 * Mathf.PI / num * (float)i;
            //    Vector3 pos = ellipse.center + Mathf.Sin(angle) * ellipse.axisA + Mathf.Cos(angle) * ellipse.axisB;

            //    float angle1 = 2 * Mathf.PI / num * (float)(i + 1);
            //    Vector3 pos1 = ellipse.center + Mathf.Sin(angle1) * ellipse.axisA + Mathf.Cos(angle1) * ellipse.axisB;
            //    UnityEngine.Debug.DrawLine(pos, pos1);
            //}

            if (nearestNeighbor)
            {
                var a = R.Column(ia).Normalize(2);
                var b = R.Column(ib).Normalize(2);
                var c = Vector<float>.Build.DenseOfArray(new float[] { vecC.x, vecC.y, vecC.z });
                var A = Xbar.Transpose() * a;
                var B = Xbar.Transpose() * b;
                var C = Xbar.Transpose() * c;
                var tmp2 = new List<Vector3>();
                if (Vector3.Dot(force, vecC) > 0)
                {
                    for (int i = 0; i < np; i++)
                    {
                        tmp2.Add(new Vector3(A[i], B[i], C[i]));
                    }
                }
                else
                {
                    for (int i = 0; i < np; i++)
                    {
                        tmp2.Add(new Vector3(-A[i], B[i], -C[i]));
                    }
                }
                 trees.Add(new KDTree.Tree(tmp2));
            }
        }
    }



    public override bool Feedback(ref HaptoObject haptObject, float[] point, int numPoint)
    {
        return false;
    }

    public override void Feedback(ref List<HaptoObject> haptObjects, float[] point, int numPoint)
    {
    }

}
