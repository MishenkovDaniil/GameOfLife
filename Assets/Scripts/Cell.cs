using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class Cell : MonoBehaviour
{
    [Header("Configurable colors")]
    public Color deadColor;
    public Color whiteColor;
    public Color blackColor;

    private SpriteRenderer sr;

    [HideInInspector] public int x, y;

    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;

    private Coroutine currentAnimation;
    private Color targetColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

        if (sr != null)
            targetColor = sr.color;
    }

    /// <summary>
    /// Set visual state: 0 = dead, 1 = white, 2 = black
    /// </summary>
    public void SetVisualState(int newState)
    {
        if (sr == null) return;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        Color newColor = GetColorForState(newState);

        if (newState == 0)
            currentAnimation = StartCoroutine(AnimateFadeOut());
        else
            currentAnimation = StartCoroutine(AnimateFadeIn(newColor));
    }

    /// <summary>
    /// Set visual state immediately without animation (for initialization)
    /// </summary>
    public void SetVisualStateImmediate(int state)
    {
        if (sr == null) return;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        currentAnimation = null;

        sr.color = GetColorForState(state);
    }

    private Color GetColorForState(int state)
    {
        switch (state)
        {
            case 0:
                return deadColor;
            case 1:
                return whiteColor;
            case 2:
                return blackColor;
            default:
                return Color.magenta;
        }
    }

    IEnumerator AnimateFadeIn(Color targetFadeColor)
    {
        sr.color = new Color(targetFadeColor.r, targetFadeColor.g, targetFadeColor.b, 0f);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;

            Color currentColor = targetFadeColor;
            currentColor.a = progress;

            sr.color = currentColor;
            yield return null;
        }

        // finally
        sr.color = targetFadeColor;
        currentAnimation = null;
    }

    IEnumerator AnimateFadeOut()
    {
        float elapsed = 0f;
        Color startColor = sr.color;
        Color endColor = deadColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            Color currentColor = Color.Lerp(startColor, endColor, progress);
            currentColor.a = 1f;
            sr.color = currentColor;
            yield return null;
        }

        // finally
        sr.color = deadColor;
        currentAnimation = null;
    }
}
