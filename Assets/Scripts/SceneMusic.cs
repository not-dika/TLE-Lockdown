using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField] private bool fade = true;

    private void Start()
    {
        if (fade)
            MusicManager.Instance.ChangeMusic(music);
        else
            MusicManager.Instance.PlayMusic(music);
    }
}