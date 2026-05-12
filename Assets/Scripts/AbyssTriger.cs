using UnityEngine;
using UnityEngine.SceneManagement;

public class AbyssTrigger : MonoBehaviour
{
    public float deathHeight = -20f;
    
    [Header("UI")]
    public DeathMessageUI deathMessageUI;
    public float messageDelay = 2f;

    private void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            if (player.transform.position.y < deathHeight)
            {
                OnPlayerFall();
            }
        }
    }

    private void OnPlayerFall()
    {
        Debug.Log("Падение в бездну! Рестарт...");

        PlayerAudioManager audio = FindFirstObjectByType<PlayerAudioManager>();
        if (audio != null) audio.PlayFall();
        
        if (deathMessageUI != null)
        {
            deathMessageUI.ShowMessageWithDelay(
                "Вы упали. \nПридется начать сначала....", 
                messageDelay, 
                () => {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            );
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerFall();
        }
    }
}