using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using System;

[System.Serializable]
public class GameSegment
{
    public string segmentName;
    public List<FrogControl> interactiveObjects = new List<FrogControl>();
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
            
            // Setup click handlers for all interactive objects in this segment
            for (int objectIndex = 0; objectIndex < segment.interactiveObjects.Count; objectIndex++)
            {
                int capturedSegmentIndex = segmentIndex;
                int capturedObjectIndex = objectIndex;
                
                segment.interactiveObjects[objectIndex].OnClicked += () =>
                {
                    OnInteractiveObjectClicked(capturedSegmentIndex, capturedObjectIndex);
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

    void OnInteractiveObjectClicked(int segmentIndex, int objectIndex)
    {
        if (debugMode)
            Debug.Log($"Interactive object clicked: Segment {segmentIndex}, Object {objectIndex}");
            
        // Handle click logic here - check if it's the correct sequence
        if (segmentIndex == currentSegmentIndex && isPlayingSegment)
        {
            var segment = gameSegments[segmentIndex];
            if (objectIndex < segment.interactiveObjects.Count)
            {
                // Check if this is the next expected object in the sequence
                if (objectIndex == currentObjectIndex)
                {
                    // Correct sequence - advance to next expected object
                    currentObjectIndex++;
                    
                    if (debugMode)
                        Debug.Log($"Correct! Object {objectIndex} clicked in correct order. Next expected: {currentObjectIndex}");
                    
                    // Check if this was the last object in the sequence
                    if (currentObjectIndex >= segment.interactiveObjects.Count)
                    {
                        // Last frog clicked correctly - give temporary feedback first
                        segment.interactiveObjects[objectIndex].Play(true);
                        
                        if (debugMode)
                            Debug.Log($"Sequence completed! Coloring all frogs in segment {segmentIndex} after delay...");
                        
                        // Add delay before permanently coloring all frogs (wait for temporary animation to finish)
                        DOVirtual.DelayedCall(1.01f, () =>
                        {
                            // Sequence completed successfully - permanently color ALL frogs in this segment
                            foreach (var frog in segment.interactiveObjects)
                            {
                                frog.SetColored();
                            }
                            
                            if (debugMode)
                                Debug.Log($"Segment {segmentIndex} sequence completed! All frogs permanently colored.");
                            
                            CompleteSegment(segmentIndex);
                        });
                    }
                    else
                    {
                        // Correct click but sequence not finished yet - just temporarily show feedback
                        segment.interactiveObjects[objectIndex].Play(true);
                    }
                }
                else
                {
                    // Wrong sequence - only temporarily color the frog and reset sequence
                    segment.interactiveObjects[objectIndex].Play(true);
                    currentObjectIndex = 0; // Reset sequence on wrong click
                    
                    if (debugMode)
                        Debug.Log($"Wrong! Object {objectIndex} clicked, but expected {currentObjectIndex}. Sequence reset.");
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
