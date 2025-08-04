using UnityEngine;
using DG.Tweening;

public class VFXController : MonoBehaviour
{
    [Header("VFX Containers")]
    public GameObject perfectContainer;
    public GameObject goodContainer;
    public GameObject okContainer;
    public GameObject wrongContainer;
    
    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip perfectSound;
    public AudioClip goodSound;
    public AudioClip okSound;
    public AudioClip wrongSound;
    public AudioClip correctSound;
    
    [Header("Background Ambiance")]
    public AudioSource ambianceAudioSource;
    public AudioClip backgroundAmbianceClip;
    public float ambianceFadeOutDuration = 3f;
    
    [Header("VFX Settings")]
    public float vfxDisplayDuration = 1.5f;
    public float vfxFadeOutDuration = 0.5f;
    
    private Tween currentVFXTween;
    private Tween ambianceFadeTween;
    private bool isAmbiancePlaying = false;
    private float initialAmbianceVolume;
    
    public void ShowPerfectFeedback(Vector3? position = null)
    {
        //PlayAudioFeedback(perfectSound);
        ShowVFXContainer(perfectContainer, position);
    }
    
    public void ShowGoodFeedback(Vector3? position = null)
    {
       // PlayAudioFeedback(goodSound);
        ShowVFXContainer(goodContainer, position);
    }
    
    public void ShowOkFeedback(Vector3? position = null)
    {
        //PlayAudioFeedback(okSound);
        ShowVFXContainer(okContainer, position);
    }
    
    public void ShowWrongFeedback(Vector3? position = null)
    {
        PlayAudioFeedback(wrongSound);
        ShowVFXContainer(wrongContainer, position);
    }
    
    public void PlayCorrectSound()
    {
        PlayAudioFeedback(correctSound);
    }
    
    private void ShowVFXContainer(GameObject container, Vector3? position = null)
    {
        if (container == null) return;
        
        // Kill any existing VFX animation
        if (currentVFXTween != null)
        {
            currentVFXTween.Kill();
        }
        
        // Hide all containers first
        HideAllContainers();
        
        // Show the target container
        container.SetActive(true);
        
        // Set position if provided, otherwise use current position
        if (position.HasValue)
        {
            container.transform.position = position.Value;
        }
        
        // Animate in with scale effect
        container.transform.localScale = Vector3.zero;
        
        Sequence vfxSequence = DOTween.Sequence();
        
        // Scale in effect
        vfxSequence.Append(container.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
        
        // Hold for duration
        vfxSequence.AppendInterval(vfxDisplayDuration);
        
        // Scale out effect
        vfxSequence.Append(container.transform.DOScale(Vector3.zero, vfxFadeOutDuration).SetEase(Ease.InBack));
        
        // Hide container when done
        vfxSequence.AppendCallback(() => {
            container.SetActive(false);
        });
        
        currentVFXTween = vfxSequence;
        vfxSequence.Play();
    }
    
    private void HideAllContainers()
    {
        if (perfectContainer != null) perfectContainer.SetActive(false);
        if (goodContainer != null) goodContainer.SetActive(false);
        if (okContainer != null) okContainer.SetActive(false);
        if (wrongContainer != null) wrongContainer.SetActive(false);
    }
    
    public void HideAllVFX()
    {
        if (currentVFXTween != null)
        {
            currentVFXTween.Kill();
        }
        
        HideAllContainers();
    }
    
    private void PlayAudioFeedback(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void Start()
    {
        // Cache the initial ambiance volume from the AudioSource component
        if (ambianceAudioSource != null)
        {
            initialAmbianceVolume = ambianceAudioSource.volume;
        }
        
        StartBackgroundAmbiance();
    }
    
    public void StartBackgroundAmbiance()
    {
        if (ambianceAudioSource != null && backgroundAmbianceClip != null && !isAmbiancePlaying)
        {
            ambianceAudioSource.clip = backgroundAmbianceClip;
            ambianceAudioSource.loop = true;
            ambianceAudioSource.volume = initialAmbianceVolume; // Use cached initial volume
            ambianceAudioSource.Play();
            isAmbiancePlaying = true;
        }
    }
    
    public void FadeOutBackgroundAmbiance()
    {
        if (ambianceAudioSource != null && isAmbiancePlaying)
        {
            // Kill any existing fade tween
            if (ambianceFadeTween != null)
            {
                ambianceFadeTween.Kill();
            }
            
            // Fade out the ambiance volume
            ambianceFadeTween = ambianceAudioSource.DOFade(0f, ambianceFadeOutDuration)
                .OnComplete(() => {
                    ambianceAudioSource.Stop();
                    isAmbiancePlaying = false;
                });
        }
    }
    
    public void StopBackgroundAmbiance()
    {
        if (ambianceAudioSource != null)
        {
            if (ambianceFadeTween != null)
            {
                ambianceFadeTween.Kill();
            }
            ambianceAudioSource.Stop();
            isAmbiancePlaying = false;
        }
    }
    
    void OnDestroy()
    {
        if (currentVFXTween != null)
        {
            currentVFXTween.Kill();
        }
        
        if (ambianceFadeTween != null)
        {
            ambianceFadeTween.Kill();
        }
    }
} 