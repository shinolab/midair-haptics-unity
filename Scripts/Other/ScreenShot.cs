
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift))
        {
            ScreenCapture.CaptureScreenshot(DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");
        }
        if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftShift))
        {
            StartCoroutine(CaptureWithAlpha());
        }
    }

    IEnumerator CaptureWithAlpha()
    {
        yield return new WaitForEndOfFrame();

        var tex = ScreenCapture.CaptureScreenshotAsTexture();

        var width = tex.width;
        var height = tex.height;
        var texAlpha = new Texture2D(width, height, TextureFormat.ARGB32, false);
        // Read screen contents into the texture
        texAlpha.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texAlpha.Apply();

        // Encode texture into PNG
        var bytes = texAlpha.EncodeToPNG();
        Destroy(tex);
        var filePath = Application.streamingAssetsPath + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        var directryPath = Application.streamingAssetsPath + "/";
        bool notDirectory = !Directory.Exists(directryPath);
        if (notDirectory)
        {
            Directory.CreateDirectory(directryPath);
        }

        File.WriteAllBytes(filePath, bytes);
    }
}
