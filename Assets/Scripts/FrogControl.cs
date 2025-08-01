using DG.Tweening;
using UnityEngine;

public class FrogControl : MonoBehaviour
{
    private GameObject idle;
    private GameObject active;
    private GameObject happy;
    private Tween playingTween;
    
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
