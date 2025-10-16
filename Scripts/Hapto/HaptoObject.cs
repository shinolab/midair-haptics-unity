using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class HaptoObject : MonoBehaviour
{
    public Rigidbody? rigidbody = null;
    public float ratioForceToRigidbody = 1.0f;
    public Vector3 coeffForce = new Vector3(1, 1, 1);

    public bool detection = true;
    public float radiusBound = 0.45f;
    public Vector3 centerBound = new Vector3(0, 0, 0);
    public int startFaceCollision = -1;
    public int endFaceCollision = -1;
    public bool rayCast = false;
    public bool touchFromBothSides = true;
    public bool useTouchDirection = false;
    public bool usePointOutside = false;
    public float thrCosDirection = 0.2f;
    public bool dynamicBone = false;
    public bool isKinematic = false;
    public bool onlyApplyForce = false;
    public SkinnedMeshRenderer rendererBoneTarget;
    public float coeffFingerToBone = 1f;
    public float coeffKBonePos = 0f;
    public float coeffKBoneRot = 0.1f;
    float ratioKinematic = 0f;
    float dratioKinematic = 0.05f;

    [System.NonSerialized] public List<Rigidbody> rbBones = new List<Rigidbody>();

    [System.NonSerialized] public List<BoneInfo> infoBones = new List<BoneInfo>();

    [System.NonSerialized] public Transform[] bonesTarget;

    [System.NonSerialized] public List<(int, int, float)> vertexToBone = new List<(int, int, float)>(); 

    [System.NonSerialized] public int numBone = 0; // for soft body ( particles are registered as bones)


    [System.NonSerialized] public float[] bindingBox;

    [System.NonSerialized] public int id;  // for Collision Ditection

    [System.NonSerialized] public int numFace;
    [System.NonSerialized] public int numFaceCollision;
    [System.NonSerialized] public int[] triangles;
    [System.NonSerialized] public int[] trianglesCollision;

    [System.NonSerialized] public int numVertex;
    [System.NonSerialized] public Vector3[] vertices;
    [System.NonSerialized] public float[] verticesF;
    [System.NonSerialized] public float[] verticesTargetF;

    [System.NonSerialized] public Vector3[] externalForce;
    [System.NonSerialized] public float[] externalForceF;
    [System.NonSerialized] public float ampForce;

    [System.NonSerialized] public new SkinnedMeshRenderer renderer;
    [System.NonSerialized] public new MeshFilter meshFilter;
    //[System.NonSerialized] public HaptoReaction reaction = null;

    [System.NonSerialized] public float[] transformRigid;
    [System.NonSerialized] public float[] transformSoft;

    [System.NonSerialized] public float[] point;
    [System.NonSerialized] public float[] pointInObject;
    [System.NonSerialized] public float[] pointNearObject;
    [System.NonSerialized] public int numPointInObject;
    [System.NonSerialized] public int numPointNearObject;
    [System.NonSerialized] public int[] indexStartCluster;
    [System.NonSerialized] public int[] indexStartClusterNear;
    [System.NonSerialized] public int numClusterIn;
    [System.NonSerialized] public int numClusterNear;

    [System.NonSerialized] public float[] centroids;
    [System.NonSerialized] public float[] centroidsNear;
    [System.NonSerialized] public float[] directions;
    [System.NonSerialized] public float[] touchDirections;
    [System.NonSerialized] public float[] meanForce;
    [System.NonSerialized] public float[] sumForce;
    [System.NonSerialized] public int[] indexFaces;
    [System.NonSerialized] public float[] posMinPoint;
    [System.NonSerialized] public int[] indexFacePoint;
    [System.NonSerialized] public int[] indexPointTouch;
    [System.NonSerialized] public int numTouch;
    [System.NonSerialized] public float sumDeformation = 0;
    [System.NonSerialized] public HaptoFeedback feedback = null;
    [System.NonSerialized] public Mesh mesh;



    protected virtual void Awake()
    {
        if (rigidbody == null)
            rigidbody = GetComponent<Rigidbody>();
        //reaction = GetComponent<HaptoReaction>();
        feedback = GetComponent<HaptoFeedback>();
        mesh = new Mesh();
    }

    public virtual void SetObject(int _id, int maxNumPoint, int maxNumCluster)
    {
        id = _id;
        var mesh = new Mesh();
        if (renderer != null)
        {
            mesh = renderer.sharedMesh;
        }
        else
        {
            mesh = meshFilter.sharedMesh;
        }
        triangles = mesh.triangles;
        numVertex = mesh.vertices.Length;
        numFace = triangles.Length / 3;

        vertices = new Vector3[numVertex];
        verticesF = new float[3 * numVertex];
        verticesTargetF = new float[3 * numVertex];
        externalForce = new Vector3[numVertex];
        externalForceF = new float[3 * numVertex];

        transformRigid = new float[12];
        transformSoft = new float[12];
        point = new float[maxNumPoint * 3];
        pointInObject = new float[maxNumPoint * 3];
        pointNearObject = new float[maxNumPoint * 3];
        indexFacePoint = new int[maxNumPoint];
        posMinPoint = new float[maxNumPoint * 3];
        indexPointTouch = new int[maxNumPoint];

        indexStartCluster = new int[maxNumCluster];
        indexStartClusterNear = new int[maxNumCluster];
        numPointInObject = 0;
        if (startFaceCollision >= 0)
        {
            int start = startFaceCollision;
            int end = endFaceCollision;
            if (end < 0)
                end = numFace;
            numFaceCollision = end - start;
            trianglesCollision = new int[3 * numFaceCollision];
            int index = 0;
            for (int i = 3 * start; i < 3 * end; i++)
            {
                trianglesCollision[index++] = triangles[i];
            }
        }
        else
        {
            numFaceCollision = numFace;
            trianglesCollision = mesh.triangles;
        }

        centroids = new float[3 * maxNumCluster];
        centroidsNear = new float[3 * maxNumCluster];
        directions = new float[3 * maxNumCluster];
        touchDirections = new float[3 * maxNumCluster];
        meanForce = new float[3 * maxNumCluster];
        sumForce = new float[3 * maxNumCluster];
        indexFaces = new int[maxNumCluster];
        bindingBox = new float[6];

        if (dynamicBone)
        {
            PrepareDynamicBone();
        }
    }

    public void PrepareDynamicBone()
    {
        if (renderer == null || rendererBoneTarget == null)
            return;

        bonesTarget = new Transform[renderer.bones.Length];
        var buf = rendererBoneTarget.bones;

        numBone = 0;
        for (int i = 0; i < bonesTarget.Length; i++)
        {
            rbBones.Add(renderer.bones[i].GetComponent<Rigidbody>());
            infoBones.Add(renderer.bones[i].GetComponent<BoneInfo>());
            var name = renderer.bones[i].name;
            var bone = buf.FirstOrDefault(b => b.name == name);
            if (bone != null)
            {
                bonesTarget[i] = bone;
                numBone++;
                //Debug.Log(i + " " + renderer.bones[i].name + ": " + bonesTarget[i].name);
            }
        }

        //var weights = renderer.sharedMesh.boneWeights;
        var weights = rendererBoneTarget.sharedMesh.boneWeights;
        int[] index = new int[4];
        float[] weight = new float[4];
        vertexToBone.Clear();
        for (int i = 0; i < numVertex; i++)
        {
            index[0] = weights[i].boneIndex0; weight[0] = weights[i].weight0;
            index[1] = weights[i].boneIndex1; weight[1] = weights[i].weight1;
            index[2] = weights[i].boneIndex2; weight[2] = weights[i].weight2;
            index[3] = weights[i].boneIndex3; weight[3] = weights[i].weight3;

            //Debug.Log("Vertex " + i + ": " + index[0] + " " + weight[0] + ", " + index[1] + " " + weight[1] + ", " + index[2] + " " + weight[2] + ", " + index[3] + " " + weight[3]);

            for (int j = 0; j < 4; j++)
            {
                //Debug.Log("Bone " + i + ", " + j + ": " + renderer.bones[index[j]].name + ", " + weight[j]);
                if (weight[j] > 0 && renderer.bones[index[j]].GetComponent<Rigidbody>() != null)// && !renderer.bones[index[j]].GetComponent<Rigidbody>().isKinematic)
                {
                    vertexToBone.Add((i, index[j], weight[j]));
                    //Debug.Log(i + ": " + index[j] + " " + weight[j]);
                }
            }
        }
    }

    public virtual void ApplyForce()
    {
        ApplyForceRigid();
    }

    public virtual void ApplyForceGPU(float coeffPointForceBase)
    {
        if (!detection) return; 

        if (dynamicBone || rigidbody != null)
        {
            CollisionDetector.getForce(id, externalForceF);
            CollisionDetector.getVertexGlobal(id, verticesF);
            for (int j = 0; j < numVertex; j++)
            {
                externalForce[j] = new Vector3(externalForceF[3 * j], externalForceF[3 * j + 1], externalForceF[3 * j + 2]) * coeffPointForceBase;
                vertices[j] = new Vector3(verticesF[3 * j], verticesF[3 * j + 1], verticesF[3 * j + 2]);
            }
        }
        if (dynamicBone)
        {
            ApplyForceBone();
        }

        if (rigidbody != null)
        {
            ApplyForceRigid();
        }
    }

    public void ResetRatioKinematic()
    {
        ratioKinematic = 0;
    }

    public void ApplyForceBone()
    {
        if (renderer == null || rendererBoneTarget == null)
            return;
        var bones = renderer.bones;


        //if (GetComponent<ChangeKinematic>() != null)
        //{
        //    GetComponent<ChangeKinematic>().ChangeKinematicState();
        //}
        //isKinematic = true;

        if (isKinematic)
        {
            if (!onlyApplyForce)
            {
                for (int i = 0; i < numBone; i++)
                {
                    bones[i].localScale = bonesTarget[i].localScale;
                    bones[i].localPosition = ratioKinematic * bonesTarget[i].localPosition + (1 - ratioKinematic) * bones[i].localPosition;
                    bones[i].localRotation = Quaternion.Lerp(bones[i].localRotation, bonesTarget[i].localRotation, ratioKinematic);
                }
                //Debug.Log("Kinematic is true : " + ratioKinematic);
            }
        }
        else
        {
            if (!onlyApplyForce)
            {
                for (int i = 0; i < numBone; i++)
                {
                    //if (bones[i].GetComponent<Rigidbody>() == null || bones[i].GetComponent<Rigidbody>().isKinematic)
                    if (rbBones[i] == null || rbBones[i].isKinematic)
                    {
                        //if (bones[i].GetComponent<BoneInfo>() != null && bones[i].GetComponent<BoneInfo>().isFixed)
                        if (infoBones[i] != null && infoBones[i].isFixed)
                        {
                            bones[i].localScale = bonesTarget[i].localScale;
                            bones[i].position = bonesTarget[i].position;
                            bones[i].rotation = bonesTarget[i].rotation;
                        }
                        else
                        {

                            bones[i].localScale = bonesTarget[i].localScale;
                            bones[i].localPosition = ratioKinematic * bonesTarget[i].localPosition + (1 - ratioKinematic) * bones[i].localPosition;
                            bones[i].localRotation = Quaternion.Lerp(bones[i].localRotation, bonesTarget[i].localRotation, ratioKinematic);
                        }
                    }
                    else
                    {
                        //var rb = bones[i].GetComponent<Rigidbody>();
                        var rb = rbBones[i];

                        Vector3 position = bones[i].localPosition;
                        Vector3 targetPosition = bonesTarget[i].localPosition;
                        Vector3 deltaPosition = targetPosition - position;
                        Vector3 force = deltaPosition * coeffKBonePos;
                        //if (bones[i].GetComponent<BoneInfo>() != null)
                        if (infoBones[i] != null)
                        {
                            force *= bones[i].GetComponent<BoneInfo>().coeffKPos;
                        }
                        force = bones[i].parent.TransformDirection(force);
                        rb.AddForce(force);
                        //Debug.DrawLine(bones[i].position, bones[i].position + force * 100);
                        //Debug.Log(bones[i].name + ": " + bones[i].localPosition);
                        //Debug.Log(bonesTarget[i].name + ": " + bonesTarget[i].localPosition);
                        //Debug.Log(bonesTarget[i].name + ": " + force);

                        Quaternion localRotation = bones[i].localRotation;
                        Quaternion targetRotation = bonesTarget[i].localRotation;
                        //Quaternion deltaRotation = Quaternion.Inverse(localRotation) * targetRotation;
                        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(localRotation);
                        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
                        axis = bones[i].parent.TransformDirection(axis);

                        if (angle > 180f) angle -= 360f;
                        //if (Mathf.Abs(angle) < 0.1f) continue;

                        Vector3 torque = axis.normalized * angle * coeffKBoneRot;
                        //if (bones[i].GetComponent<BoneInfo>() != null)
                        if (infoBones[i] != null)
                        {
                            //torque *= bones[i].GetComponent<BoneInfo>().coeffKRot;
                            torque *= infoBones[i].coeffKRot;
                        }
                        //torque -= rb.angularVelocity * coeffDBoneRot;

                        rb.AddTorque(torque, ForceMode.Force);

                        //Debug.Log(bones[i].name + " " + bonesTarget[i].name + ": " + force.magnitude + ", " + torque.magnitude);
                    }
                }
            }
            for (int i = 0; i < vertexToBone.Count; i++)
            {
                var (ivert, ibone, weight) = vertexToBone[i];
                //Debug.Log(i + ": " + ivert + ", " + ibone + ", " + weight);
                //var rb = bones[ibone].GetComponent<Rigidbody>();
                var rb = rbBones[ibone];
                var force = externalForce[ivert] * weight * coeffFingerToBone;
                //if (bones[ibone].GetComponent<BoneInfo>() != null)
                if (infoBones[ibone] != null)
                {
                    //force *= bones[ibone].GetComponent<BoneInfo>().coeffPoint;
                    force *= infoBones[ibone].coeffPoint;
                }
                rb.AddForceAtPosition(force, vertices[ivert]);
            }
        }
        ratioKinematic = ratioKinematic + dratioKinematic;
        if (ratioKinematic > 1) ratioKinematic = 1;
    }

    public void ApplyForceRigid()
    {
        if (rigidbody == null || ratioForceToRigidbody == 0) return;
        for (int i = 0; i < numVertex; i++)
        {
            if (externalForce[i].magnitude == 0) continue;
            var posP = vertices[i];
            var force = Vector3.Scale(externalForce[i], coeffForce) * ratioForceToRigidbody;
            rigidbody.AddForceAtPosition(force, posP);

        }
    }

    public void SetTransformedVertices()
    {
        Transform trans;
        if (renderer != null)
        {
            renderer.BakeMesh(mesh, true);
            trans = renderer.transform;
        }
        else
        {
            mesh = meshFilter.sharedMesh;
            trans = meshFilter.transform;
        }
        vertices = mesh.vertices;

        for (int j = 0; j < numVertex; j++)
        {
            vertices[j] = trans.TransformPoint(vertices[j]);
            verticesF[3 * j] = vertices[j].x;
            verticesF[3 * j + 1] = vertices[j].y;
            verticesF[3 * j + 2] = vertices[j].z;
        }

        var matRigid = transform.localToWorldMatrix;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                transformRigid[r * 4 + c] = matRigid[r, c];
            }
        }
    }

    public void SetTransformedVerticesLocal()
    {
        Transform trans;
        if (renderer != null)
        {
            renderer.BakeMesh(mesh, true);
            trans = renderer.transform;
        }
        else
        {
            mesh = meshFilter.sharedMesh;
            trans = meshFilter.transform;
        }
        vertices = mesh.vertices;
        for (int j = 0; j < numVertex; j++)
        {
            //Debug.DrawLine(vertices[j], vertices[j] + new Vector3(0, 0.05f, 0));
            verticesF[3 * j] = vertices[j].x;
            verticesF[3 * j + 1] = vertices[j].y;
            verticesF[3 * j + 2] = vertices[j].z;
        }
        CollisionDetector.sendVertices(id, verticesF);

        var mat = trans.transform.localToWorldMatrix;
        //Debug.Log(mat);
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                transformSoft[r * 4 + c] = mat[r, c];
            }
        }
        CollisionDetector.sendTransformSoftBody(id, transformSoft);

        if (detection)
        {
            var matRigid = transform.localToWorldMatrix;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    transformRigid[r * 4 + c] = matRigid[r, c];
                }
            }
            CollisionDetector.sendTransformRigidBody(id, transformRigid);
        }
    }

    public virtual void SendObject()
    {
        float[] _centerBound = new float[3];
        for (int i = 0; i < 3; i++) _centerBound[i] = centerBound[i];
        CollisionDetector.setObject(numVertex, numFaceCollision, trianglesCollision, radiusBound, _centerBound);
    }

    void OnDrawGizmos()
    {
       Gizmos.DrawWireSphere(transform.position + centerBound, radiusBound);
    }
}
