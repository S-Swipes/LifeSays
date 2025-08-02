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
        Debug.Log("Playing segment: " + segment.segmentName + " with delay: " + segment.delayBetweenObjects + 
                  ", start delay: " + segment.segmentStartDelay + 
                  ", end delay: " + segment.segmentEndDelay + 
                  ", start offset: " + startOffset);
        loopSequence = DOTween.Sequence();
        
        // Add initial start delay from segment timing
        float totalStartDelay = segment.segmentStartDelay + startOffset;
        if (totalStartDelay > 0f)
        {
            loopSequence.AppendInterval(totalStartDelay);
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
        
        // Add ending delay from segment timing before looping
        if (segment.segmentEndDelay > 0f)
        {
            loopSequence.AppendInterval(segment.segmentEndDelay);
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
