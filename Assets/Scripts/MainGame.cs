using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using System;
using Unity.Cinemachine;

[System.Serializable]
public class GameSegment
{
    public string segmentName;
    public List<MusicalObjectControl> interactiveObjects = new List<MusicalObjectControl>();
    public float delayBetweenObjects = 1f;
    public bool isCompleted = false;
    public bool isLooping = false; // Track if this segment is currently looping
    
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
    
    [Header("Master Timing")]
    public float masterBeatInterval = 1f; // Master timing grid for synchronization
    
    [Header("Camera Settings")]
    public CinemachineMixingCamera mixingCamera;
    public float cameraTransitionDuration = 2f;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    // Private variables
    private int currentSegmentIndex = 0;
    private int currentObjectIndex = 0;
    private int currentRevealedLength = 1; // Simon Says: how many elements of the sequence are currently revealed
    private bool isPlayingSegment = false;
    private bool isWaitingForPlayerInput = false; // True when sequence finished playing and waiting for player
    private int currentCameraIndex = 0;
    private float gameStartTime; // Track when the game started for master timing
    private Dictionary<int, Sequence> segmentLoops = new Dictionary<int, Sequence>(); // Track individual segment loops
    
    // Events
    public event Action<int> OnSegmentStarted;
    public event Action<int> OnSegmentCompleted;
    public event Action OnAllSegmentsCompleted;

    void Start()
    {
        InitializeCamera();
        InitializeGame();
    }
    
    void InitializeCamera()
    {
        if (mixingCamera != null)
        {
            // Set initial camera weights - start with camera 0
            for (int i = 0; i < mixingCamera.ChildCameras.Count; i++)
            {
                float weight = (i == currentCameraIndex) ? 1f : 0f;
                mixingCamera.SetWeight(i, weight);
            }
            
            if (debugMode)
                Debug.Log($"Camera initialized. Starting with camera {currentCameraIndex}");
        }
        else
        {
            Debug.LogWarning("MixingCamera reference not set in MainGame!");
        }
    }

    void InitializeGame()
    {
        gameStartTime = Time.time; // Record game start time for master timing synchronization
        
        if (debugMode)
            Debug.Log($"Initializing game with {gameSegments.Count} segments. Game start time: {gameStartTime}");
            
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
            segment.isLooping = false;
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
            
            // Switch to next camera when all segments are complete
            SwitchToNextCamera();
            
            OnAllSegmentsCompleted?.Invoke();
            return;
        }

        // Don't stop existing loops - let completed segments continue playing in background

        currentSegmentIndex = segmentIndex;
        currentObjectIndex = 0;
        currentRevealedLength = 1; // Simon Says: start with revealing only the first element
        isPlayingSegment = true;
        isWaitingForPlayerInput = false;
        
        var segment = gameSegments[segmentIndex];
        
        if (debugMode)
            Debug.Log($"Starting segment {segmentIndex}: {segment.segmentName} - Revealing first {currentRevealedLength} elements");
            
        OnSegmentStarted?.Invoke(segmentIndex);
        
        // Clamp segment start delay to prevent extremely long waits that could break the flow
        float clampedStartDelay = Mathf.Clamp(segment.segmentStartDelay, 0f, 10f);
        
        if (debugMode && segment.segmentStartDelay != clampedStartDelay)
            Debug.LogWarning($"Segment {segmentIndex} start delay clamped from {segment.segmentStartDelay}s to {clampedStartDelay}s to prevent timing issues");
        
