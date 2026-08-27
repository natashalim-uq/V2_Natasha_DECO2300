using UnityEngine;

public class FilterCarousel : MonoBehaviour
{
    // Drag each filter icon GameObject into this array in the Inspector.
    // Only one will be shown/active at a time.
    public GameObject[] filterIcons;

    private int currentIndex = 0;

    private void Start()
    {
        ShowCurrent();
    }

    // Wire the Right Arrow button's On Click() to this
    public void NextFilter()
    {
        currentIndex++;
        if (currentIndex >= filterIcons.Length)
        {
            currentIndex = 0; // wrap back to the start
        }
        ShowCurrent();
    }

    // Wire the Left Arrow button's On Click() to this
    public void PreviousFilter()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = filterIcons.Length - 1; // wrap to the end
        }
        ShowCurrent();
    }

    // Wire your Confirm/Next (proceed) button to this
    public void ConfirmSelection()
    {
        Debug.Log("Selected filter: " + filterIcons[currentIndex].name);
        // Staging environment goes here later — for now this just confirms
        // which filter was chosen, visible in the Console.
    }

    private void ShowCurrent()
    {
        // Turn all icons off, then turn on just the current one
        for (int i = 0; i < filterIcons.Length; i++)
        {
            filterIcons[i].SetActive(i == currentIndex);
        }
    }
}