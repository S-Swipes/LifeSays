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
        Debug.Log("Playing segment: " + segment.segmentName);
    }
}
