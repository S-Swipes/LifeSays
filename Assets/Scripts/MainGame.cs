using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using System;

[System.Serializable]
public class GameSegment
{
    public string segmentName;
    public List<MusicalObjectControl> interactiveObjects = new List<MusicalObjectControl>();
    public float delayBetweenObjects = 1f;
    public bool isCompleted = false;
    
    [Header("Segment Timing")]
    public float segmentStartDelay = 0f;
    public float segmentEndDelay = 1f;
}

public class MainGame : MonoBehaviour
{
    [Header("Game Segments")]
    public List<GameSegment> gameSegments = new List<GameSegment>();
    
    [Header("Game Settings")]
    public float delayBetweenSegments = 2f;
    public bool autoProgressSegments = true;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    // Private variables
    private int currentSegmentIndex = 0;
    private int currentObjectIndex = 0;
    private bool isPlayingSegment = false;
    
    // Events
    public event Action<int> OnSegmentStarted;
    public event Action<int> OnSegmentCompleted;
    public event Action OnAllSegmentsCompleted;

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        if (debugMode)
            Debug.Log($"Initializing game with {gameSegments.Count} segments");
            
        // Initialize all segments
        for (int segmentIndex = 0; segmentIndex < gameSegments.Count; segmentIndex++)
        {
            var segment = gameSegments[segmentIndex];
            
            // Get unique musical objects in this segment to avoid duplicate click handlers
            var uniqueMusicalObjects = new HashSet<MusicalObjectControl>();
            foreach (var musicalObject in segment.interactiveObjects)
            {
                uniqueMusicalObjects.Add(musicalObject);
            }
            
            // Setup click handlers only for unique musical objects
            foreach (var musicalObject in uniqueMusicalObjects)
            {
                int capturedSegmentIndex = segmentIndex;
                
                musicalObject.OnClicked += () =>
                {
                    OnInteractiveObjectClicked(capturedSegmentIndex, musicalObject);
                };
            }
            
            segment.isCompleted = false;
        }
        
