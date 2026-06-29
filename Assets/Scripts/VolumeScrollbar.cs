using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VolumeScrollbar : MonoBehaviour
{
    private Scrollbar scrollbar;
    private MusicManager musicManager;

    void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
        musicManager = FindFirstObjectByType<MusicManager>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (musicManager != null)
        {
            scrollbar.onValueChanged.AddListener(musicManager.SetVolume);
            scrollbar.value = musicManager.GetVolume();
        }
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (musicManager != null)
        {
            scrollbar.onValueChanged.RemoveListener(musicManager.SetVolume);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (musicManager == null)
            musicManager = FindFirstObjectByType<MusicManager>();

        if (musicManager != null)
        {
            scrollbar.value = musicManager.GetVolume();
        }
    }
}