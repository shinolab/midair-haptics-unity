using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;
using System.Diagnostics;
public class HaptoController : CollisionDetector
{
    public enum VisualizingMode
    {
        Off,
        On,
        ParticleLayerOnly
    }

    public List<GameObject> proxy = new List<GameObject>();
    public bool dynamicBound = false;
    public List<HaptoObject> haptoObjects;
    public bool interaction = true;
    public Preprocessor preprocessor;
    public HandTracker handTracker;
    public List<HaptoFeedback> haptFeedbacks = null;
    public VisualizingMode visualizingMode = VisualizingMode.Off;
    public bool visualizePointColor= true;
    public bool visualizeFaceTouched = true;
    //public bool touchFromBothSides = true;
    public int minPointInCluster = 3;
    public float thresholdClustering = 0.2f;
    public float thresholdNearObject = 0.05f;
    public float coefPointForce = 500f;
    protected bool first = true;

    protected ParticleSystem.Particle[] particle;
    protected ParticleSystem emitter;
    protected int maxNumPoint = 0;
    protected int maxNumCluster = 100;
    protected float[] pointBuffer = null;
   


    unsafe private void Awake()
    {
        setDebugLogFunc();
        makeInstance(false);// true);
    }

    void OnDestroy()
    {
        deleteInstance();
    }


    private void Start()
    {
        if (interaction && proxy == null)
        {
            maxNumPoint = RealsenseController.instance.maxNumPoint;
        }
        else
        {
            maxNumPoint = 100000;
        }
        particle = new ParticleSystem.Particle[maxNumPoint];
        emitter = GetComponent<ParticleSystem>();
        if (visualizingMode == VisualizingMode.ParticleLayerOnly)
            gameObject.layer = LayerMask.NameToLayer("HandParticle");

        pointBuffer = new float[maxNumPoint * 3];
        //Prepare();
    }


    void Update()
    {
        if (first)
        {
            Prepare();
            first = false;
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            switch (visualizingMode)
            {
                case VisualizingMode.Off:
                    visualizingMode = VisualizingMode.On;
                    break;
                case VisualizingMode.On:
                    gameObject.layer = LayerMask.NameToLayer("HandParticle");
                    visualizingMode = VisualizingMode.ParticleLayerOnly;
                    break;
                case VisualizingMode.ParticleLayerOnly:
                    gameObject.layer = LayerMask.NameToLayer("Default");
                    visualizingMode = VisualizingMode.Off;
                    break;
            }
        }
        if (Input.GetKeyDown(KeyCode.C))
            visualizePointColor = !visualizePointColor;
    }

    void Prepare()
    {
        int id = 0;
        foreach (var hapt in haptoObjects)
        {
            AddHaptoObject(hapt, id++);
        }
    }

    public void AddHaptoObject(HaptoObject hapt, int id)
    {
        //if (hapt.renderer != null)
        {
            if (id >= 0)
                hapt.SetObject(id, maxNumPoint, maxNumCluster);
            else
            {
                hapt.SetObject(haptoObjects.Count, maxNumPoint, maxNumCluster);
                haptoObjects.Add(hapt);
            }
            hapt.SendObject();

            //float[] _centerBound = new float[3];
            //for (int i = 0; i < 3; i++) _centerBound[i] = hapt.centerBound[i];
            //setObject(hapt.numVertex, hapt.numFaceCollision, hapt.trianglesCollision, hapt.radiusBound, _centerBound);
        }
    }


