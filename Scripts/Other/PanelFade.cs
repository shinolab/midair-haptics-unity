using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelFade : MonoBehaviour
{
    private Image panel;
    public float fadeDuration = 1.0f;

    private void Awake()
    {
        panel = GetComponent<Image>();
    }

    public IEnumerator FadeOut()
    {
        panel.enabled = true;
        float elapsedTime = 0.0f;
        Color startColor = new Color(0, 0, 0, 0);
        Color endColor = new Color(0, 0, 0, 1.0f);
        panel.color = startColor;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            panel.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        panel.color = endColor;
    }

    public IEnumerator FadeIn()
    {
        panel.enabled = true;
        float elapsedTime = 0.0f;
        Color startColor = new Color(0, 0, 0, 1.0f);
        Color endColor = new Color(0, 0, 0, 0);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            panel.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        panel.color = endColor;
    }
}
