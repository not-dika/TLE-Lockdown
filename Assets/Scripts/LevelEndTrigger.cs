using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportPad : MonoBehaviour
{
    // Name of the scene to load
    public string targetScene;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player touched the pad
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadSceneAsync(targetScene);
        }
    }
}