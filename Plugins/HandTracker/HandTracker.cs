using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using static HandTracker;
//using System.Diagnostics;
unsafe public class HandTracker: MonoBehaviour
{
    [DllImport("dll_handTracking")] unsafe protected static extern void deleteInstance();
    [DllImport("dll_handTracking")] unsafe protected static extern void setDatasetPath(string path);
    [DllImport("dll_handTracking")] unsafe protected static extern void setSigmaPose(float _sigmaPoseA, float _sigmaPoseB, float _sigmaPoseC);
    [DllImport("dll_handTracking")] unsafe protected static extern void setDelay(float delay);
    [DllImport("dll_handTracking")] unsafe public static extern void setEdirsHR(float[] v_template, float[] weights, float[] posedirs, float[] shapedirs, int numVertex);
    [DllImport("dll_handTracking")] unsafe protected static extern void makeInstance(bool _printTime, int numPoint, 
        float[] initPos, float[] initRot, float[] _minRegion, float[] _maxRegion, int[] _numGrid, int _numPointStart, int _numPointEnd,
            int iaxis, bool fromMax, float ratioCut);
    [DllImport("dll_handTracking")] unsafe protected static extern void makeInstancePrediction(bool _printTime, int numPoint,
     float[] initPos, float[] initRot, float[] _minRegion, float[] _maxRegion, int[] _numGrid, int _numPointStart, int _numPointEnd,
         int iaxis, bool fromMax, float ratioCut, bool prediction);
    [DllImport("dll_handTracking")] unsafe protected static extern int track(float* pointD, float* normalD, bool* validityD, bool _prediction);
    [DllImport("dll_handTracking")] unsafe protected static extern void getVertex(float[] vertices);
    [DllImport("dll_handTracking")] unsafe protected static extern void getVertexPredicted(float[] vertices);
    [DllImport("dll_handTracking")] unsafe public static extern void getVertexHR(float[] vertices);
    [DllImport("dll_handTracking")] unsafe public static extern void getVertexPredictedHR(float[] vertices);
    [DllImport("dll_handTracking")] unsafe protected static extern void getParameter(float[] beta, float[] trans, float[] pose);
    [DllImport("dll_handTracking")] unsafe protected static extern void getParameterPredicted(float[] beta, float[] trans, float[] pose);
    [DllImport("dll_handTracking")] unsafe protected static extern int getNumBeta();
    [DllImport("dll_handTracking")] unsafe protected static extern int getNumJoint();
    [DllImport("dll_handTracking")] unsafe protected static extern void getIndexFace(int[] indices);
    [DllImport("dll_handTracking")] unsafe protected static extern int getPointSampled(float[] points);
    [DllImport("dll_handTracking")] unsafe public static extern int getJointPosition(float[] position, int id);
    [DllImport("dll_handTracking")] unsafe public static extern int getJointPositionPredicted(float[] position, int id);
    [DllImport("dll_handTracking")] unsafe public static extern int getJointRotMatirx(float[] rotMat, int id);
    [DllImport("dll_handTracking")] unsafe public static extern int getJointRotMatirxPredicted(float[] rotMat, int id);
    public delegate void DebugLogDelegate(string str);
    static DebugLogDelegate debugLogFunc = msg => Debug.Log(msg);
    [DllImport("dll_handTracking")] public static extern void set_debug_log_func(DebugLogDelegate func);

    [System.Serializable]
    public struct Int3
    {
        public int x;
        public int y;
        public int z;
    }