    protected int SetPointsFromProxy(float[] point, int num)
    {
        int sum = num;
        foreach (var pr in proxy)
        {
            if (!pr.active) continue;
            var vertices = pr.GetComponent<MeshFilter>().mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                var pos = pr.transform.TransformPoint(vertices[i]);
                point[3 * (sum + i)] = pos.x;
                point[3 * (sum + i) + 1] = pos.y;
                point[3 * (sum + i) + 2] = pos.z;
            }
            sum += vertices.Length;
        }
        return sum;
    }

    void Visualize(float[] point, int numPoint)
    {
        if (visualizingMode == VisualizingMode.Off) return;

        int num = numPoint;
        foreach (var hapt in haptoObjects)
        {
            num += hapt.numPointInObject;
            if (visualizePointColor)
            {
                num += hapt.numPointNearObject;
                num += hapt.numClusterIn;
                num += hapt.numClusterNear;
            }
        }

        emitter.Clear();
        emitter.Emit(num);
        emitter.GetParticles(particle);
        for (int i = 0; i < numPoint; i++)
        {
            particle[i].position = new Vector3(point[3 * i], point[3 * i + 1], point[3 * i + 2]) ;
            particle[i].startColor = new Color32(255, 200, 200, 1);
        }

        if (visualizePointColor)
        {
            int id = numPoint;
            foreach (var hapt in haptoObjects)
            {
                for (int i = 0; i < hapt.numPointInObject; i++)
                {
                    particle[id].position = new Vector3(hapt.pointInObject[3 * i], hapt.pointInObject[3 * i + 1], hapt.pointInObject[3 * i + 2]);
                    particle[id++].startColor = new Color32(0, 255, 0, 1);
                }


                for (int i = 0; i < hapt.numPointNearObject; i++)
                {
                    particle[id].position = new Vector3(hapt.pointNearObject[3 * i], hapt.pointNearObject[3 * i + 1], hapt.pointNearObject[3 * i + 2]);
                    particle[id++].startColor = new Color32(0, 0, 255, 1);
                }

                for (int i = 0; i < hapt.numClusterIn; i++)
                {
                    particle[id].position = new Vector3(hapt.centroids[3 * i], hapt.centroids[3 * i + 1], hapt.centroids[3 * i + 2]);
                    particle[id].startSize = 0.03f;
                    particle[id++].startColor = new Color32(0, 0, 255, 1);
                }

                for (int i = 0; i < hapt.numClusterNear; i++)
                {
                    particle[id].position = new Vector3(hapt.centroidsNear[3 * i], hapt.centroidsNear[3 * i + 1], hapt.centroidsNear[3 * i + 2]);
                    particle[id].startSize = 0.03f;
                    particle[id++].startColor = new Color32(0, 255, 0, 1);
                }
            }
        }
        emitter.SetParticles(particle, num);
    }

    protected int SetPointsFromHandTracker(float[] point, int num)
    {
        int mode = handTracker.Track();
        if (mode != 3) return num;
        Vector3[] vertex;
        if (handTracker.prediction)
            vertex = handTracker.getVertexPredictedHR();
        else
            vertex = handTracker.getVertexHR();
        int index = num * 3;
        for (int i = 0; i < vertex.Length; i++)
        {
            point[index++] = vertex[i].x;
            point[index++] = vertex[i].y;
            point[index++] = vertex[i].z;

        }
        return num + vertex.Length;
    }



    unsafe protected void FixedUpdate()
    {
        if (first)
        {
            return;
        }

        float[] point = null;
        int numPoint = 0;


        if (preprocessor != null) 
            numPoint = preprocessor.getProcessedPoints(ref point);
        if (numPoint == 0) point = pointBuffer;

        if (proxy.Count > 0)
            numPoint = SetPointsFromProxy(point, numPoint);

        if (handTracker != null)
        numPoint = SetPointsFromHandTracker(point, numPoint);

        GetCollisionInfo(numPoint, point);
    }

    unsafe protected void GetCollisionInfo(int numPoint, float[] point) {
        //var stopwatch = new Stopwatch();
        //stopwatch.Start();
        //long elapsedTicks = 0;

        foreach (var hapt in haptoObjects)
        {
            setDetection(hapt.id, hapt.detection);
            setTouchConditions(hapt.id, hapt.rayCast, hapt.touchFromBothSides, hapt.useTouchDirection, hapt.usePointOutside, hapt.thrCosDirection);
            hapt.SetTransformedVerticesLocal();
            //sendVertices(hapt.id, hapt.verticesF);
            //sendTransformRigidBody(hapt.id, hapt.transformRigid);
            //sendTransformSoftBody(hapt.id, hapt.transformSoft);
        }
        transformVertices();


        if (proxy.Count == 0 && handTracker == null && preprocessor != null)
            collisionDetectionBothSide(numPoint, preprocessor.getPointerPoint(), thresholdNearObject, minPointInCluster, thresholdClustering, dynamicBound);
        else
            collisionDetectionHostBothSide(numPoint, point, thresholdNearObject, minPointInCluster, thresholdClustering, dynamicBound);

        //UnityEngine.Debug.Log("Collision Detection: " + (stopwatch.ElapsedTicks - elapsedTicks) * (1000.0 / Stopwatch.Frequency) + " ms");
        //elapsedTicks = stopwatch.ElapsedTicks;

        for (int i = 0; i < haptoObjects.Count; i++)
        {
            var hapt = haptoObjects[i];
            hapt.numClusterIn = getClustersTouch(hapt.id, hapt.pointInObject, hapt.indexStartCluster);
            hapt.numClusterNear = getClustersNear(hapt.id, hapt.pointNearObject, hapt.indexStartClusterNear);
            hapt.numPointNearObject = hapt.indexStartClusterNear[hapt.numClusterNear];

            hapt.numPointInObject = hapt.indexStartCluster[hapt.numClusterIn];

            getCentroid(hapt.id, hapt.centroids);
            getCentroidNear(hapt.id, hapt.centroidsNear);
            getSumForce(hapt.id, hapt.sumForce);
            getMeanForce(hapt.id, hapt.meanForce);
            //getBindingBox(hapt.id, hapt.bindingBox);
            //UnityEngine.Debug.DrawLine(new Vector3(hapt.bindingBox[0], hapt.bindingBox[1], hapt.bindingBox[2]), new Vector3(hapt.bindingBox[0], hapt.bindingBox[1], hapt.bindingBox[5]));
            //UnityEngine.Debug.DrawLine(new Vector3(hapt.bindingBox[0], hapt.bindingBox[1], hapt.bindingBox[2]), new Vector3(hapt.bindingBox[3], hapt.bindingBox[1], hapt.bindingBox[2]));
            //UnityEngine.Debug.DrawLine(new Vector3(hapt.bindingBox[0], hapt.bindingBox[1], hapt.bindingBox[2]), new Vector3(hapt.bindingBox[0], hapt.bindingBox[4], hapt.bindingBox[2]));


            if (visualizeFaceTouched)
            {
                getDirection(hapt.id, hapt.directions);
                getIndexFace(hapt.id, hapt.indexFaces);
                getTouchDirection(hapt.id, hapt.touchDirections);

                getPoint(hapt.id, hapt.point);
                int numTouch = getIndexPointTouch(hapt.id, hapt.indexPointTouch);
                getPosMinPoint(hapt.id, hapt.posMinPoint);
                getIndexFacePoint(hapt.id, hapt.indexFacePoint);

                for (int j = 0; j < hapt.numClusterIn; j++)
                {
                    int iface = hapt.indexFaces[j];
                    var v0 = hapt.vertices[hapt.trianglesCollision[3 * iface]];
                    var v1 = hapt.vertices[hapt.trianglesCollision[3 * iface + 1]];
                    var v2 = hapt.vertices[hapt.trianglesCollision[3 * iface + 2]];
                    UnityEngine.Debug.DrawLine(v0, v1, Color.green);
                    UnityEngine.Debug.DrawLine(v0, v2, Color.green);
                    UnityEngine.Debug.DrawLine(v2, v1, Color.green);

                    var centroid = new Vector3(hapt.centroids[3 * j], hapt.centroids[3 * j + 1], hapt.centroids[3 * j + 2]);
                    var direction = new Vector3(hapt.touchDirections[3 * j], hapt.touchDirections[3 * j + 1], hapt.touchDirections[3 * j + 2]);
                    UnityEngine.Debug.DrawLine(centroid, centroid + direction * 0.3f, Color.red);
                }

                for (int j = 0; j < numTouch; j++)
                {
                    int k = hapt.indexPointTouch[j];
                    if (hapt.indexFacePoint[k] < 0)
                    {
                        continue;
                    }
                    var posmin = new Vector3(hapt.posMinPoint[3 * k], hapt.posMinPoint[3 * k + 1], hapt.posMinPoint[3 * k + 2]);
                    var pos = new Vector3(hapt.point[3 * k], hapt.point[3 * k + 1], hapt.point[3 * k + 2]);
                    UnityEngine.Debug.DrawLine(pos, posmin);
                }
            }

            hapt.ApplyForceGPU(coefPointForce);

            if (hapt.feedback)
                hapt.feedback.Feedback(ref hapt, point, numPoint);
        }


        Visualize(point, numPoint);
        foreach (var feedback in haptFeedbacks)
        {
            feedback.Feedback(ref haptoObjects, point, numPoint);
        }

    }
}
