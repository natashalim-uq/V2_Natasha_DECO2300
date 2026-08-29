using UnityEngine;

public class FilterCarousel : MonoBehaviour
{
    // Filter icon GameObject array cycles through carousel, only one icon is visible at a time
    public GameObject[] filterIcons;

    // Tracks the index of the currently displayed filter icon
    private int currentIndex = 0;

    private void Start()
    {
        // On scene start, make sure only the first filter icon is visible
        ShowCurrent();
    }

    // Wire the Right Arrow button's On Click() to this
    public void NextFilter()
    {
        currentIndex++; 

        // Wrap back to the beginning on last icon
        if (currentIndex >= filterIcons.Length)
        {
            currentIndex = 0;
        }

        ShowCurrent(); // Refresh which icon is visible
    }

    // Wire the Left Arrow button's On Click() to this
    public void PreviousFilter()
    {
        currentIndex--; // Move backward one filter

        // Wrap around to end on the last icon
        if (currentIndex < 0)
        {
            currentIndex = filterIcons.Length - 1;
        }

        ShowCurrent(); // Refresh which icon is visible
    }

    // Wire your Confirm/Next (proceed) button to this
    public void ConfirmSelection()
    {
        // Log  name of the  selected filter icon in the Console.
        Debug.Log("Selected filter: " + filterIcons[currentIndex].name);
    }

    // Shows only the icon at currentIndex, hides all others
    private void ShowCurrent()
    {
        for (int i = 0; i < filterIcons.Length; i++)
        {
            // SetActive(true) only for the matching index, false for everything else
            filterIcons[i].SetActive(i == currentIndex);
        }
    }
}