    public bool autoUpdate = false;
    public bool updateMesh = true;
    public bool visualizePoint = true;
    public bool prediction = false;
    public float delay = 70;
    public float scaleUnity = 10f;
    public Preprocessor preprocessor;
    public string datasetPath = "HandTracking/data/";
    public GameObject hand;
    public GameObject handPredicted;
    public Vector3 minRegion;
    public Vector3 maxRegion;
    public Int3 numGrid;
    public int numPointStart = 900;
    public int numPointEnd = 100;
    public float sigmaPoseA = 0.003f;
    public float sigmaPoseB = 0.03f;
    public float sigmaPoseC = 0.09f;
    public int iaxisCut = 2;
    public bool fromMax = false;
    public float ratioCut = 0.95f;
    [SerializeField][HideInInspector] public float[] vertex;
    [SerializeField][HideInInspector] public float[] vertexPredicted;
    [SerializeField][HideInInspector] public float[] vertexBind;
    [SerializeField][HideInInspector] public float[] trans;
    [SerializeField][HideInInspector] public float[] pose;
    [SerializeField][HideInInspector] public float[] beta;
    [SerializeField][HideInInspector] public float[] transPredicted;
    [SerializeField][HideInInspector] public float[] posePredicted;
    [SerializeField][HideInInspector] public float[] betaPredicted;
    [SerializeField][HideInInspector] public int numVertex = 0;
    [SerializeField][HideInInspector] public int numBeta = 0;
    [SerializeField][HideInInspector] public int numJoint = 0;
    [SerializeField][HideInInspector] public int degFreeJoint = 0;
    [SerializeField][HideInInspector] public int degElemJoint = 0;
    private Vector3[] vlist;
    private Vector3[] vlistPredicted;
    bool instanced = false;
    float[] points;
    MeshBinderBone meshBinder = null;
    MeshBinderBone meshBinderPredicted = null;
    MeshRenderer meshRenderer = null;
    MeshRenderer meshRendererPredicted = null;
    [SerializeField][HideInInspector] public int mode = 0;
    float[] buf = new float[12];

    public class Coeffs
    {
        public DenseMatrix v_template;
        public DenseMatrix J_regressor;
        public DenseMatrix weights;
        public int[,] kintree_table;
        public DenseMatrix[] posedirs;
        public DenseMatrix[] shapedirs;
    }
    [SerializeField][HideInInspector] public Coeffs coeffs = new Coeffs();


    public static void setDebugLogFunc()
    {
        set_debug_log_func(debugLogFunc);
    }


    private void Awake()
    {
        setDebugLogFunc();
    }

    public static void LoadCsvToDenseMatrixArray(ref DenseMatrix[] array, string filePath, int num)
    {
        var mat = LoadCsvToDenseMatrix(filePath);
        array = new DenseMatrix[num];
        var ncol = mat.ColumnCount;
        var nrow = mat.RowCount / num;  
        for (int i = 0; i < num; i++)
        {
            array[i] = DenseMatrix.OfMatrix(mat.SubMatrix(nrow * i, nrow, 0, ncol));
        }
    }

    public static DenseMatrix LoadCsvToDenseMatrix(string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        int rowCount = lines.Length;
        int colCount = lines[0].Split(',').Length;

        var matrix = new float[rowCount, colCount];

        for (int i = 0; i < rowCount; i++)
        {
            var parts = lines[i].Split(',');
            for (int j = 0; j < colCount; j++)
            {
                matrix[i, j] = float.Parse(parts[j], CultureInfo.InvariantCulture);
            }
        }

        return DenseMatrix.OfArray(matrix);
    }

    public static void LoadCsvToIntMatrix(ref int[,] matrix, string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        int rowCount = lines.Length;
        int colCount = lines[0].Split(',').Length;

        matrix = new int[rowCount, colCount];

        for (int i = 0; i < rowCount; i++)
        {
            var parts = lines[i].Split(',');
            for (int j = 0; j < colCount; j++)
            {
                matrix[i, j] = Mathf.RoundToInt(float.Parse(parts[j], CultureInfo.InvariantCulture));
            }
        }
    }

    public Vector3[] getVertex()
    {
        return vlist;
    }

    public Vector3 getJointPosition(int id)
    {
        int i = getJointPosition(buf, id);
        if (i == -1)
            return Vector3.zero;
        else
            return new Vector3(-buf[0], buf[1], buf[2]) * scaleUnity;
    }

    public Vector3 getJointPositionPredicted(int id)
    {
        int i = getJointPositionPredicted(buf, id);
        if (i == -1)
            return Vector3.zero;
        else
            return new Vector3(-buf[0], buf[1], buf[2]) * scaleUnity;
    }

    public Vector3[] getVertexPredicted()
    {
        if (prediction)
            return vlistPredicted;
        else
            return null;
    }

    public Vector3[] getVertexHR()
    {
        if (meshBinder != null)
            return meshBinder.getVertex();
        else
            return null;
    }

    public Vector3[] getVertexPredictedHR()
    {
        if (meshBinderPredicted != null && prediction)
            return meshBinderPredicted.getVertex();
        else
            return null;
    }

