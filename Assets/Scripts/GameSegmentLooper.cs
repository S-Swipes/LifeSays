using DG.Tweening;
using UnityEngine;

public class GameSegmentLooper : MonoBehaviour
{
    private Sequence loopSequence;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySegment(GameSegment segment, float startOffset = 0f)
    {
        // Stop any existing loop
        if (loopSequence != null)
        {
            loopSequence.Kill();
        }
        
        // Play the segment
        Debug.Log("Playing segment: " + segment.segmentName + " with delay: " + segment.delayBetweenObjects + ", start offset: " + startOffset);
        loopSequence = DOTween.Sequence();
        
        // Add initial offset if provided to synchronize with reveal sequence
        if (startOffset > 0f)
        {
            loopSequence.AppendInterval(startOffset);
        }
        
        for (int i = 0; i < segment.interactiveObjects.Count; i++)
        {
            int index = i;  
            loopSequence.AppendCallback(() => {
                Debug.Log("Playing interactive object: " + index);    
                segment.interactiveObjects[index].Play(true);
            });
            loopSequence.AppendInterval(segment.delayBetweenObjects);
        }
        
        loopSequence.AppendCallback(() => {
            Debug.Log("Segment completed, looping...");
        });
        
        loopSequence.SetLoops(-1).Play();
    }
    
    public void StopLoop()
    {
        if (loopSequence != null)
        {
            loopSequence.Kill();
            loopSequence = null;
        }
    }
    
    void OnDestroy()
    {
        StopLoop();
    }
}
