using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu UI to Disable")]
    [SerializeField] private GameObject menuPanel; // Drag your main menu panel/canvas here

    private void Awake()
    {
        // Freeze game time immediately when the scene starts
        Time.timeScale = 0f;
    }

    // Call this from your Start / Play Button
    public void StartGame()
    {
        // Unfreeze time so physics, movement, and game loops resume
        Time.timeScale = 1f;

        // Disable the main menu UI
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        else
        {
            // Fallback: disables whatever GameObject this script is attached to
            gameObject.SetActive(false);
        }
    }

    // Call this from your Exit / Quit Button
    public void ExitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        Debug.Log("Exit Game button pressed.");
        #endif
    }
}