    private void initBinding()
    {
        beta = new float[numBeta];
        trans = new float[3];
        pose = new float[3 * numJoint];
        betaPredicted = new float[numBeta];
        transPredicted = new float[3];
        posePredicted = new float[3 * numJoint];
        Debug.Log("numBeta: " + numBeta);
        Debug.Log("numJoin: " + numJoint);
        vertexBind = new float[numVertex * 3];

        coeffs.v_template = LoadCsvToDenseMatrix(System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/v_template.csv"));
        coeffs.J_regressor = LoadCsvToDenseMatrix(System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/J_regressor.csv"));
        coeffs.weights = LoadCsvToDenseMatrix(System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/weights.csv"));
        LoadCsvToIntMatrix(ref coeffs.kintree_table, System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/kintree_table.csv"));
        LoadCsvToDenseMatrixArray(ref coeffs.posedirs, System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/posedirs.csv"), degElemJoint);
        LoadCsvToDenseMatrixArray(ref coeffs.shapedirs, System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/shapedirs.csv"), numBeta);


        SetParameter();
        if (hand != null )
        {
            meshBinder = hand.GetComponent<MeshBinderBone>();
            if (meshBinder != null)
                meshBinder.InitBinder();
        }
        if (handPredicted != null)
        {
            meshBinderPredicted = handPredicted.GetComponent<MeshBinderBone>();
            if (meshBinderPredicted != null)
                meshBinderPredicted.InitBinder();
        }

        //Debug.Log("v_template: " + coeffs.v_template.RowCount + ", " + coeffs.v_template.ColumnCount + ", " + coeffs.v_template[0, 0]);
        //Debug.Log(coeffs.posedirs.Length);
        //Debug.Log("posedirs: " + coeffs.posedirs[degElemJoint-1].RowCount + ", " + coeffs.posedirs[degElemJoint - 1].ColumnCount + ", " + coeffs.posedirs[degElemJoint - 1][0, 0]);
        //Debug.Log("shapedirs: " + coeffs.shapedirs[numBeta-1].RowCount + ", " + coeffs.shapedirs[numBeta - 1].ColumnCount + ", " + coeffs.shapedirs[numBeta - 1][0, 0]);

        //CalcVertexGlobal(coeffs, vertexBind, beta, pose, trans);
        //for (int i = 0; i < vertexBind.Length/3; i++)
        //{
        //    Debug.DrawLine(Vector3.zero, new Vector3(vertexBind[3*i], vertexBind[3*i+1], vertexBind[3 * i + 2]), Color.red, 10000000);
        //    Debug.Log(new Vector3(vertexBind[3 * i], vertexBind[3 * i + 1], vertexBind[3 * i + 2]));
        //}

    }

    public void SetParameter()
    {
        getParameter(beta, trans, pose);
        if (prediction)
        {
            getParameterPredicted(betaPredicted, transPredicted, posePredicted);
        }
    }

    private void Start()
    {
        if (RealsenseController.instance == null)
        {
            Debug.LogError("RealsenseController is not found");
            return;
        }

        string fullPath = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath);
        setDatasetPath(fullPath);

        float[] initPos = new float[3];
        float[] initRot = new float[3];

        var rot = hand.transform.rotation;
        float angle = - 2.0f * Mathf.Acos(rot.w);
        float sinHalfAngle = Mathf.Sqrt(1.0f - rot.w * rot.w);
        Vector3 axis;
        if (sinHalfAngle < 0.0001f)
        {
            axis = new Vector3(1, 0, 0);
        }
        else
        {
            axis = new Vector3(rot.x, rot.y, rot.z) / sinHalfAngle;
        }
        initRot[0] = -axis.x * angle;
        initRot[1] = axis.y * angle;
        initRot[2] = axis.z * angle;

        string fileJoint = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/J.csv");
        var reader = new StreamReader(fileJoint);
        var line = reader.ReadLine();
        var values = line.Split(',');
        var j0 = new Vector3(-float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2])) * scaleUnity;
        Vector3 localJ0 = hand.transform.TransformPoint(j0);
        //Vector3 localJ0 = transform.InverseTransformPoint(hand.transform.TransformPoint(j0));
        //Debug.DrawLine(j0, localJ0, Color.red, 100000);
        var diff = localJ0 - j0;

        initPos[0] = -diff.x / scaleUnity;
        initPos[1] = diff.y / scaleUnity;
        initPos[2] = diff.z / scaleUnity;

        float[] _minRegion = new float[3] { -maxRegion.x / scaleUnity, minRegion.y / scaleUnity, minRegion.z / scaleUnity };
        float[] _maxRegion = new float[3] { -minRegion.x / scaleUnity, maxRegion.y / scaleUnity, maxRegion.z / scaleUnity };
        int[] _numGrid = new int[3] { numGrid.x, numGrid.y, numGrid.z };

        makeInstancePrediction(false, RealsenseController.instance.maxNumPoint, initPos, initRot, _minRegion, _maxRegion, _numGrid, numPointStart, numPointEnd, iaxisCut, fromMax, ratioCut, true);
        instanced = true;
        setSigmaPose(sigmaPoseA, sigmaPoseB, sigmaPoseC);

        numVertex = hand.GetComponent<MeshFilter>().mesh.vertexCount;
        numBeta = getNumBeta();
        numJoint = getNumJoint();
        degFreeJoint = numJoint * 3;
        degElemJoint = (numJoint - 1) * 9;
        hand.transform.position = Vector3.zero;
        hand.transform.rotation = Quaternion.identity;
        meshRenderer = hand.GetComponent<MeshRenderer>();
        if (handPredicted != null)
        {
            handPredicted.transform.position = Vector3.zero;
            handPredicted.transform.rotation = Quaternion.identity;
            meshRendererPredicted = handPredicted.GetComponent<MeshRenderer>();
        }

        //CreateHandMesh();
        vertex = new float[numVertex * 3];
        vertexPredicted = new float[numVertex * 3];
        vlist = new Vector3[numVertex];
        vlistPredicted = new Vector3[numVertex];

        points = new float[RealsenseController.instance.maxNumPoint * 3];

        initBinding();

        //CreateHandMesh();
    }

    private void OnDestroy()
    {
        if (instanced)
        {
            deleteInstance();
        }
    }

    private void OnValidate()
    {
        if (instanced)
        {
            setSigmaPose(sigmaPoseA, sigmaPoseB, sigmaPoseC);
        }
    }

    private void CreateHandMesh()
    {
        string fileVertex = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/v_template.csv");
        string fileFace = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + datasetPath + "/f.csv");
        //load csv file to make List<Vector3> of vertex positions.
        List<Vector3> vlist = new List<Vector3>();
        List<int> flist = new List<int>();
        using (var reader = new StreamReader(fileVertex))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                vlist.Add(new Vector3(-float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2])) * scaleUnity);
                //Debug.Log(vlist[vlist.Count - 1]);
            }
        }
        using (var reader = new StreamReader(fileFace))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                flist.Add((int)float.Parse(values[0]));
                flist.Add((int)float.Parse(values[2]));
                flist.Add((int)float.Parse(values[1]));
                //Debug.Log(flist[flist.Count - 1]);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vlist);
        mesh.SetTriangles(flist, 0);
        hand.GetComponent<MeshFilter>().mesh = mesh;

        string path = "Assets/my/hand.asset";
        //UnityEditor.AssetDatabase.CreateAsset(mesh, path);
        //UnityEditor.AssetDatabase.SaveAssets();
    }

    private void UpdateHandMesh()
    {
        if (updateMesh)
        {
            if (hand != null)
            {
                //if (meshRenderer.enabled == false) meshRenderer.enabled = true;
                var filter = hand.GetComponent<MeshFilter>();
                filter.mesh.SetVertices(vlist);
                filter.mesh.RecalculateNormals();
                filter.mesh.RecalculateBounds();
            }
            if (handPredicted != null && prediction)
            {
                //if (meshRendererPredicted == false) meshRendererPredicted.enabled = true;
                var filter = handPredicted.GetComponent<MeshFilter>();
                filter.mesh.SetVertices(vlistPredicted);
                filter.mesh.RecalculateNormals();
                filter.mesh.RecalculateBounds();
            }
        }
        else
        {

        }
    }

    public void VisualizePoint()
    {
        //var stopwatch = new Stopwatch();
        //stopwatch.Start();
        //long elapsedTicks = 0;

        int num = getPointSampled(points);
        //Debug.Log(num);
        var emitter = GetComponent<ParticleSystem>();
        ParticleSystem.Particle[] particle = new ParticleSystem.Particle[num];
        emitter.Clear();
        emitter.Emit(num);
        emitter.GetParticles(particle);


        for (int i = 0; i < num; i++)
        {
            particle[i].position = new Vector3(-points[3 * i], points[3 * i + 1], points[3 * i + 2]) * scaleUnity;
            particle[i].startSize = 0.02f;
        }

        emitter.SetParticles(particle, num);
        //UnityEngine.Debug.Log(num.ToString() + ": " + points[0].ToString() + ", " + points[1].ToString() + ", " + points[2].ToString());

        //UnityEngine.Debug.Log("Visualize: " + (stopwatch.ElapsedTicks - elapsedTicks) * (1000.0 / Stopwatch.Frequency) + " ms");
        //elapsedTicks = stopwatch.ElapsedTicks;
    }

    private void FixedUpdate()
    {
        if (autoUpdate)
            Track();
    }


    public int Track( )
    {
        if (preprocessor.Process() < 0) return -1;
        if (prediction) setDelay(delay);
        mode = track(preprocessor.getPointerPointGlobal(), preprocessor.getPointerNormal(), preprocessor.getPointerValidity(), prediction);
        getVertex(vertex);
        if (prediction)
            getVertexPredicted(vertexPredicted);
        if (visualizePoint)
            VisualizePoint();

        for (int i = 0; i < numVertex; i++)
        {
            vlist[i] = (new Vector3(-vertex[i * 3], vertex[i * 3 + 1], vertex[i * 3 + 2]) * scaleUnity);
            if (prediction)
                vlistPredicted[i] = (new Vector3(-vertexPredicted[i * 3], vertexPredicted[i * 3 + 1], vertexPredicted[i * 3 + 2]) * scaleUnity);
        }
        if (meshBinder != null)
        {
            meshBinder.Bind(false);
        }
        if (meshBinderPredicted && prediction)
        {
            meshBinderPredicted.Bind(true);
        }

        SetParameter();
        UpdateHandMesh();

        return mode;
    }
    
    public Vector3 getTrans()
    {
        return new Vector3(-trans[0], trans[1], trans[2]) * scaleUnity;
    }

    public Vector3 getTransPredicted()
    {
        if (prediction)
            return new Vector3(-transPredicted[0], transPredicted[1], transPredicted[2]) * scaleUnity;
        else
            return Vector3.zero;
    }

    public static DenseMatrix RodriguesToMatrix(Span<float> vec)
    {
        float theta = Mathf.Sqrt(vec[0]* vec[0] + vec[1] * vec[1]+ vec[2]* vec[2]);
        //Debug.Log(vec[0] + ", " + vec[1] + ", " + vec[2] + ": " + theta);

        if (theta <= 1e-8)
        {
            return DenseMatrix.CreateIdentity(3);
        }

        float x = vec[0]/theta, y = vec[1]/theta, z = vec[2]/theta;

        var K = DenseMatrix.OfArray(new float[,] {
            { 0f, -z,  y },
            { z,  0f, -x },
            {-y,  x,  0f }
        });

        var I = DenseMatrix.CreateIdentity(3);
        var K2 = K.Multiply(K);

        DenseMatrix R = (DenseMatrix)(I + K.Multiply((float)Math.Sin(theta)) + K2.Multiply(1 - (float)Math.Cos(theta)));
        return R;
    }

    public DenseMatrix[] CalcVertexGlobal(Coeffs _coeffs, Vector3[] _vertex, float[] _beta, float[] _pose, float[] _trans)
    {
        var v_shaped = _coeffs.v_template.Clone();
        for (int i = 0; i < numBeta; i++)
        {
            v_shaped = v_shaped.Add(_coeffs.shapedirs[i].Multiply(_beta[i]));
        }
        var J = _coeffs.J_regressor * v_shaped;

        int index = 0;
        for (int i = 1; i < numJoint; i++)
        {
            var R = RodriguesToMatrix(_pose.AsSpan(3 * i));
            var diff = R - DenseMatrix.CreateIdentity(3);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    v_shaped = v_shaped.Add(_coeffs.posedirs[index++].Multiply(diff[r, c]));
        }

        var rotMats = makeRotMats(_coeffs, J, _beta, _pose, _trans);
        CalcVertexGlobal2(_coeffs, rotMats, v_shaped, _vertex, _beta,_pose,_trans);

        return rotMats;
    }

    DenseMatrix[] makeRotMats(Coeffs _coeffs, Matrix<float> J, float[] _beta, float[] _pose, float[] _trans)
    {
        var rotMats = new DenseMatrix[numJoint];

        var mat0 = DenseMatrix.CreateIdentity(4);
        mat0.SetSubMatrix(0, 0, RodriguesToMatrix(_pose.AsSpan(0)));

        var transVec = Vector<float>.Build.DenseOfArray(new float[] { _trans[0], _trans[1], _trans[2] });
        mat0.SetSubMatrix(0, 3, J.Row(0).ToColumnMatrix().Add(transVec.ToColumnMatrix()));

        rotMats[0] = mat0;

        for (int i = 1; i < _coeffs.kintree_table.GetLength(1); i++)
        {
            int iJoint = _coeffs.kintree_table[1, i];
            int iParent = _coeffs.kintree_table[0, i];

            var mat = DenseMatrix.CreateIdentity(4);
            mat.SetSubMatrix(0, 0, RodriguesToMatrix(_pose.AsSpan(3 * iJoint)));
            mat.SetSubMatrix(0, 3, (J.Row(iJoint) - J.Row(iParent)).ToColumnMatrix());

            rotMats[iJoint] = (DenseMatrix)(rotMats[iParent] * mat);
        }

        for (int i = 0; i < numJoint; i++)
        {
            var R = rotMats[i].SubMatrix(0, 3, 0, 3);
            var t = rotMats[i].SubMatrix(0, 3, 3, 1);
            var J_i = J.Row(i).ToColumnMatrix();
            var offset = R * J_i;
            rotMats[i].SetSubMatrix(0, 3, t - offset);
        }

        return rotMats;
    }


    public void CalcVertexGlobal(Coeffs _coeffs, DenseMatrix[] rotMats, Vector3[] _vertex, float[] _beta, float[] _pose, float[] _trans)
    {
        var v_shaped = _coeffs.v_template.Clone();
        for (int i = 0; i < numBeta; i++)
        {
            v_shaped = v_shaped.Add(_coeffs.shapedirs[i].Multiply(_beta[i]));
        }

        int index = 0;
        for (int i = 1; i < numJoint; i++)
        {
            var R = RodriguesToMatrix(_pose.AsSpan(3 * i));
            var diff = R - DenseMatrix.CreateIdentity(3);
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    v_shaped = v_shaped.Add(_coeffs.posedirs[index++].Multiply(diff[r, c]));
        }
        CalcVertexGlobal2(_coeffs, rotMats, v_shaped, _vertex, _beta, _pose, _trans);
    }

    public void CalcVertexGlobal2(Coeffs _coeffs, DenseMatrix[] rotMats, Matrix<float> v_shaped, Vector3[] _vertex, float[] _beta, float[] _pose, float[] _trans)
    {
        var matx = DenseMatrix.Create(4, 4, 0f);
        for (int i = 0; i < _vertex.Length; i++)
        {
            matx.Clear();
            for (int j = 0; j < numJoint; j++)
            {
                float w = _coeffs.weights[i, j];
                if (w != 0f)
                    matx.Map2((x, y) => x + w * y, rotMats[j], matx);
            }

            var R = matx.SubMatrix(0, 3, 0, 3);
            var t = matx.SubMatrix(0, 3, 3, 1);
            var v = v_shaped.Row(i).ToColumnMatrix();

            var transformed = R * v + t;

            _vertex[i] = new Vector3(-transformed[0, 0], transformed[1, 0], transformed[2, 0]) * scaleUnity;
            //_vertex[3 * i + 1] = transformed[1, 0] * scaleUnity;
            //_vertex[3 * i + 2] = transformed[2, 0] * scaleUnity;
        }
    }

    public void CalcVertexGlobalForShapedirs(Coeffs _coeffs,  Vector3[] _vertex, float[] _beta, float[] _pose,float[] _trans, float[] _coeffPose)
    {
        var v_shaped = _coeffs.v_template.Clone();
        for (int i = 0; i < numBeta; i++)
        {
            v_shaped = v_shaped.Add(_coeffs.shapedirs[i].Multiply(_beta[i]));
        }
        var J = _coeffs.J_regressor * v_shaped;

        for (int i = 0; i < degElemJoint; i++)
        {
             v_shaped = v_shaped.Add(_coeffs.posedirs[i].Multiply(_coeffPose[i]));
        }

        var rotMats = makeRotMats(_coeffs, J, _beta, _pose, _trans);
        CalcVertexGlobal2(_coeffs, rotMats, v_shaped, _vertex, _beta, _pose, _trans);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var min = new Vector3(minRegion.x, minRegion.y, minRegion.z);
        var max = new Vector3(maxRegion.x, maxRegion.y, maxRegion.z);
        Gizmos.DrawWireCube((min + max) / 2.0f, max - min);
    }

}
