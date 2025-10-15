using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegisrateCameraPosition : MonoBehaviour
{
    public Preprocessor preprocessor = null;
    List<Matrix4x4> matrix = new List<Matrix4x4>();
    int idCamera = 1;
    public float stepX = 0.01f;
    public float stepTheta = 0.01f;

    Vector3 transX, transY, transZ;
    Matrix4x4 rotX, rotY, rotZ;


    void Start()
    {
        RealsenseController.instance.getMatrix(matrix);
    }

    private void OnValidate()
    {
        rotX = Matrix4x4.Rotate(new Quaternion(Mathf.Sin(stepTheta / 2), 0, 0, Mathf.Cos(stepTheta / 2)));
        rotY = Matrix4x4.Rotate(new Quaternion(0, Mathf.Sin(stepTheta / 2), 0, Mathf.Cos(stepTheta / 2)));
        rotZ = Matrix4x4.Rotate(new Quaternion(0, 0, Mathf.Sin(stepTheta / 2), Mathf.Cos(stepTheta / 2)));
        transX = new Vector3(stepX, 0, 0);
        transY = new Vector3(0, stepX, 0);
        transZ = new Vector3(0, 0, stepX);
    }

    void Update()
    {
        List<Matrix4x4> tmp = new List<Matrix4x4>();
        RealsenseController.instance.getMatrix(tmp);
        KeyInput(tmp);
        if (preprocessor != null){
            for (int i = 0; i < tmp.Count; i++)
            {
                if (tmp[i] != matrix[i])
                {
                    preprocessor.SetParameter();
                    break;
                }
            }
        }
        matrix = tmp;
    }

    public void Test()
    {
        Debug.Log("dddddddddddd");

    }

    void MoveCameraIndividual(ref Matrix4x4 mat, ref Vector3 translate, ref Matrix4x4 rotate)
    {
        mat = rotate * mat;
        mat[0, 3] += translate[0];
        mat[1, 3] += translate[1];
        mat[2, 3] += translate[2];
    }


    void MoveCamera(List<Matrix4x4> matrix, Vector3 translate, Matrix4x4 rotate)
    {
        if(idCamera == 0)
        {
            for (int i = 0; i < matrix.Count; i++)
            {
                var mat = matrix[i];
                MoveCameraIndividual(ref mat, ref translate, ref rotate);
                matrix[i] = mat;
            }

        }
        else if (idCamera <= matrix.Count)
        {
            var mat = matrix[idCamera - 1];
            MoveCameraIndividual(ref mat, ref translate, ref rotate);
            matrix[idCamera - 1] = mat;
        }
        RealsenseController.instance.setMatrix(matrix);
    }


    void KeyInput(List<Matrix4x4> matrix)
    {
        if (Input.GetKey(KeyCode.Alpha0))
        {
            idCamera = 0;
            Debug.Log("idCamera: All");
        }
        if (Input.GetKey(KeyCode.Alpha1))
        {
            idCamera = 1;
            Debug.Log("idCamera: " + idCamera);
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            idCamera = 2;
            Debug.Log("idCamera: " + idCamera);
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            idCamera = 3;
            Debug.Log("idCamera: " + idCamera);
        }
        if (Input.GetKey(KeyCode.Alpha4))
        {
            idCamera = 4;
            Debug.Log("idCamera: " + idCamera);
        }
        if (Input.GetKey(KeyCode.Alpha5))
        {
            idCamera = 5;
            Debug.Log("idCamera: " + idCamera);
        }
        if (Input.GetKey(KeyCode.A))
        {
            MoveCamera(matrix, transX, Matrix4x4.identity);
        }
        if (Input.GetKey(KeyCode.D))
        {
            MoveCamera(matrix, -transX, Matrix4x4.identity);
        }
        if (Input.GetKey(KeyCode.W))
        {
            MoveCamera(matrix, transY, Matrix4x4.identity);
        }
        if (Input.GetKey(KeyCode.S))
        {
            MoveCamera(matrix, -transY, Matrix4x4.identity);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            MoveCamera(matrix, -transZ, Matrix4x4.identity);
        }
        if (Input.GetKey(KeyCode.E))
        {
            MoveCamera(matrix, transZ, Matrix4x4.identity);
        }
        if (Input.GetKey(KeyCode.H))
        {
            MoveCamera(matrix, Vector3.zero, rotZ.transpose);
        }
        if (Input.GetKey(KeyCode.K))
        {
            MoveCamera(matrix, Vector3.zero, rotZ);
        }
        if (Input.GetKey(KeyCode.U))
        {
            MoveCamera(matrix, Vector3.zero, rotX.transpose);
        }
        if (Input.GetKey(KeyCode.J))
        {
            MoveCamera(matrix, Vector3.zero, rotX);
        }
        if (Input.GetKey(KeyCode.I))
        {
            MoveCamera(matrix, Vector3.zero, rotY.transpose);
        }
        if (Input.GetKey(KeyCode.Y))
        {
            MoveCamera(matrix, Vector3.zero, rotY);
        }
    }
}
