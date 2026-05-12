using UnityEngine;
using System.Collections;

public class PlayerAudioManager : MonoBehaviour
{
    [Header("Источники звука")]
    [Tooltip("Основной источник для коротких звуков (шаги, прыжок, сбор)")]
    public AudioSource sfxSource;
    
    [Tooltip("Отдельный источник для победы (чтобы не прерывался)")]
    public AudioSource musicSource;

    [Header("Звуки")]
    public AudioClip[] footstepClips;
    public AudioClip jumpClip;
    public AudioClip collectClip;
    public AudioClip victoryClip;
    public AudioClip fallClip;

    [Header("Настройки")]
    [Tooltip("Как часто проигрывать шаги (в метрах)")]
    public float footstepDistance = 1.0f;
    
    [Tooltip("Громкость шагов")]
    [Range(0f, 1f)] public float footstepVolume = 0.4f;
    
    [Tooltip("Громкость остальных звуков")]
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    private CharacterController controller;
    private Vector3 lastFootstepPos;
    private bool isGroundedLastFrame;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastFootstepPos = transform.position;

        if (sfxSource == null) sfxSource = SetupSource("SFX_Source", 1f, false);
        if (musicSource == null) musicSource = SetupSource("Music_Source", 0.8f, false);
    }

    AudioSource SetupSource(string name, float volume, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        
        AudioSource source = go.AddComponent<AudioSource>();
        source.volume = volume;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        
        return source;
    }

    void Update()
    {
        if (controller == null) return;

        if (controller.isGrounded && isGroundedLastFrame)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastFootstepPos);
            
            if (distanceMoved >= footstepDistance)
            {
                PlayFootstep();
                lastFootstepPos = transform.position;
            }
        }
        isGroundedLastFrame = controller.isGrounded;
    }

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        PlayOneShot(sfxSource, clip, footstepVolume);
    }

    public void PlayJump()
    {
        PlayOneShot(sfxSource, jumpClip, sfxVolume);
    }

    public void PlayCollect()
    {
        PlayOneShot(sfxSource, collectClip, sfxVolume);
    }

    public void PlayVictory()
    {
        if (victoryClip != null && musicSource != null)
        {
            musicSource.clip = victoryClip;
            musicSource.Play();
        }
    }

    public void PlayFall()
    {
        PlayOneShot(sfxSource, fallClip, sfxVolume * 1.2f);
    }

    void PlayOneShot(AudioSource source, AudioClip clip, float volume)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip, volume);
    }
}