        // Start the first segment after a brief delay
        DOVirtual.DelayedCall(1f, () => StartSegment(0));
    }

    void StartSegment(int segmentIndex)
    {
        if (segmentIndex >= gameSegments.Count)
        {
            if (debugMode)
                Debug.Log("All segments completed!");
            OnAllSegmentsCompleted?.Invoke();
            return;
        }

        currentSegmentIndex = segmentIndex;
        currentObjectIndex = 0;
        isPlayingSegment = true;
        
        var segment = gameSegments[segmentIndex];
        
        if (debugMode)
            Debug.Log($"Starting segment {segmentIndex}: {segment.segmentName}");
            
        OnSegmentStarted?.Invoke(segmentIndex);
        
        // Start playing the segment sequence after the segment start delay
        DOVirtual.DelayedCall(segment.segmentStartDelay, () => PlaySegmentSequence(segmentIndex));
    }

    void PlaySegmentSequence(int segmentIndex)
    {
        var segment = gameSegments[segmentIndex];
        
        if (segment.interactiveObjects.Count == 0)
        {
            CompleteSegment(segmentIndex);
            return;
        }
        
        // Play each object in the segment with delays
        for (int i = 0; i < segment.interactiveObjects.Count; i++)
        {
            int objectIndex = i;
            float delay = i * segment.delayBetweenObjects;
            
            DOVirtual.DelayedCall(delay, () =>
            {
                if (currentSegmentIndex == segmentIndex && objectIndex < segment.interactiveObjects.Count)
                {
                    if (debugMode)
                        Debug.Log($"Playing object {objectIndex} in segment {segmentIndex}");
                        
                    segment.interactiveObjects[objectIndex].Play();
                }
            });
        }
    }

    void OnInteractiveObjectClicked(int segmentIndex, MusicalObjectControl clickedMusicalObject)
    {
        if (debugMode)
            Debug.Log($"Interactive object clicked: Segment {segmentIndex}, Musical Object {clickedMusicalObject.name}");
            
        // Handle click logic here - check if it's the correct sequence
        if (segmentIndex == currentSegmentIndex && isPlayingSegment)
        {
            var segment = gameSegments[segmentIndex];
            if (segment.interactiveObjects.Contains(clickedMusicalObject))
            {
                // Find the index of the clicked musical object in the segment's interactive objects
                int clickedObjectIndex = segment.interactiveObjects.IndexOf(clickedMusicalObject);

                // Check if this is the expected musical object at the current position in the sequence
                var expectedMusicalObject = segment.interactiveObjects[currentObjectIndex];
                
                if (clickedMusicalObject == expectedMusicalObject)
                {
                    // Correct sequence - advance to next expected position
                    currentObjectIndex++;
                    
                    if (debugMode)
                        Debug.Log($"Correct! Expected musical object at position {currentObjectIndex-1} clicked. Next expected position: {currentObjectIndex}");
                    
                                         // Check if this was the last object in the sequence
                     if (currentObjectIndex >= segment.interactiveObjects.Count)
                     {
                         // Give temporary feedback to the last musical object first
                         clickedMusicalObject.Play(true);
                         
                         if (debugMode)
                             Debug.Log($"Sequence completed! Making all musical objects permanently happy...");
                         
                         // Wait for temporary animation to complete, then permanently color all musical objects
                         DOVirtual.DelayedCall(0.2f, () =>
                        {
                            // Sequence completed successfully - permanently color ALL musical objects in this segment
                            foreach (var musicalObject in segment.interactiveObjects)
                            {
                                musicalObject.SetColored(permanent: true);
                            }
                            
                            if (debugMode)
                                Debug.Log($"Segment {segmentIndex} sequence completed! All musical objects permanently colored.");
                            
                            CompleteSegment(segmentIndex);
                        });
                    }
                    else
                    {
                        // Correct click but sequence not finished yet - give temporary feedback
                        clickedMusicalObject.Play(true);
                        
                        if (debugMode)
                            Debug.Log($"Correct click {clickedObjectIndex}, waiting for sequence completion...");
                    }
                }
                else
                {
                    // Wrong sequence - play wrong animations and reset sequence
                    
                    // Play wrong selected animation on the clicked musical object
                    clickedMusicalObject.PlayWrongSelected();
                    
                    // Play wrong reset animation on all other musical objects in the segment
                    for (int i = 0; i < segment.interactiveObjects.Count; i++)
                    {
                        if (segment.interactiveObjects[i] != clickedMusicalObject) // Skip the clicked musical object
                        {
                            segment.interactiveObjects[i].PlayWrongReset();
                        }
                    }
                    
                    currentObjectIndex = 0; // Reset sequence on wrong click
                    
                    if (debugMode)
                        Debug.Log($"Wrong! Clicked musical object {clickedObjectIndex}, but expected musical object at position {currentObjectIndex}. Sequence reset.");
                }
            }
        }
    }

    void CompleteSegment(int segmentIndex)
    {
        if (segmentIndex >= gameSegments.Count) return;
        
        var segment = gameSegments[segmentIndex];
        segment.isCompleted = true;
        isPlayingSegment = false;
        
        if (debugMode)
            Debug.Log($"Segment {segmentIndex} completed: {segment.segmentName}");
            
        OnSegmentCompleted?.Invoke(segmentIndex);
        
        // Move to next segment after delay
        if (autoProgressSegments)
        {
            DOVirtual.DelayedCall(segment.segmentEndDelay + delayBetweenSegments, () =>
            {
                StartSegment(segmentIndex + 1);
            });
        }
    }

    // Public methods for manual control
    public void StartNextSegment()
    {
        if (currentSegmentIndex < gameSegments.Count - 1)
        {
            StartSegment(currentSegmentIndex + 1);
        }
    }

    public void RestartCurrentSegment()
    {
        StartSegment(currentSegmentIndex);
    }

    public void RestartGame()
    {
        // Reset all segments
        foreach (var segment in gameSegments)
        {
            segment.isCompleted = false;
            foreach (var obj in segment.interactiveObjects)
            {
                obj.ResetState();
            }
        }
        
        currentSegmentIndex = 0;
        currentObjectIndex = 0;
        isPlayingSegment = false;
        
        DOVirtual.DelayedCall(0.5f, () => StartSegment(0));
    }

    // Getters for current state
    public int CurrentSegmentIndex => currentSegmentIndex;
    public bool IsPlayingSegment => isPlayingSegment;
    public GameSegment CurrentSegment => currentSegmentIndex < gameSegments.Count ? gameSegments[currentSegmentIndex] : null;
}
