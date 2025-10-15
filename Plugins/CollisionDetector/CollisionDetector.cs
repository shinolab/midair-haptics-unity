using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class CollisionDetector : MonoBehaviour
{
    [DllImport("dll_collisionDetection")] unsafe protected static extern void makeInstance(bool printTime);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void deleteInstance();
    //[DllImport("dll_collisionDetection")] unsafe protected static extern float* getPointerPoint();
    //[DllImport("dll_collisionDetection")] unsafe protected static extern void setPointerPoint(float* pointer);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getPoint(int id, float[] point);
    [DllImport("dll_collisionDetection")] unsafe public static extern int getVertexGlobal(int id, float[] vertices);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getClusters(int id, float[] points, int[] indexStart);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getClustersTouch(int id, float[] points, int[] indexStart);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getClustersNear(int id, float[] points, int[] indexStart);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getCentroid(int id, float[] point);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getCentroidNear(int id, float[] point);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getDirection(int id, float[] direction);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getMeanForce(int id, float[] force);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getSumForce(int id, float[] force);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getIndexFace(int id, int[] indexFace);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getIndexPointTouch(int id, int[] indexPoint);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getPosMinPoint(int id, float[] posMinPoint);
    [DllImport("dll_collisionDetection")] unsafe protected static extern int getIndexFacePoint(int id, int[] indexFacePoint);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void getNormalFace(int id, float[] normalFace);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void getBindingBox(int id, float[] bindingBox);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void getTouchDirection(int id, float[] touchDirection);
    [DllImport("dll_collisionDetection")] unsafe public static extern void getForce(int id, float[] force);
    [DllImport("dll_collisionDetection")] unsafe public static extern void getParticleForce(int id, float[] force, 
        float ratioInternalForce, float ratioExternalForce, float coeffK, float coeffD, float maxForce, float fixedDelta);
    [DllImport("dll_collisionDetection")] unsafe public static extern void setObject(int numVertex, int numFace, int[] face, float radius, float[] centerBound);
    [DllImport("dll_collisionDetection")] unsafe public static extern void setObjectParticle(int numVertex, int numFace, int[] face, float radius, float[] centerBound,
        float[] weightVertex, float[] weightParticle, int[] idParticle, int numParticle, int numParticleConnected);
    [DllImport("dll_collisionDetection")] unsafe public static extern void setDetection(int id, bool detection);
    [DllImport("dll_collisionDetection")] unsafe public static extern void setTouchConditions(int id, bool rayCast, bool touchFromBothSides, bool useTouchDirection, bool usePointOutside, float thrCosDirection);
    [DllImport("dll_collisionDetection")] unsafe public static extern void sendVertices(int id, float[] vertices);
    [DllImport("dll_collisionDetection")] unsafe public static extern void sendVerticesTarget(int id, float[] vertices);
    [DllImport("dll_collisionDetection")] unsafe public static extern void sendTransformSoftBody(int id, float[] transform);
    [DllImport("dll_collisionDetection")] unsafe public static extern void sendTransformRigidBody(int id, float[] transform);
    [DllImport("dll_collisionDetection")] unsafe public static extern void transformVertices();
    [DllImport("dll_collisionDetection")] unsafe public static extern void transformVerticesTarget(int id);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void collisionDetection
        (int numPoint, float* point, float thresholdInObject);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void collisionDetectionHost
        (int numPoint, float[] point, float thresholdInObject);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void collisionDetectionBothSide
    (int numPoint, float* point, float thresholdNearObject, int minClusterSize, float thresholdClustering, bool dynamicBound);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void collisionDetectionHostBothSide
        (int numPoint, float[] point, float thresholdNearObject, int minClusterSize, float thresholdClustering, bool dynamicBound);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void clustering(int minClusterSize, float threshold);
    [DllImport("dll_collisionDetection")] unsafe protected static extern void setThresholdNearObject(float threshold);

    [DllImport("dll_collisionDetection")] unsafe public static extern void collisionDetectionSphere(int numPoint, float* point,  float radius, int numSphere);
    [DllImport("dll_collisionDetection")] unsafe public static extern void collisionDetectionSphereRadius(int numPoint, float* point, float[] radius, int numSphere);
    [DllImport("dll_collisionDetection")] unsafe public static extern void setSphereCollision(int maxNumCenter, int maxNumInCenter);
    [DllImport("dll_collisionDetection")] unsafe public static extern void sendCenterSphere(float[] centers, int numSphere);
    [DllImport("dll_collisionDetection")] unsafe public static extern void getCentroidSphere(float[] points, int numSphere);

    public delegate void DebugLogDelegate(string str);
    static DebugLogDelegate debugLogFunc = msg => Debug.Log(msg);
    [DllImport("dll_collisionDetection")] public static extern void set_debug_log_func(DebugLogDelegate func);


    public static void setDebugLogFunc()
    {
        set_debug_log_func(debugLogFunc);
    }
}
