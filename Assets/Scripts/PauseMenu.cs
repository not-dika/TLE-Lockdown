using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject SettingsMenuUI;
    [SerializeField] private GameObject playerObject;

    private PlayerInputHandler playerInputHandler;
    private PlayerController playerController;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    public bool IsPaused => isPaused;

    private void Start()
    {
        // Try to get components from the assigned Player Object
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
            playerInputHandler = playerObject.GetComponent<PlayerInputHandler>();
        }
        else
        {
            // Fallback: search for them automatically in the scene
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerObject = playerController.gameObject;
                playerInputHandler = playerController.GetComponent<PlayerInputHandler>();
            }
            else
            {
                playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
            }
        }

        // Ensure menu is closed on start
        if (pauseMenuUI != null && SettingsMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            SettingsMenuUI.SetActive(false);
        }
    }

    private void Update()
    {
        // Toggle pause menu with Escape key
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // If the player is currently using the keypad, let the keypad handle the Escape key first
            if (playerController != null && playerController.IsUsingKeypad)
            {
                return;
            }

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null && SettingsMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
            SettingsMenuUI.SetActive(false);
        }

        Time.timeScale = 1f;
        isPaused = false;

        // Restore cursor lock state based on player input handler settings
        if (playerInputHandler != null)
        {
            playerInputHandler.SetCursorLock(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        Time.timeScale = 0f;
        isPaused = true;

        // Unlock cursor for menu interaction
        if (playerInputHandler != null)
        {
            playerInputHandler.SetCursorLock(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void QuitToMainMenu()
    {
        // Reset timeScale so the loaded scene does not start frozen
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
