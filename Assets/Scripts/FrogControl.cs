using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class FrogControl : MonoBehaviour
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
        Play(true);
    }

    public void Play(bool Colored = false)
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
        playingTween = DOVirtual.DelayedCall(1, ResetState);
    }

    public void SetColored()
    {
        //Set the frog to colored
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
        playingTween = DOVirtual.DelayedCall(1, ResetState);

    }

    public void ResetState()
    {
        //Reset the frog to colored
        activeColored.SetActive(isColored);
        activeEmpty.SetActive(!isColored);
        idleColored.SetActive(isColored);
        idleEmpty.SetActive(!isColored);

        //Reset the frog to idle
        idle.SetActive(true);
        active.SetActive(false);
        happy.SetActive(false);
    }
}
