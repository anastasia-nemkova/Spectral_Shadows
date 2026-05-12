using UnityEngine;
using TMPro;
using System.Collections;

public class DeathMessageUI : MonoBehaviour
{
    private TextMeshProUGUI text;
    private CanvasGroup canvasGroup;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        
        if (GetComponent<CanvasGroup>() == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        else
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        HideMessage();
    }

    public void ShowMessage(string message)
    {
        text.text = message;
        
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void HideMessage()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ShowMessageWithDelay(string message, float delay, System.Action onFinished)
    {
        StartCoroutine(ShowAndHideCoroutine(message, delay, onFinished));
    }

    private IEnumerator ShowAndHideCoroutine(string message, float delay, System.Action onFinished)
    {
        ShowMessage(message);
        
        yield return new WaitForSeconds(delay);
        
        HideMessage();
        
        if (onFinished != null)
        {
            onFinished.Invoke();
        }
    }
}