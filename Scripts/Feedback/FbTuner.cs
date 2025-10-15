using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;
using UnityEditor;
public class FbTuner : FbvEllipse
{

    public float frequencySTM = 5f;
    public float frequencyAM1 = 30f;
    public float frequencyAM2 = 100f;
    public bool dynamicRadius = false;
    public float radiusSTM = 0.05f;
    public int numPointSTM = 50;
    public GameObject cube;
    public int numSamplingAM = 4000;

    [SerializeField, Range(0.0f, 1.0f)]
    public float peakWaveform = 1f;
    [SerializeField, Range(0.0f, 1.0f)]
    public float ratioVibration = 0f;
    [SerializeField, Range(0.0f, 1.0f)]
    public float ratioAM1 = 1f;

    bool updated = false;
    protected List<Ellipse> bufEllipses = new List<Ellipse>();
    protected List<KDTree.Tree> bufTrees = new List<KDTree.Tree>();
    protected System.Random rand = new System.Random();
    protected bool touched = false;



    private void Start()
    {
        sendAM();
    }

    private void sendAM()
    {
        var buf = new byte[numSamplingAM];
        for (int i = 0; i < numSamplingAM; i++)
        {
            float t = (float)i / numSamplingAM;
            var a = peakWaveform * (1f - ratioVibration / 2);
            var b = peakWaveform * ratioVibration/2 * ratioAM1 *  Mathf.Sin(2 * Mathf.PI * t * frequencyAM1);
            var c = peakWaveform * ratioVibration / 2 * (1 - ratioAM1) * Mathf.Sin(2 * Mathf.PI * t * frequencyAM2);
            buf[i] = (byte)((a + b + c) * 255);
        }
        AUTDController.instance.AMCustom(buf, (uint)numSamplingAM);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (EditorApplication.isPlaying && AUTDController.instance != null && AUTDController.instance.isopen)
        {
            updated = true;
        }
    }
#endif



    protected void getEllipseFromCube(ref List<Ellipse> ellipses, HaptoObject hapt)
    {
        int maxNp = 0;
        int iMaxNum = 0;
        for (int icluster = 0; icluster < hapt.numClusterIn; icluster++)
        {
            int _start = hapt.indexStartCluster[icluster];
            int _end = hapt.indexStartCluster[icluster + 1];
            int _np = _end - _start;
            if (_np > maxNp)
            {
                maxNp = _np;
                iMaxNum = icluster;
            }
        }

        int start = hapt.indexStartCluster[iMaxNum];
        int end = hapt.indexStartCluster[iMaxNum + 1];
        int np = end - start;

        if (np < 3) return;
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

        var axisA = cube.transform.TransformVector(new Vector3(1, 0, 0));
        var axisB = cube.transform.TransformVector(new Vector3(0, 0, 1));
        var axisC = cube.transform.TransformVector(new Vector3(0, -1, 0));

        Ellipse ellipse;
        var force = new Vector3(hapt.sumForce[3 * iMaxNum], hapt.sumForce[3 * iMaxNum + 1], hapt.sumForce[3 * iMaxNum + 2]);

        var vecA = new DenseMatrix(1, 3);
        var vecB = new DenseMatrix(1, 3);
        var vecC = new DenseMatrix(1, 3);
        for (int i = 0; i < 3; i++)
        {
            vecA[0, i] = axisA[i];
            vecB[0, i] = axisB[i];
            vecC[0, i] = axisC[i];
        }
        var Xa = vecA * Xbar;
        var Xb = vecB * Xbar;
        var Xc = vecC * Xbar;

        var a = Mathf.Sqrt((Xa * Xa.Transpose())[0, 0] / np);
        var b = Mathf.Sqrt((Xb * Xb.Transpose())[0, 0] / np);


        if (nearestNeighbor)
        {
            var tmp2 = new List<Vector3>();
            for (int i = 0; i < np; i++)
            {
                tmp2.Add(new Vector3(Xa[0, i], Xb[0, i], Xc[0, i]));
            }
            trees.Add(new KDTree.Tree(tmp2));
        }

        ellipse.center = new Vector3(xmean[0], xmean[1], xmean[2]);// * scaleUnity;
        ellipse.axisA = axisA;
        ellipse.axisB = axisB;
        ellipse.axisC = axisC;
        ellipse.amp = Mathf.Sqrt(force.magnitude * ratioAmpFeedback);
        if (!dynamicRadius)
        {
            ellipses.Add(ellipse);
            return;
        }
        ellipse.axisA *= a;
        ellipse.axisB *= b;
        ellipses.Add(ellipse);
        //Debug.DrawLine(ellipse.center, ellipse.center + ellipse.axisC);
    }

