using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class FrogControl : MonoBehaviour
{
    private GameObject idle;
    private GameObject active;
    private GameObject happy;
    private Tween playingTween;
    public event Action OnClicked;  // 🔔 C# event

    
    void Start()
    {
        idle = transform.Find("VisualContainer/IdleContainer").gameObject;
        active = transform.Find("VisualContainer/ActiveContainer").gameObject;
        happy = transform.Find("VisualContainer/HappyContainer").gameObject;
        idle.SetActive(true);
        active.SetActive(false);
        happy.SetActive(false);
    }
    
    void Update()
    {
        
    }
    
    void OnMouseDown()
    {
        OnClicked?.Invoke();
        Play();
    }

    public void Play()
    {
        if (playingTween != null)
        {
            playingTween.Kill();
        }
        
        // ADD PLAYING SOUND
        active.SetActive(true);
        idle.SetActive(false);
        playingTween = DOVirtual.DelayedCall(1, ResetState);
    }

    public void ResetState()
    {
        idle.SetActive(true);
        active.SetActive(false);
    }
}