        // Start playing the segment sequence after the segment start delay
        DOVirtual.DelayedCall(clampedStartDelay, () => PlaySegmentSequence(segmentIndex));
    }
    
    void StartSegmentLoop(int segmentIndex)
    {
        if (segmentIndex >= gameSegments.Count) return;
        
        var segment = gameSegments[segmentIndex];
        
        // Stop any existing loop for this specific segment
        if (segmentLoops.ContainsKey(segmentIndex))
        {
            segmentLoops[segmentIndex].Kill();
        }
        
        // Clamp delay values to prevent timing issues
        float clampedStartDelay = Mathf.Clamp(segment.segmentStartDelay, 0f, 10f);
        float clampedEndDelay = Mathf.Clamp(segment.segmentEndDelay, 0f, 10f);
        
        // Calculate the actual cycle time including all delays
        float actualCycleTime = clampedStartDelay + 
                               (segment.interactiveObjects.Count * segment.delayBetweenObjects) + 
                               clampedEndDelay;
        
        // For segments with high delays, use a simplified approach to avoid timing conflicts
        float startOffset = 0f;
        
        // Only use complex synchronization for segments with reasonable delays (<=2 seconds total additional delay)
        if (clampedStartDelay + clampedEndDelay <= 2f)
        {
            // Original synchronization logic for normal delay values
            float timeSinceGameStart = Time.time - gameStartTime;
            float baseCycleTime = segment.interactiveObjects.Count * segment.delayBetweenObjects;
            float cyclePosition = timeSinceGameStart % baseCycleTime;
            startOffset = baseCycleTime - cyclePosition;
        }
        
        if (debugMode)
        {
            if (segment.segmentStartDelay != clampedStartDelay || segment.segmentEndDelay != clampedEndDelay)
                Debug.LogWarning($"Segment {segmentIndex} delays clamped in loop - Start: {segment.segmentStartDelay}s->{clampedStartDelay}s, End: {segment.segmentEndDelay}s->{clampedEndDelay}s");
            
            Debug.Log($"Starting loop for segment {segmentIndex} with cycle time: {actualCycleTime}s, start offset: {startOffset}s");
        }
        
        // Create new loop sequence
        Sequence loopSequence = DOTween.Sequence();
        
        // Add initial offset for synchronization (only if reasonable)
        if (startOffset > 0f && startOffset < actualCycleTime)
        {
            loopSequence.AppendInterval(startOffset);
        }
        
        // Add segment start delay
        if (clampedStartDelay > 0f)
        {
            loopSequence.AppendInterval(clampedStartDelay);
        }
        
        // Add the segment playback loop
        for (int i = 0; i < segment.interactiveObjects.Count; i++)
        {
            int index = i;
            loopSequence.AppendCallback(() => {
                if (segment.interactiveObjects[index] != null)
                {
                    segment.interactiveObjects[index].Play(true);
                }
            });
            loopSequence.AppendInterval(segment.delayBetweenObjects);
        }
        
        // Add segment end delay
        if (clampedEndDelay > 0f)
        {
            loopSequence.AppendInterval(clampedEndDelay);
        }
        
        // Set to loop infinitely and start
        loopSequence.SetLoops(-1).Play();
        
        // Store the sequence for later management
        segmentLoops[segmentIndex] = loopSequence;
        segment.isLooping = true;
    }
    
    void StopSegmentLoop(int segmentIndex)
    {
        if (segmentLoops.ContainsKey(segmentIndex))
        {
            segmentLoops[segmentIndex].Kill();
            segmentLoops.Remove(segmentIndex);
        }
        
        if (segmentIndex < gameSegments.Count)
        {
            gameSegments[segmentIndex].isLooping = false;
        }
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
        
        // Check for invisible musical objects in the revealed part and make them visible
        for (int i = 0; i < elementsToPlay; i++)
        {
            var musicalObject = segment.interactiveObjects[i];
            if (musicalObject != null && !musicalObject.gameObject.activeInHierarchy)
            {
                musicalObject.gameObject.SetActive(true);
                if (debugMode)
                    Debug.Log($"Setting visibility to true for musical object {i} in segment {segmentIndex} (part of revealed sequence)");
            }
        }
        
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
            
            // Check if player has completed the current revealed sequence
            if (currentObjectIndex >= currentRevealedLength)
            {
                // Player completed current revealed sequence successfully!
                if (currentRevealedLength >= segment.interactiveObjects.Count)
                {
                    // Entire segment sequence completed!
                    // First play the final click feedback
                    clickedMusicalObject.Play(true);
                    
                    // Then after the active animation completes, show success
                    DOVirtual.DelayedCall(1f, () => // Wait for Play() animation to complete (1s) + small buffer
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
                    // Not the final sequence, give individual feedback
                    clickedMusicalObject.Play(true); // Give feedback
                    
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
            else
            {
                // Not completed current sequence yet, just give feedback and wait for next click
                clickedMusicalObject.Play(true);
            }
            
            if (debugMode)
                Debug.Log($"Correct! Clicked object at position {currentObjectIndex-1}. Progress: {currentObjectIndex}/{currentRevealedLength}");
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
        
        // Start the loop for this completed segment in the background
        DOVirtual.DelayedCall(1f, () =>
        {
            StartSegmentLoop(segmentIndex);
            
            // Move camera to the next segment (if there are more segments)
            if (segmentIndex + 1 < gameSegments.Count)
            {
                SwitchToNextCamera();
            }
        });
        
        // Move to next segment after delay
        if (autoProgressSegments)
        {
            // Clamp segment end delay to prevent extremely long waits that could break the flow
            float clampedEndDelay = Mathf.Clamp(segment.segmentEndDelay, 0f, 10f);
            float totalDelay = clampedEndDelay + delayBetweenSegments;
            
            if (debugMode && segment.segmentEndDelay != clampedEndDelay)
                Debug.LogWarning($"Segment {segmentIndex} end delay clamped from {segment.segmentEndDelay}s to {clampedEndDelay}s to prevent timing issues");
            
            DOVirtual.DelayedCall(totalDelay, () =>
            {
                StartSegment(segmentIndex + 1);
            });
        }
    }
    
    void SwitchToNextCamera()
    {
        if (mixingCamera == null)
        {
            Debug.LogWarning("MixingCamera reference not set!");
            return;
        }
        
        int nextCameraIndex = currentCameraIndex + 1;
        
        // Make sure we don't exceed available cameras
        if (nextCameraIndex >= mixingCamera.ChildCameras.Count)
        {
            if (debugMode)
                Debug.LogWarning($"Cannot switch to camera {nextCameraIndex} - only {mixingCamera.ChildCameras.Count} cameras available");
            return;
        }
        
        SwitchToCamera(nextCameraIndex);
    }
    
    void SwitchToCamera(int cameraIndex)
    {
        if (mixingCamera == null || cameraIndex < 0 || cameraIndex >= mixingCamera.ChildCameras.Count)
        {
            Debug.LogWarning($"Cannot switch to camera {cameraIndex} - invalid index or missing camera reference");
            return;
        }
        
        if (cameraIndex == currentCameraIndex)
        {
            return; // Already on this camera
        }
        
        int previousCameraIndex = currentCameraIndex;
        currentCameraIndex = cameraIndex;
        
        // Smooth transition between cameras
        DOVirtual.Float(0, 1, cameraTransitionDuration, (weight) =>
        {
            mixingCamera.SetWeight(cameraIndex, weight);
            mixingCamera.SetWeight(previousCameraIndex, 1 - weight);
        }).SetEase(Ease.Linear);
        
        if (debugMode)
            Debug.Log($"Camera switched from {previousCameraIndex} to {cameraIndex}");
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
        // Stop all existing segment loops
        foreach (var kvp in segmentLoops)
        {
            kvp.Value.Kill();
        }
        segmentLoops.Clear();
        
        // Reset game start time for new master timing
        gameStartTime = Time.time;
        
        // Reset all segments
        foreach (var segment in gameSegments)
        {
            segment.isCompleted = false;
            segment.isLooping = false;
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
        
        // Reset camera to first position
        currentCameraIndex = 0;
        if (mixingCamera != null)
        {
            for (int i = 0; i < mixingCamera.ChildCameras.Count; i++)
            {
                float weight = (i == currentCameraIndex) ? 1f : 0f;
                mixingCamera.SetWeight(i, weight);
            }
        }
        
        DOVirtual.DelayedCall(0.5f, () => StartSegment(0));
    }

    // Getters for current state
    public int CurrentSegmentIndex => currentSegmentIndex;
    public bool IsPlayingSegment => isPlayingSegment;
    public GameSegment CurrentSegment => currentSegmentIndex < gameSegments.Count ? gameSegments[currentSegmentIndex] : null;
    
    // Background loop management
    public bool IsSegmentLooping(int segmentIndex) => segmentIndex < gameSegments.Count && gameSegments[segmentIndex].isLooping;
    public int GetActiveLoopCount() => segmentLoops.Count;
    public List<int> GetLoopingSegmentIndices() => new List<int>(segmentLoops.Keys);
    
    // Manual loop control methods
    public void StartSegmentLoopManual(int segmentIndex)
    {
        if (segmentIndex < gameSegments.Count && gameSegments[segmentIndex].isCompleted)
        {
            StartSegmentLoop(segmentIndex);
        }
    }
    
    public void StopSegmentLoopManual(int segmentIndex)
    {
        StopSegmentLoop(segmentIndex);
    }
    
    public void StopAllLoops()
    {
        foreach (var kvp in segmentLoops)
        {
            kvp.Value.Kill();
        }
        segmentLoops.Clear();
        
        foreach (var segment in gameSegments)
        {
            segment.isLooping = false;
        }
    }
    
    // Camera control methods
    public int CurrentCameraIndex => currentCameraIndex;
    public void SwitchToCameraManual(int cameraIndex)
    {
        SwitchToCamera(cameraIndex);
    }
}
