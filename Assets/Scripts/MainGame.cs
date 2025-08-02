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
    private int currentRevealedLength = 1; // Simon Says: how many elements of the sequence are currently revealed
    private bool isPlayingSegment = false;
    private bool isWaitingForPlayerInput = false; // True when sequence finished playing and waiting for player
    
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
        currentRevealedLength = 1; // Simon Says: start with revealing only the first element
        isPlayingSegment = true;
        isWaitingForPlayerInput = false;
        
        var segment = gameSegments[segmentIndex];
        
        if (debugMode)
            Debug.Log($"Starting segment {segmentIndex}: {segment.segmentName} - Revealing first {currentRevealedLength} elements");
            
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
        
        // Simon Says: only play up to the current revealed length
        int elementsToPlay = Mathf.Min(currentRevealedLength, segment.interactiveObjects.Count);
        
        if (debugMode)
            Debug.Log($"Playing sequence: showing {elementsToPlay} out of {segment.interactiveObjects.Count} elements");
        
        // Play each object in the revealed portion of the sequence with delays
        for (int i = 0; i < elementsToPlay; i++)
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
        
        // After the sequence finishes playing, wait for player input
        float totalSequenceTime = (elementsToPlay - 1) * segment.delayBetweenObjects + 1f; // +1 for last object play time
        DOVirtual.DelayedCall(totalSequenceTime, () =>
        {
            isWaitingForPlayerInput = true;
            if (debugMode)
                Debug.Log($"Sequence finished playing. Waiting for player to repeat {elementsToPlay} elements.");
        });
    }

    void OnInteractiveObjectClicked(int segmentIndex, MusicalObjectControl clickedMusicalObject)
    {
        if (debugMode)
            Debug.Log($"Interactive object clicked: Segment {segmentIndex}, Musical Object {clickedMusicalObject.name}");
            
        // Simon Says: Only accept clicks when waiting for player input
        if (segmentIndex != currentSegmentIndex || !isPlayingSegment || !isWaitingForPlayerInput)
        {
            if (debugMode)
                Debug.Log("Click ignored - not waiting for player input");
            return;
        }

        var segment = gameSegments[segmentIndex];
        if (!segment.interactiveObjects.Contains(clickedMusicalObject))
            return;

        // Check if clicked object matches the expected object at current position
        var expectedMusicalObject = segment.interactiveObjects[currentObjectIndex];
        
        if (clickedMusicalObject == expectedMusicalObject)
        {
            // Correct click!
            currentObjectIndex++;
            clickedMusicalObject.Play(true); // Give feedback
            
            if (debugMode)
                Debug.Log($"Correct! Clicked object at position {currentObjectIndex-1}. Progress: {currentObjectIndex}/{currentRevealedLength}");
            
            // Check if player has completed the current revealed sequence
            if (currentObjectIndex >= currentRevealedLength)
            {
                // Player completed current revealed sequence successfully!
                if (currentRevealedLength >= segment.interactiveObjects.Count)
                {
                    // Entire segment sequence completed!
                    DOVirtual.DelayedCall(0.2f, () =>
                    {
                        // Permanently color all musical objects in this segment
                        foreach (var musicalObject in segment.interactiveObjects)
                        {
                            musicalObject.SetColored(permanent: true);
                        }
                        
                        if (debugMode)
                            Debug.Log($"Segment {segmentIndex} completed! All musical objects permanently colored.");
                        
                        CompleteSegment(segmentIndex);
                    });
                }
                                 else
                 {
                     // Capture current revealed length before incrementing
                     int completedSequenceLength = currentRevealedLength;
                     
                     // Successfully completed current revealed sequence - play happy anim on revealed objects
                     DOVirtual.DelayedCall(0.2f, () =>
                     {
                         // Play happy animation for the musical objects that were just successfully completed
                         for (int i = 0; i < completedSequenceLength; i++)
                         {
                             segment.interactiveObjects[i].PlayHappyTemporary();
                         }
                         
                         if (debugMode)
                             Debug.Log($"Revealed sequence of {completedSequenceLength} elements completed successfully! Playing happy animations.");
                     });
                     
                     // Increase revealed length and replay sequence
                     currentRevealedLength++;
                     currentObjectIndex = 0;
                     isWaitingForPlayerInput = false;
                     
                     if (debugMode)
                         Debug.Log($"Sequence completed! Revealing {currentRevealedLength} elements now.");
                     
                     // Wait a longer moment then replay with more elements revealed
                     DOVirtual.DelayedCall(2f, () => PlaySegmentSequence(segmentIndex));
                 }
            }
            // If not completed current sequence yet, just wait for next click
        }
        else
        {
            // Wrong click! Reset current attempt and replay current sequence
            clickedMusicalObject.PlayWrongSelected();
            
            // Play wrong reset animation on other objects
            for (int i = 0; i < segment.interactiveObjects.Count; i++)
            {
                if (segment.interactiveObjects[i] != clickedMusicalObject)
                {
                    segment.interactiveObjects[i].PlayWrongReset();
                }
            }
            
            currentObjectIndex = 0; // Reset current attempt
            isWaitingForPlayerInput = false;
            
            if (debugMode)
                Debug.Log($"Wrong! Expected object at position {currentObjectIndex}, got {clickedMusicalObject.name}. Replaying current sequence.");
            
            // Wait a moment then replay current sequence
            DOVirtual.DelayedCall(2f, () => PlaySegmentSequence(segmentIndex));
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
        currentRevealedLength = 1; // Simon Says: reset to reveal first element
        isPlayingSegment = false;
        isWaitingForPlayerInput = false;
        
        DOVirtual.DelayedCall(0.5f, () => StartSegment(0));
    }

    // Getters for current state
    public int CurrentSegmentIndex => currentSegmentIndex;
    public bool IsPlayingSegment => isPlayingSegment;
    public GameSegment CurrentSegment => currentSegmentIndex < gameSegments.Count ? gameSegments[currentSegmentIndex] : null;
}
