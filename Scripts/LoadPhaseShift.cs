//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.IO;

//public class LoadPhaseShift : MonoBehaviour
//{

//    public string csvPhaseShift;
//    List<List<float>> phaseShift = new List<List<float>>();

//    void Start()
//    {
//        if (!AUTDController.instance.isopen) return;
//        var idDevice = new List<int>() { 16, 9, 5, 20, 13, 8, 4, 17, 12, 10, 1, 19, 15, 7, 6, 18, 14, 3, 11, 2 };
//        if (csvPhaseShift != null && csvPhaseShift != "")
//            ReadPhaseFromCsv(csvPhaseShift, idDevice);

//        AUTDController.instance.SendPhaseCorrection(false, phaseShift);

//        //AUTDController.instance.AM(150);
//        //AUTDController.instance.Focus(new Vector3(0, 0, -0.526f));
//    }


//    void ReadPhaseFromCsv(string csv, List<int> idDevice)
//    {
//        string fullPath = System.IO.Path.GetFullPath(Application.streamingAssetsPath + "/" + csv);
//        StreamReader sr = new StreamReader(@fullPath);
//        UnityEngine.Debug.Log(fullPath);

//        phaseShift.Clear();
//        for (int i = 0; i < 20; i++)
//        {
//            phaseShift.Add(new List<float>());
//        }

//        string line = sr.ReadLine();
//        while (!sr.EndOfStream)
//        {
//            line = sr.ReadLine();
//            string[] values = line.Split(',');

//            List<string> lists = new List<string>();
//            lists.AddRange(values);

//            phaseShift[idDevice[int.Parse(lists[0]) - 1] - 1].Add(float.Parse(lists[5]));
//        }

//        for (int i = 0; i < phaseShift.Count; i++)
//        {
//            for (int j = 0; j < phaseShift[i].Count; j++)
//            {
//                UnityEngine.Debug.Log(i + ", " + j + ": " + phaseShift[i][j]);
//            }
//        }
//    }

//    //void Update()
//    //{
//    //    if (Input.GetKeyDown(KeyCode.Space))
//    //    {
//    //        if (AUTDController.instance.frequencyAM != 0)
//    //            AUTDController.instance.AM(0);
//    //        else
//    //        {
//    //            AUTDController.instance.AM(150);
//    //        }
//    //    }
//    //    if (Input.GetKeyDown(KeyCode.Alpha1))
//    //    {
//    //        AUTDController.instance.SendPhaseCorrection(true, phaseShift);
//    //    }
//    //    if (Input.GetKeyDown(KeyCode.Alpha2))
//    //    {
//    //        AUTDController.instance.SendPhaseCorrection(false, phaseShift);
//    //    }
//    //    if (Input.GetKeyDown(KeyCode.Alpha0))
//    //    {
//    //        AUTDController.instance.ResetPhaseCorrection();
//    //    }

//    //}
//}
