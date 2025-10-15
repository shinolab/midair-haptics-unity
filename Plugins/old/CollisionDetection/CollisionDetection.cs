using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class CollisionDetection : MonoBehaviour
{
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void makeInstance(bool printTime);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void deleteInstance();
    [DllImport("NativePluginHaptUnity")] unsafe public static extern float* getPointerPoint();
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getPoint(int id, float[] point);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getClusters(int id, float[] points, int[] indexStart);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getClustersTouch(int id, float[] points, int[] indexStart);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getClustersNear(int id, float[] points, int[] indexStart);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getCentroid(int id, float[] point);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getCentroidNear(int id, float[] point);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getDirection(int id, float[] direction);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getMeanForce(int id, float[] force);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getSumForce(int id, float[] force);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern int getIndexFace(int id, int[] indexFace);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void getNormalFace(int id, float[] normalFace);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void getForce(int id, float[] force);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void setObject(int numVertex, int numFace, int[] face, float radius, float[] centerBound);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void sendVertices(int id, float[] vertices);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void sendTransformSoftBody(int id, float[] transform);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void sendTransformRigidBody(int id, float[] transform);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void collisionDetection(int numPoint);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void collisionDetectionBothSide(int numPoint, int minClusterSize, float threshold);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void clustering(int minClusterSize, float threshold);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void setThresholdNearObject(float threshold);


    [DllImport("NativePluginHaptUnity")] unsafe public static extern void collisionDetectionCloth(int numPoint, float radiusNear, float radiusCenter);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void setCloth(int numCenter);
    [DllImport("NativePluginHaptUnity")] unsafe public static extern void sendCenter(float[] centers);

    [DllImport("NativePluginHaptUnity")] unsafe public static extern void getCentroidCloth(float[] points);


    [DllImport("NativePluginHaptUnity")] unsafe public static extern void setUseDirection(bool useDirection);
    public delegate void DebugLogDelegate(string str);
    static DebugLogDelegate debugLogFunc = msg => Debug.Log(msg);
    [DllImport("NativePluginHaptUnity")] public static extern void set_debug_log_func(DebugLogDelegate func);


    public static void setDebugLogFunc()
    {
        set_debug_log_func(debugLogFunc);
    }
}
