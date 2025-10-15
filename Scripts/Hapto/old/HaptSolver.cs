using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;

public class HaptSolver : MonoBehaviour
{

    protected bool first = true;
    protected bool touched = false;
    protected float[] point;

    protected ParticleSystem.Particle[] particle;
    protected ParticleSystem emitter;

    protected int maxNumPoint = 0;
    protected int numPoint = 0;
    protected Mesh mesh;

    public enum VisualizingMode
    {
        Off,
        On,
        ParticleLayerOnly
    }

    public List<HaptObject> haptObjects;
    public HaptFeedback haptFeedback = null;
    public VisualizingMode visualizingMode = VisualizingMode.Off;
    public bool coloringPointCloud = true;
    public bool getFaceTouched = true;
    public bool printTimeCollisionDetection = false;
    public bool clustering = true;
    public float thresholdClustering = 0.2f;
    public int minClusterSize = 3;
    public bool touchFromBothSide = true;

    public float thresholdInObject = 0.05f;
    public float coefficientFinger = 500f;

    public RealsenseCapture capture;
    public float scaleUnity = 10f;
    public bool useDirection = false;
 
    private void Awake()
    {
        if (capture.on)
        {
            CollisionDetection.setDebugLogFunc();
            CollisionDetection.makeInstance(printTimeCollisionDetection);
        }
    }

    void OnDestroy()
    {
        if (capture.on)
        {
            CollisionDetection.deleteInstance();
        }
    }

    public void Start()
    {
        maxNumPoint = capture.imageWidth * capture.imageHeight; // * capture.numRealsense * 3 / 10;
        point = new float[maxNumPoint];
        particle = new ParticleSystem.Particle[maxNumPoint];
        emitter = GetComponent<ParticleSystem>();
        mesh = new Mesh();
        if (visualizingMode == VisualizingMode.ParticleLayerOnly)
            gameObject.layer = LayerMask.NameToLayer("HandParticle"); ;

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
        {
            coloringPointCloud = !coloringPointCloud;
        }
    }

    void Prepare()
    {
        if (capture.on)
            CollisionDetection.setThresholdNearObject(thresholdInObject);
        int idCol = 0;
        foreach (var hapt in haptObjects)
        {
            //if (hapt.renderer != null)
                hapt.GetComponent<HaptObject>().setObject(idCol++, maxNumPoint);
        }
    }

    private void OnValidate()
    {
        CollisionDetection.setUseDirection(useDirection);
    }



