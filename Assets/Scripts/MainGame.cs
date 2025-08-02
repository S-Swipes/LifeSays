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
    public float totalDuration = 5f;
    public float startDelay = 0f;
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
    
    [Header("VFX System")]
    public VFXController vfxController;
    
    [Header("Timing Precision Settings")]
    [Tooltip("Time window in seconds for Perfect timing")]
    public float perfectTimingWindow = 0.15f;
    [Tooltip("Time window in seconds for Good timing")]
    public float goodTimingWindow = 0.3f;
    [Tooltip("Time window in seconds for OK timing")]
    public float okTimingWindow = 0.5f;
    
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
    
    // Timing precision tracking
    private float sequencePlayStartTime; // When the current sequence started playing
    private List<float> expectedClickTimes = new List<float>(); // When each object should be clicked
    
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
        
        OnSegmentStarted?.Invoke(segmentIndex);
        
        // PlaySegmentSequence now handles its own synchronization with master timing
        // No need for additional delays here
        if (debugMode)
            Debug.Log($"Starting segment {segmentIndex}: {segment.segmentName} - sequence will sync to master timing");
            
        PlaySegmentSequence(segmentIndex);
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
        float clampedStartDelay = Mathf.Clamp(segment.startDelay, 0f, 10f);
        float objectsPlayTime = segment.interactiveObjects.Count * segment.delayBetweenObjects;
        float clampedEndDelay = Mathf.Clamp(segment.totalDuration - clampedStartDelay - objectsPlayTime, 0f, 10f);
        
        // Use the total duration as the actual cycle time
        float actualCycleTime = segment.totalDuration;
        
        // For segments with high delays, use a simplified approach to avoid timing conflicts
        float startOffset = 0f;
        
        // Simple global master beat synchronization
        // All segments sync to the same master timeline starting from game start
        float timeSinceGameStart = Time.time - gameStartTime;
        float nextGlobalBeat = Mathf.Ceil(timeSinceGameStart / masterBeatInterval) * masterBeatInterval;
        float targetStartTime = nextGlobalBeat + clampedStartDelay;
        startOffset = targetStartTime - timeSinceGameStart;
        
        // Ensure we have a minimum delay to avoid immediate starts
        if (startOffset < 0.1f)
        {
            startOffset += masterBeatInterval;
        }
        
        if (debugMode)
        {
            float originalEndDelay = segment.totalDuration - segment.startDelay - objectsPlayTime;
            if (segment.startDelay != clampedStartDelay || originalEndDelay != clampedEndDelay)
                Debug.LogWarning($"Segment {segmentIndex} delays clamped in loop - Start: {segment.startDelay}s->{clampedStartDelay}s, End: {originalEndDelay}s->{clampedEndDelay}s");
            
            // Check if totalDuration is aligned with masterBeatInterval for better sync
            float beatAlignment = segment.totalDuration % masterBeatInterval;
            if (beatAlignment > 0.01f)
                Debug.LogWarning($"Segment {segmentIndex} totalDuration ({segment.totalDuration}s) is not aligned with masterBeatInterval ({masterBeatInterval}s). Consider using multiples of masterBeatInterval for better sync.");
            
            Debug.Log($"Segment {segmentIndex}: timeSinceStart={timeSinceGameStart:F2}s, nextBeat={nextGlobalBeat:F2}s, targetStart={targetStartTime:F2}s, startOffset={startOffset:F2}s");
        }
        
        // Create new loop sequence
        Sequence loopSequence = DOTween.Sequence();
        
        // Add initial sync offset (only for the first iteration)
        if (startOffset > 0f)
        {
            loopSequence.AppendInterval(startOffset);
        }
        
        // Create the repeating segment loop with exact totalDuration
        Sequence segmentLoop = DOTween.Sequence();
        
        // Add segment start delay
        if (clampedStartDelay > 0f)
        {
            segmentLoop.AppendInterval(clampedStartDelay);
        }
        
        // Add the segment playback
        for (int i = 0; i < segment.interactiveObjects.Count; i++)
        {
            int index = i;
            segmentLoop.AppendCallback(() => {
                if (segment.interactiveObjects[index] != null)
                {
                    segment.interactiveObjects[index].Play(true);
                }
            });
            segmentLoop.AppendInterval(segment.delayBetweenObjects);
        }
        
        // Add segment end delay
        if (clampedEndDelay > 0f)
        {
            segmentLoop.AppendInterval(clampedEndDelay);
        }
        
        // Set the segment loop to repeat infinitely
        segmentLoop.SetLoops(-1);
        
        // Add the repeating segment loop to the main sequence
        loopSequence.Append(segmentLoop);
        
        // Start the sequence
        loopSequence.Play();
        
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
        
        // Synchronize with the segment's actual totalDuration cycle timing
        float clampedStartDelay = Mathf.Clamp(segment.startDelay, 0f, 10f);
        
        // Calculate where we are in the segment's cycle and when the next object play phase starts
        float timeSinceGameStart = Time.time - gameStartTime;
        
        // Find the next time the segment cycle will start playing objects
        // Objects start playing after startDelay within each totalDuration cycle
        float cycleLength = segment.totalDuration;
        float timeSinceLastCycleStart = timeSinceGameStart % cycleLength;
        float timeUntilNextCycleStart = cycleLength - timeSinceLastCycleStart;
        
        // Calculate when objects would start playing in the next cycle
        float timeUntilObjectsStart;
        if (timeSinceLastCycleStart < clampedStartDelay)
        {
            // We're still in the start delay of the current cycle
            timeUntilObjectsStart = clampedStartDelay - timeSinceLastCycleStart;
        }
        else
        {
            // We're past the start delay, wait for the next cycle
            timeUntilObjectsStart = timeUntilNextCycleStart + clampedStartDelay;
        }
        
        // Ensure we have a minimum delay to avoid immediate starts
        if (timeUntilObjectsStart < 0.1f)
        {
            timeUntilObjectsStart += cycleLength;
        }
        
        if (debugMode)
        {
            Debug.Log($"Reveal sequence sync to segment cycle: timeSinceStart={timeSinceGameStart:F2}s, cycleLength={cycleLength:F2}s, " +
                     $"timeSinceLastCycleStart={timeSinceLastCycleStart:F2}s, timeUntilObjectsStart={timeUntilObjectsStart:F2}s");
        }
        
        // Calculate expected click times for timing precision (accounting for synchronization)
        expectedClickTimes.Clear();
        float totalSequenceTime = (elementsToPlay - 1) * segment.delayBetweenObjects + 1f; // +1 for last object play time
        float actualSequenceStartTime = Time.time + timeUntilObjectsStart; // When the sequence will actually start
        sequencePlayStartTime = actualSequenceStartTime + totalSequenceTime; // Player input starts after sequence finishes
        
        for (int i = 0; i < elementsToPlay; i++)
        {
            // Expected click time is when the sequence finishes + delay for this object
            float expectedClickTime = sequencePlayStartTime + (i * segment.delayBetweenObjects);
            expectedClickTimes.Add(expectedClickTime);
        }
        
        if (debugMode)
            Debug.Log($"Expected click times calculated. Sequence will start at: {actualSequenceStartTime:F2}s, accepting input at: {sequencePlayStartTime:F2}s");
        
        // Add the synchronization delay before starting the sequence
        DOVirtual.DelayedCall(timeUntilObjectsStart, () =>
        {
            if (debugMode)
                Debug.Log($"Starting segment-cycle-synchronized reveal sequence for segment {segmentIndex}");
            
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
                            Debug.Log($"Playing object {objectIndex} in segment {segmentIndex} at segment-cycle-synchronized time");
                            
                        segment.interactiveObjects[objectIndex].Play();
                    }
                });
            }
            
            // After the sequence finishes playing, wait for player input
            DOVirtual.DelayedCall(totalSequenceTime, () =>
            {
                isWaitingForPlayerInput = true;
                if (debugMode)
                    Debug.Log($"Segment-cycle-synchronized sequence finished playing. Waiting for player to repeat {elementsToPlay} elements.");
            });
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
            // Correct click! Now evaluate timing precision
            currentObjectIndex++;
            
            // Calculate timing accuracy
            string timingFeedback = EvaluateTimingAccuracy(currentObjectIndex - 1);
            
            // Check if player has completed the current revealed sequence
            if (currentObjectIndex >= currentRevealedLength)
            {
                // Player completed current revealed sequence successfully!
                if (currentRevealedLength >= segment.interactiveObjects.Count)
                {
                    // Entire segment sequence completed!
                    // First play the final click feedback
                    clickedMusicalObject.Play(true);
                    
                    // Show timing-based VFX feedback
                    ShowTimingVFX(timingFeedback, clickedMusicalObject.transform.position);
                    
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
                    
                    // Show timing-based VFX feedback
                    ShowTimingVFX(timingFeedback, clickedMusicalObject.transform.position);
                    
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
                    
                    // PlaySegmentSequence will sync to the next appropriate master beat timing
                    PlaySegmentSequence(segmentIndex);
                }
            }
            else
            {
                // Not completed current sequence yet, just give feedback and wait for next click
                clickedMusicalObject.Play(true);
                
                // Show timing-based VFX feedback
                ShowTimingVFX(timingFeedback, clickedMusicalObject.transform.position);
            }
            
            if (debugMode)
                Debug.Log($"Correct! Clicked object at position {currentObjectIndex-1}. Progress: {currentObjectIndex}/{currentRevealedLength}. Timing: {timingFeedback}");
        }
        else
        {
            // Wrong click! Reset current attempt and replay current sequence
            clickedMusicalObject.PlayWrongSelected();
            
            // Show wrong VFX feedback
            if (vfxController != null)
            {
                vfxController.ShowWrongFeedback(clickedMusicalObject.transform.position);
            }
            
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
            
            // PlaySegmentSequence will sync to the next appropriate master beat timing
            PlaySegmentSequence(segmentIndex);
        }
    }

    string EvaluateTimingAccuracy(int clickedObjectIndex)
    {
        if (clickedObjectIndex < 0 || clickedObjectIndex >= expectedClickTimes.Count)
        {
            return "Unknown";
        }
        
        float currentTime = Time.time;
        float expectedTime = expectedClickTimes[clickedObjectIndex];
        float timingDifference = Mathf.Abs(currentTime - expectedTime);
        
        if (debugMode)
            Debug.Log($"Timing evaluation - Current: {currentTime:F3}, Expected: {expectedTime:F3}, Difference: {timingDifference:F3}");
        
        if (timingDifference <= perfectTimingWindow)
        {
            return "Perfect";
        }
        else if (timingDifference <= goodTimingWindow)
        {
            return "Good";
        }
        else if (timingDifference <= okTimingWindow)
        {
            return "OK";
        }
        else
        {
            return "Late";
        }
    }
    
    void ShowTimingVFX(string timingFeedback, Vector3 position)
    {
        if (vfxController == null)
        {
            if (debugMode)
                Debug.LogWarning("VFXController not assigned! Cannot show timing feedback.");
            return;
        }
        
        switch (timingFeedback)
        {
            case "Perfect":
                vfxController.ShowPerfectFeedback(position);
                break;
            case "Good":
                vfxController.ShowGoodFeedback(position);
                break;
            case "OK":
                vfxController.ShowOkFeedback(position);
                break;
            default:
                // For "Late" or other cases, don't show positive feedback
                if (debugMode)
                    Debug.Log($"No VFX shown for timing feedback: {timingFeedback}");
                break;
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
            // Calculate segment end delay based on total duration
            float objectsPlayTime = segment.interactiveObjects.Count * segment.delayBetweenObjects;
            float clampedEndDelay = Mathf.Clamp(segment.totalDuration - segment.startDelay - objectsPlayTime, 0f, 10f);
            float totalDelay = clampedEndDelay + delayBetweenSegments;
            
            float originalEndDelay = segment.totalDuration - segment.startDelay - objectsPlayTime;
            if (debugMode && originalEndDelay != clampedEndDelay)
                Debug.LogWarning($"Segment {segmentIndex} end delay clamped from {originalEndDelay}s to {clampedEndDelay}s to prevent timing issues");
            
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
