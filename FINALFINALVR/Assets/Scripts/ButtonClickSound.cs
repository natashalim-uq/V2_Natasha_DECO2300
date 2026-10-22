using UnityEngine;
using UnityEngine.UI;

// Plays a click sound whenever any button in the scene is pressed.
// Put it on one object (e.g. EventSystem) and drag the click sound into Click.
// WHY: in VR there is no physical click when you poke a floating button, so a sound
// confirms the press. Doing it in one script means I don't have to wire a sound to every button by hand.
public class ButtonClickSound : MonoBehaviour
{
    // The sound file to play (dragged in from the Inspector)
    public AudioClip click;

    // [Range] turns this into a slider in the Inspector, so the volume can only be 0 to 1
    [Range(0f, 1f)]
    public float volume = 0.6f;

    // The "speaker" that plays the click. Private because only this script uses it.
    private AudioSource source;

    // Start runs once, on the first frame the object is active
    private void Start()
    {
        // Make a speaker on this object in code, so there's nothing extra to set up in the scene
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;   // don't play anything by itself when the scene loads
        source.spatialBlend = 0f; // same volume wherever you are (2D sound, not coming from a point in the room)

        // Find every Button in the scene and tell it: "when you're clicked, also call PlayClick".
        // Includes buttons on panels that are hidden at the start (FindObjectsInactive.Include),
        // otherwise buttons on panels that appear later would be silent.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        // PlayOneShot lets clicks overlap if two happen quickly, instead of cutting each other off.
        // The null check stops an error if no sound was dragged in.
        if (click != null) source.PlayOneShot(click, volume);
    }
}