    public  void visualize()
    {
        int num = numPoint;
        foreach (var hapt in haptObjects)
        {
            num += hapt.numPointInObject;
            if (coloringPointCloud)
            {
                num += hapt.numPointNearObject;
                num += hapt.numCluster;
                num += hapt.numClusterNear;
            }
        }

        emitter.Clear();
        emitter.Emit(num);
        emitter.GetParticles(particle);

        for (int i = 0; i < numPoint; i++)
        {
            particle[i].position = new Vector3(point[3 * i], point[3 * i + 1], point[3 * i + 2]) * scaleUnity;
        }

        if (coloringPointCloud)
        {
            int id = numPoint;
            foreach (var hapt in haptObjects)
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

                for (int i = 0; i < hapt.numCluster; i++)
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


    unsafe public void FixedUpdate()
    {
        if (first)
        {
            return;
        }

        foreach (var hapt in haptObjects)
        {
            if (hapt.renderer != null)
            {
                hapt.renderer.BakeMesh(mesh, true);
                hapt.vertices = mesh.vertices;
                for (int j = 0; j < hapt.numVertex; j++)
                {
                    hapt.vertices[j] = hapt.renderer.transform.TransformPoint(hapt.vertices[j]);// hapt.renderer.transform.lossyScale;
                    hapt.verticesF[3 * j] = hapt.vertices[j].x;
                    hapt.verticesF[3 * j + 1] = hapt.vertices[j].y;
                    hapt.verticesF[3 * j + 2] = hapt.vertices[j].z;
                }
            }
        }


        if (RealsenseCapture.instance == null)
        {
            foreach (var hapt in haptObjects) hapt.applyForce();
            return;
        }

        numPoint = RealsenseCapture.getPointGpu(CollisionDetection.getPointerPoint(), false);
        RealsenseCapture.getPoint(point, false);


        foreach (var hapt in haptObjects)
        {
            if (!hapt.applyforce) continue;
            CollisionDetection.sendVertices(hapt.id, hapt.verticesF);
            //for (int j = 0; j < hapt.numFaceCollision; j++)
            //{

            //    Debug.DrawLine(hapt.vertices[hapt.trianglesCollision[3 * j]], hapt.vertices[hapt.trianglesCollision[3 * j + 1]]);
            //    Debug.DrawLine(hapt.vertices[hapt.trianglesCollision[3 * j + 1]], hapt.vertices[hapt.trianglesCollision[3 * j + 2]]);
            //    Debug.DrawLine(hapt.vertices[hapt.trianglesCollision[3 * j + 2]], hapt.vertices[hapt.trianglesCollision[3 * j]]);
            //}

            var matRigid = hapt.transform.localToWorldMatrix;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    //hapt.transformSoft[r * 4 + c] = matSoft[r, c];
                    hapt.transformRigid[r * 4 + c] = matRigid[r, c];
                }
            }

            CollisionDetection.sendTransformRigidBody(hapt.id, hapt.transformRigid);
        }



        if (touchFromBothSide)
        {
            CollisionDetection.collisionDetectionBothSide(numPoint, minClusterSize, thresholdClustering);
        }
        else
        {
            CollisionDetection.collisionDetection(numPoint);
            if (clustering)
                CollisionDetection.clustering(minClusterSize, thresholdClustering);
            else
                CollisionDetection.clustering(minClusterSize, -1);
        }



        for (int i = 0; i < haptObjects.Count; i++)
        {
            var hapt = haptObjects[i];
            if (hapt.applyforce)
            {
                if (touchFromBothSide)
                {
                    hapt.numCluster = CollisionDetection.getClustersTouch(hapt.id, hapt.pointInObject, hapt.indexStartCluster);
                    hapt.numClusterNear = CollisionDetection.getClustersNear(hapt.id, hapt.pointNearObject, hapt.indexStartClusterNear);
                    hapt.numPointNearObject = hapt.indexStartClusterNear[hapt.numClusterNear];
                }
                else
                    hapt.numCluster = CollisionDetection.getClusters(hapt.id, hapt.pointInObject, hapt.indexStartCluster);

                hapt.numPointInObject = hapt.indexStartCluster[hapt.numCluster];

                CollisionDetection.getCentroid(hapt.id, hapt.centroids);
                CollisionDetection.getCentroidNear(hapt.id, hapt.centroidsNear);
                CollisionDetection.getDirection(hapt.id, hapt.directions);
                CollisionDetection.getSumForce(hapt.id, hapt.sumForce);


                if (getFaceTouched)
                {
                    for (int j = 0; j < hapt.numCluster; j++)
                    {
                        Vector3 pos = new Vector3(hapt.centroids[3 * j], hapt.centroids[3 * j + 1], hapt.centroids[3 * j + 2]);
                        Vector3 normal = new Vector3(hapt.sumForce[3 * j], hapt.sumForce[3 * j + 1], hapt.sumForce[3 * j + 2]);
                        //Debug.DrawLine(pos, pos + normal);
                    }

                    CollisionDetection.getMeanForce(hapt.id, hapt.meanForce);
                    CollisionDetection.getIndexFace(hapt.id, hapt.indexFaces);
                    for (int j = 0; j < hapt.numCluster; j++)
                    {
                        int iface = hapt.indexFaces[j];
                        var v0 = hapt.vertices[hapt.trianglesCollision[3 * iface]];
                        var v1 = hapt.vertices[hapt.trianglesCollision[3 * iface + 1]];
                        var v2 = hapt.vertices[hapt.trianglesCollision[3 * iface + 2]];
                        Debug.DrawLine(v0, v1, Color.green);
                        Debug.DrawLine(v0, v2, Color.green);
                        Debug.DrawLine(v2, v1, Color.green);
                    }
                }

                Vector3 sumForce = new Vector3(0, 0, 0);
                CollisionDetection.getForce(hapt.id, hapt.externalForceF);
                for (int j = 0; j < hapt.numVertex; j++)
                {
                    Vector3 force = new Vector3(hapt.externalForceF[3 * j], hapt.externalForceF[3 * j + 1], hapt.externalForceF[3 * j + 2]) * coefficientFinger;
                    hapt.externalForce[j] = force;
                    sumForce = sumForce + force;
                    //hapt.externalForce[j] = new Vector3(0, 0, 0);
                }

                if (hapt.reaction)
                    hapt.reaction.React(hapt, point, numPoint);
            }
            hapt.applyForce();
            //hapt.sumForce[3 * i] = sumForce.x / coefficientFinger;
            //hapt.sumForce[0] = sumForce.x / coefficientFinger;
            //hapt.sumForce[0] = sumForce.x / coefficientFinger;
            //Debug.Log("sumForce: " + sumForce);
        }

        if (visualizingMode != VisualizingMode.Off)
        {
            visualize();
        }

        if (haptFeedback != null)
            haptFeedback.Feedback(ref haptObjects, point, numPoint);

        //sw.Stop();
        //Debug.Log("time: " + sw.ElapsedMilliseconds + "ms");
    }
}
