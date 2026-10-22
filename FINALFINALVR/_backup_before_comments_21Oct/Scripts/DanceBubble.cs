using System.Collections;
using UnityEngine;

// One bubble in Learn a Dance, spawned by DanceManager.
// Hit it by touching it with a hand (anything tagged "Hand" with a trigger collider).
// Feedback: glows and grows while approaching, pops green with a sound on a hit, turns red and shrinks on a miss.
public class DanceBubble : MonoBehaviour
{
    public enum Visibility { Full, Outline, Hidden }

    [Tooltip("The bubble's mesh. Its material must be transparent (URP Unlit or Lit, Surface Type: Transparent).")]
    public Renderer body;

    [Tooltip("Full size in metres")]
    public float size = 0.25f;

    public Color approachColor = new Color(1f, 1f, 1f, 0.3f);
    public Color readyColor = new Color(0.3f, 0.8f, 1f, 0.85f);
    public Color hitColor = new Color(0.3f, 1f, 0.4f, 1f);
    public Color missColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Tooltip("How see-through bubbles are in Mastery's outline stage (0 = invisible, 1 = normal)")]
    [Range(0f, 1f)]
    public float outlineAlpha = 0.2f;

    public AudioClip hitSound;
    public AudioClip missSound;

    [Range(0f, 1f)]
    public float missVolume = 0.4f;

    [Tooltip("Optional particle effect spawned on a hit")]
    public GameObject popEffect;

    private DanceManager manager;
    private float hitTime;
    private Visibility visibility;
    private bool finished;
    private int handsInside;
    private Material material;

    public void Init(DanceManager owner, float time, Visibility vis)
    {
        manager = owner;
        hitTime = time;
        visibility = vis;

        if (body == null) body = GetComponentInChildren<Renderer>();
        material = body.material;
        body.enabled = visibility != Visibility.Hidden;
        transform.localScale = Vector3.one * size * 0.3f;
    }

    private void Update()
    {
        if (finished || manager == null) return;

        float now = manager.SongTime;
        if (now < hitTime)
        {
            // Approach glow: small and faint, growing and pulsing until it's due
            float progress = Mathf.Clamp01(1f - (hitTime - now) / manager.leadTime);
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 12f);
            transform.localScale = Vector3.one * size * Mathf.Lerp(0.3f, 1f, progress) * pulse;
            SetColor(Dim(approachColor));
        }
        else
        {
            transform.localScale = Vector3.one * size;
            SetColor(Dim(readyColor));
            if (now > hitTime + manager.hitWindow)
            {
                StartCoroutine(Finish(false));
                return;
            }
        }

        if (handsInside > 0 && now >= hitTime - manager.earlyWindow)
        {
            StartCoroutine(Finish(true));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand")) handsInside++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand")) handsInside = Mathf.Max(0, handsInside - 1);
    }

    private IEnumerator Finish(bool hit)
    {
        finished = true;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (hit)
        {
            manager.BubbleHit(this);
            body.enabled = true; // always show the pop, even in Mastery
            if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);
            if (popEffect != null) Instantiate(popEffect, transform.position, Quaternion.identity);
        }
        else
        {
            manager.BubbleMissed(this);
            if (missSound != null) AudioSource.PlayClipAtPoint(missSound, transform.position, missVolume);
        }

        // Hit: grow and fade out green. Miss: turn red and shrink away.
        Color color = hit ? hitColor : missColor;
        float duration = hit ? 0.2f : 0.35f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = hit ? startScale * 1.6f : Vector3.zero;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, k);
            Color c = color;
            if (hit) c.a = color.a * (1f - k);
            SetColor(c);
            yield return null;
        }
        Destroy(gameObject);
    }

    private Color Dim(Color c)
    {
        if (visibility == Visibility.Outline) c.a *= outlineAlpha;
        return c;
    }

    private void SetColor(Color c)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
        else material.color = c;
    }
}
