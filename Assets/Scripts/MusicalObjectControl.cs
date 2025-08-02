using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class MusicalObjectControl : MonoBehaviour
{
    public GameObject idle;
    public GameObject active;
    public GameObject happy;
    public GameObject idleColored;
    public GameObject idleEmpty;
    public GameObject activeColored;
    public GameObject activeEmpty;
    private bool isColored = false;
    private Tween playingTween;
    public event Action OnClicked;  // 🔔 C# event
    public Animation Animation;

    
    void Start()
    {
        /*idle = transform.Find("VisualContainer/IdleContainer").gameObject;
        active = transform.Find("VisualContainer/ActiveContainer").gameObject;
        happy = transform.Find("VisualContainer/HappyContainer").gameObject;*/
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
    }

    public void Play(bool Colored = false, Action onComplete = null)
    {
        Debug.Log("Play colored: " + Colored);
        if (playingTween != null)
        {
            playingTween.Kill();

            //Play with colored or empty
            activeColored.SetActive(Colored);
            activeEmpty.SetActive(!Colored);
            
        }
        
        // ADD PLAYING SOUND
        active.SetActive(true);
        idle.SetActive(false);
        playingTween = DOVirtual.DelayedCall(1, () => 
        {
            ResetState();
            onComplete?.Invoke(); // Call the completion callback if provided
        });
    }

    public void PlayHappyTemporary()
    {
        // Kill any existing tween to prevent conflicts
        if (playingTween != null)
        {
            playingTween.Kill();
        }
        
        // Store current colored state to restore it later
        bool wasColored = isColored;
        
        // Play happy animation temporarily without changing permanent colored state
        happy.SetActive(true);
        idle.SetActive(false);
        active.SetActive(false);
        Animation.Play("Happy_General");
        
        // Reset after animation completes, restoring the previous colored state
        playingTween = DOVirtual.DelayedCall(1, () => {
            isColored = wasColored; // Restore previous state
            ResetState();
        });
    }

    public void SetColored(bool permanent = false)
    {
        // Kill any existing tween to prevent conflicts
        if (playingTween != null)
        {
            playingTween.Kill();
        }
        
        //Set the musical object to colored
        isColored = true;
        happy.SetActive(true);
        idle.SetActive(false);
        active.SetActive(false);
/*        activeColored.SetActive(true);
        activeEmpty.SetActive(false);
        idleColored.SetActive(true);
        idleEmpty.SetActive(false);
        */
        Animation.Play("Happy_General");
        
        if (!permanent)
        {
            playingTween = DOVirtual.DelayedCall(1, ResetState);
        }
        // If permanent is true, don't set up any reset tween - musical object stays happy forever

    }

    public void PlayWrongSelected()
    {
        // Kill any existing tween to prevent conflicts
        if (playingTween != null)
        {
            playingTween.Kill();
        }
        
        // Play wrong selected animation for the clicked wrong musical object
        Animation.Play("WrongSelected");
        
        // Reset after animation completes
        playingTween = DOVirtual.DelayedCall(1, ResetState);
    }

    public void PlayWrongReset()
    {
        // Kill any existing tween to prevent conflicts
        if (playingTween != null)
        {
            playingTween.Kill();
        }
        
        // Play wrong reset animation for other musical objects
        Animation.Play("WrongReset");
        
        // Reset after animation completes
        playingTween = DOVirtual.DelayedCall(1, ResetState);
    }

    public void ResetState()
    {
        //Reset the musical object to colored
        activeColored.SetActive(isColored);
        activeEmpty.SetActive(!isColored);
        idleColored.SetActive(isColored);
        idleEmpty.SetActive(!isColored);

        //Reset the musical object to idle
        idle.SetActive(true);
        active.SetActive(false);
        happy.SetActive(false);
    }
} 