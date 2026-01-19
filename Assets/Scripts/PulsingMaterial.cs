using UnityEngine;
using System.Collections;

public class ColorPulse : MonoBehaviour
{
    public float transitionSpeed = 1.5f;

    private Renderer rend;

    private Color red = new Color(1f, 0f, 0f);
    private Color darkRed = new Color(0.4f, 0f, 0f);
    private Color black = Color.black;

    void Start()
    {
        rend = GetComponent<Renderer>();
        StartCoroutine(ColorLoop());
    }

    IEnumerator ColorLoop()
    {
        while (true)
        {
            yield return StartCoroutine(LerpColor(red));
            yield return StartCoroutine(LerpColor(darkRed));
            yield return StartCoroutine(LerpColor(black));
            yield return StartCoroutine(LerpColor(red)); // tekrar açılsın
        }
    }

    IEnumerator LerpColor(Color target)
    {
        Color start = rend.material.color;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            rend.material.color = Color.Lerp(start, target, t);
            yield return null;
        }
    }
}