    public override void Feedback(ref List<HaptoObject> haptObjects, float[] point, int numPoint)
    {
        setSilencer();
        List<Ellipse> ellipses = new List<Ellipse>();
        foreach (var hapt in haptObjects)
        {
            if (hapt.numPointInObject > 0)
            {
                getEllipseFromCube(ref ellipses, hapt);
            }
        }

        if (ellipses.Count > 0)
        {
            stmEllipse(ref ellipses, trees);
            touched = true;
        }
        else
        {
            if (touched)
            {
                AUTDController.instance.Null();
                touched = false;
            }
        }
    }

    protected void getPointsEllipse(ref List<Vector3> points, ref List<Ellipse> ellipses)
    {
        foreach (var ellipse in ellipses)
        {
            int num = numPointSTM / ellipses.Count;
            for (int i = 0; i < num; i++)
            {
                if (dynamicRadius)
                {
                    float angle = 2 * Mathf.PI / num * (float)i;
                    Vector3 pos = ellipse.center + Mathf.Sin(angle) * ellipse.axisA + Mathf.Cos(angle) * ellipse.axisB;
                    points.Add(pos);
                    float angle1 = 2 * Mathf.PI / num * (float)(i + 1);
                    Vector3 pos1 = ellipse.center + Mathf.Sin(angle1) * ellipse.axisA + Mathf.Cos(angle1) * ellipse.axisB;
                    UnityEngine.Debug.DrawLine(pos, pos1);
                }
                else
                {
                    float angle = 2 * Mathf.PI / num * (float)i;
                    Vector3 pos = ellipse.center + radiusSTM * Mathf.Sin(angle) * ellipse.axisA.normalized + radiusSTM * Mathf.Cos(angle) * ellipse.axisB.normalized;
                    points.Add(pos);

                    float angle1 = 2 * Mathf.PI / num * (float)(i + 1);
                    Vector3 pos1 = ellipse.center + radiusSTM * Mathf.Sin(angle) * ellipse.axisA.normalized + radiusSTM * Mathf.Cos(angle) * ellipse.axisB.normalized;
                    UnityEngine.Debug.DrawLine(pos, pos1);
                }
            }
        }
    }

    public void getPointsOnSurface(ref List<Vector3> points, List<KDTree.Tree> _trees, List<Ellipse> ellipses)
    {
        for (int j = 0; j < ellipses.Count; j++)
        {
            var ellipse = ellipses[j];
            int num = numPointSTM / ellipses.Count;
            float a, b;
            if (dynamicRadius)
            {
                a = ellipse.axisA.magnitude;
                b = ellipse.axisB.magnitude;
            }
            else{
                a = radiusSTM;
                b = radiusSTM;
            }
            Vector3 x = ellipse.axisA.normalized;
            Vector3 y = ellipse.axisB.normalized;
            Vector3 z = ellipse.axisC.normalized;

            for (int i = 0; i < num; i++)
            {
                float angle = 2 * Mathf.PI / num * (float)i;
                Vector2 point = new Vector2(a * Mathf.Sin(angle), b * Mathf.Cos(angle));
                var nn = _trees[j].NearestNeighbor(point);
                //Vector3 pos = ellipse.center + nn.x * x + nn.y * y + nn.z * z;
                Vector3 pos = ellipse.center + point.x * x + point.y * y + nn.z * z;
                points.Add(pos);
                if (i > 0 && j == 0)
                    UnityEngine.Debug.DrawLine(points[i - 1], pos);
            }
        }
    }

    protected void stmEllipse(ref List<Ellipse> ellipses, List<KDTree.Tree> _trees)
    {
        List<Vector3> points = new List<Vector3>();
        if (nearestNeighbor)
        {
            getPointsOnSurface(ref points, _trees, ellipses);
        }
        else
        {
            getPointsEllipse(ref points, ref ellipses);
        }


        AUTDController.instance.FocusSTM(points, frequencySTM, 0);
        if (updated)
        {
            updated = false;
            sendAM();
        }

    }
}


