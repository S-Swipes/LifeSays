using DG.Tweening;
using UnityEngine;

public class GameSegmentLooper : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySegment(GameSegment segment)
    {
        // Play the segment
        Debug.Log("Playing segment: " + segment.segmentName + " with delay: " + segment.delayBetweenObjects);
        Sequence sequence = DOTween.Sequence();
        
        for (int i = 0; i < segment.interactiveObjects.Count; i++)
        {
            int index = i;  
            sequence.AppendCallback(() => {
                Debug.Log("Playing interactive object: " + index);    
                segment.interactiveObjects[index].Play(true);
            });
            sequence.AppendInterval(segment.delayBetweenObjects);
            
        }
        
        
        
        sequence.AppendCallback(() => {
            Debug.Log("Segment completed, looping...");
        });
        
        sequence.SetLoops(-1).Play();
    }
}
