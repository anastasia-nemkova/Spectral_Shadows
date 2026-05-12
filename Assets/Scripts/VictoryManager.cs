using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class VictoryManager : MonoBehaviour
{
    [Header("Элементы UI")]
    public GameObject victoryPanel;
    public Button playAgainButton;

    [Header("Настройки")]
    public float fadeInDuration = 1.5f;

    private CanvasGroup canvasGroup;
    private bool hasShownVictory = false;

    void Start()
    {
        if (victoryPanel.GetComponent<CanvasGroup>() == null)
            canvasGroup = victoryPanel.AddComponent<CanvasGroup>();
        else
            canvasGroup = victoryPanel.GetComponent<CanvasGroup>();
            
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(RestartGame);
    }

    public void ShowVictory()
    {
        if (hasShownVictory) return;
        hasShownVictory = true;

        PlayerAudioManager audio = FindFirstObjectByType<PlayerAudioManager>();
        if (audio != null) audio.PlayVictory();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 0f;
        
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private void RestartGame()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}