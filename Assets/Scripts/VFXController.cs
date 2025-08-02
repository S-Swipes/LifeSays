using UnityEngine;
using DG.Tweening;

public class VFXController : MonoBehaviour
{
    [Header("VFX Containers")]
    public GameObject perfectContainer;
    public GameObject goodContainer;
    public GameObject okContainer;
    public GameObject wrongContainer;
    
    [Header("VFX Settings")]
    public float vfxDisplayDuration = 1.5f;
    public float vfxFadeOutDuration = 0.5f;
    
    private Tween currentVFXTween;
    
    public void ShowPerfectFeedback(Vector3? position = null)
    {
        ShowVFXContainer(perfectContainer, position);
    }
    
    public void ShowGoodFeedback(Vector3? position = null)
    {
        ShowVFXContainer(goodContainer, position);
    }
    
    public void ShowOkFeedback(Vector3? position = null)
    {
        ShowVFXContainer(okContainer, position);
    }
    
    public void ShowWrongFeedback(Vector3? position = null)
    {
        ShowVFXContainer(wrongContainer, position);
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
    
    void OnDestroy()
    {
        if (currentVFXTween != null)
        {
            currentVFXTween.Kill();
        }
    }
} 