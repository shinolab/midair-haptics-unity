//using AUTD3Sharp;
//using System;
//using System.Text;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using UnityEngine;
////using System.Numerics;
////using MathNet.Numerics.LinearAlgebra.Single;
//using MathNet.Numerics;
//using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.LinearAlgebra.Complex32;

//public class GainGspatSel : AUTD3Sharp.Gain.Gain
//{
//	Dictionary<int, Drive[]> data;
//	float[] dir_coef_a;
//	float[] dir_coef_b;
//	float[] dir_coef_c;
//	float[] dir_coef_d;

//	DenseMatrix makeMatrixG(List<Vector3> points, Geometry geometry)
//	{
//		float k = geometry[0][0].Wavenumber(geometry[0].SoundSpeed);
//		float alpha = geometry[0].Attenuation;
//		int np = points.Count;
//		int nq = geometry.NumTransducers;
//		DenseMatrix G = new DenseMatrix(np, nq);

//		for (int row = 0; row < np; row++)
//		{
//			Vector3 pos = points[row];
//			int col = 0;
//			for (int i = 0; i < geometry.NumDevices; i++)
//			{
//				for (int j = 0; j < geometry[i].NumTransducers; j++)
//				{
//					var tp = geometry[i][j].Position;
//					var tn = geometry[i][j].ZDirection;
//					var dist = tp - pos;
//					var d = Vector3.Magnitude(dist);
//					var theta = Mathf.Acos(Vector3.Dot(tn, dist) / d);
//					int itheta = Mathf.FloorToInt(Mathf.Abs(theta / Mathf.PI * 180) / 10);
//					if (itheta > 8) itheta = 8;
//					float ca = dir_coef_a[itheta];
//					float cb = dir_coef_b[itheta];
//					float cc = dir_coef_c[itheta];
//					float cd = dir_coef_d[itheta];
//					float x = theta - i * 10;
//					float directivity = ca + cb * x + cc * x * x + cd * x * x * x;
//					G[row,col] = directivity * Mathf.Exp(-alpha * d) * Complex32.Exp(- new Complex32(0, 1) * k * d) / (float)(4 * Mathf.PI * d);
//					col++;
//				}
//			}
//		}
//		return G;
//	}

//	//Vector GSPAT(DenseMatrix G, Vector pAmp, int nItr)
//	//{
//	//	//int np = G.rows();
//	//	//int nq = G.cols();
//	//	//VectorXcf p(np);
//	//	//VectorXcf pTmp(np);
//	//	//srand((unsigned int)time(NULL));
//	//	//for (int i = 0; i < np; i++)
//	//	//{
//	//	//	p[i] = pAmp[i] * exp(complex<float>(0, 1) * (float)2.0 * (float)M_PI * ((float)rand() / RAND_MAX));
//	//	//}

//	//	//MatrixXcf Q = G.adjoint();
//	//	//VectorXcf tmp = Q.colwise().squaredNorm();
//	//	//Q = Q.array().rowwise() / tmp.transpose().array();
//	//	//MatrixXcf R = G * Q;

//	//	//for (int iItr = 0; iItr < nItr; iItr++)
//	//	//{
//	//	//	p = R * p;
//	//	//	pTmp = p;
//	//	//	p.array() = p.array() / p.cwiseAbs().array() * pAmp.array();
//	//	//}

//	//	//pTmp.array() = pTmp.array() / pTmp.cwiseAbs2().array() * pAmp.cwiseAbs2().array();
//	//	//VectorXcf q = Q * pTmp;
//	//	//for (int i = 0; i < nq; i++)
//	//	//{
//	//	//	if (abs(q(i)) > 1) q(i) = q(i) / abs(q(i));
//	//	//}

//	//	//return q;
//	//}

//	public GainGspatSel(Geometry geometry)
//    {
//		dir_coef_a = new float[9]{ 1.0f, 1.0f, 1.0f, 0.891250938f, 0.707945784f, 0.501187234f, 0.354813389f, 0.251188643f, 0.199526231f };
//		dir_coef_b = new float[9]{ 0, 0, -0.00459648054721f, -0.0155520765675f, -0.0208114779827f, -0.0182211227016f, -0.0122437497109f, -0.00780345575475f, -0.00312857467007f };
//		dir_coef_c = new float[9]{ 0, 0, -0.000787968093807f, -0.000307591508224f, -0.000218348633296f, 0.00047738416141f, 0.000120353137658f, 0.000323676257958f, 0.000143811850511f };
//		dir_coef_d = new float[9]{ 0, 0, 1.60125528528e-05f, 2.9747624976e-06f, 2.31910931569e-05f, -1.1901034125e-05f, 6.77743734332e-06f, -5.99548024824e-06f, -4.79372835035e-06f };

//		data = new Dictionary<int, Drive[]>();
//		for (int i = 0; i < geometry.NumDevices; i++)
//		{
//			Drive[] drives = new Drive[geometry[i].NumTransducers];
//			data.Add(i, drives);
//		}
//	}

//	public void setData(List<Vector3> points, Geometry geometry)
//	{
//		var G = makeMatrixG(points, geometry);

//		int count = 0;
//		for (int i = 0; i < geometry.NumDevices; i++)
//        {
//            for (int j = 0; j < geometry[i].NumTransducers; j++)
//            {
//                var tp = geometry[i][j].Position;
//                var dist = Vector3.Magnitude(tp - points[0]);
//                var phase = dist * geometry[i][j].Wavenumber(geometry[i].SoundSpeed);
//                //UnityEngine.Debug.Log(G[0, count].Real + " + j*" + G[0, count].Imaginary + ": " + G[0, count].Phase + ", " + Mathf.Repeat(phase, 2 * Mathf.PI));
//                data[i][j] = new Drive { Phase = -G[0, count].Phase, Amp = 1.0f };
//    //            data[i][j].Phase = phase;
//				//data[i][j].Amp = 1.0f;
//                count++;
//				//UnityEngine.Debug.Log(count);
//            }
//        }
//    }

//    public override Dictionary<int, Drive[]> Calc(Geometry geometry)
//    {
//		return new Dictionary<int, Drive[]>(data);
//        //return Transform(geometry, (dev, tr) =>
//        //{
//        //    return new Drive { Phase = 0, Amp = 1.0f };
//        //});
//    }
//}
