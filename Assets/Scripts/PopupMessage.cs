using TMPro;
using UnityEngine;
using System.Collections;

public class PopupMessage : MonoBehaviour
{
    public static PopupMessage Instance;

    public GameObject panel;
    public TMP_Text messageText;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(string message, float duration = 2f)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        panel.SetActive(true);
        messageText.text = message;

        yield return new WaitForSeconds(duration);

        panel.SetActive(false);
    }
}