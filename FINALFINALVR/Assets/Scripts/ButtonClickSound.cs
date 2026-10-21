using UnityEngine;
using UnityEngine.UI;

// Plays a click sound whenever any button in the scene is pressed.
// Put it on one object (e.g. EventSystem) and drag the click sound into Click.
public class ButtonClickSound : MonoBehaviour
{
    public AudioClip click;

    [Range(0f, 1f)]
    public float volume = 0.6f;

    private AudioSource source;

    private void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // same volume wherever you are

        // Includes buttons on panels that are hidden at the start
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (click != null) source.PlayOneShot(click, volume);
    }
}
