# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

"LifeSays" is a Unity 3D rhythm/music game built with Unity 6000.0.54f1. It's a Simon Says-style game where players must interact with musical objects in precise timing sequences. The project uses the Universal Render Pipeline (URP) for 2D rendering and includes the Unity Input System for advanced input handling.

## Unity Project Structure

- **Unity Version**: 6000.0.54f1
- **Render Pipeline**: Universal Render Pipeline (URP) 2D
- **Input System**: Unity's new Input System (not Legacy)
- **Scenes**: 
  - `Assets/Scenes/ParkScene_v01.unity` - Main game scene
  - `Assets/Scenes/LoadingScene.unity` - Loading screen scene
  - `Assets/Scenes/Archive/SampleScene.unity` - Basic template scene (archived)
  - `Assets/Scenes/Archive/dudi.unity` - Additional scene (archived)

## Key Unity Packages

- Universal Render Pipeline (URP) 17.0.4
- Unity Input System 1.14.1
- Cinemachine 3.1.4 - Advanced camera system
- DOTween (Demigiant) - Animation and tweening
- 2D Feature package (Animation, Tilemap, Sprite tools)
- TextMeshPro for UI text
- Unity Recorder 5.1.2 - Video/animation recording
- Unity Test Framework for testing

## Game Architecture

### Core Game Scripts

The game implements a musical Simon Says pattern with the following key components:

- **MainGame.cs** (`Assets/Scripts/MainGame.cs:21`) - Core game controller managing segments, timing, and player input
- **GameSegment** (`Assets/Scripts/MainGame.cs:8`) - Data structure for game segments containing musical objects and timing
- **MusicalObjectControl.cs** - Controls individual interactive musical objects
- **VFXController.cs** - Manages visual and audio feedback system for timing feedback (Perfect/Good/OK/Wrong)
- **CameraControl.cs** (`Assets/Scripts/CameraControl.cs:5`) - Manual camera switching system using keyboard input (A/S/D/E keys)
- **GameSegmentLooper.cs** (`Assets/Scripts/GameSegmentLooper.cs:4`) - Handles segment looping and timing using DOTween
- **LoadingScreenController.cs** - Manages loading screen with progress bar and scene transitions

### Game Mechanics

- **Timing System**: Precision-based timing with Perfect/Good/OK/Miss feedback windows
- **Segment-Based Gameplay**: Game divided into segments with interactive musical objects
- **Camera System**: Cinemachine mixing camera with manual switching
- **Audio-Visual Feedback**: Combined visual effects and audio feedback for timing accuracy (Perfect/Good/OK/Wrong)

### Assets Organization

- **Prefabs/**: Game objects organized by category (BGs, Environments, InteractiveObjects, Segments, VFX)
- **Scripts/**: C# scripts for game logic
- **Art/**: Sprites and animations organized by type
- **Sounds/**: Audio files organized by purpose (InteractiveObjects/, Indicators/ for timing feedback)
- **Settings/**: URP renderer and lighting settings

## Development Commands

Unity projects are built and managed through the Unity Editor, not traditional build systems:

### Building
- Open project in Unity Editor
- File → Build Settings → Build (or Build And Run)
- Platform-specific builds available for multiple targets

### Running/Testing
- Press Play button in Unity Editor to enter Play Mode
- Use Unity Test Runner (Window → General → Test Runner) for unit tests
- Access via Window → Analysis → Console for runtime debugging
- Performance profiling via Window → Analysis → Profiler
- Game flow: LoadingScene.unity → ParkScene_v01.unity (main gameplay)

### Input System Configuration

The project uses a comprehensive input action setup defined in `Assets/InputSystem_Actions.inputactions`:

**Player Actions:**
- Move (WASD/Arrow keys, Gamepad left stick)
- Look (Mouse delta, Gamepad right stick)  
- Attack (Mouse left click, Enter, Gamepad West button)
- Interact (E key, Gamepad North button) - uses Hold interaction
- Jump (Space, Gamepad South button)
- Sprint (Left Shift, Gamepad left stick press)
- Crouch (C key, Gamepad East button)
- Previous/Next (1/2 keys, Gamepad D-pad)

**UI Actions:**
- Standard UI navigation and interaction controls
- Multi-platform support (Keyboard/Mouse, Gamepad, Touch, XR)

## Development Workflow

1. Open project in Unity Editor
2. Modify scenes in `Assets/Scenes/` (primarily ParkScene_v01.unity for gameplay, LoadingScene.unity for loading screen)
3. Edit scripts in `Assets/Scripts/` for game logic changes
4. Configure input actions via the Input Actions window
5. Test in Play Mode within Unity Editor (start from LoadingScene.unity for full experience)
6. Use Unity Recorder for capturing gameplay footage
7. Build via Unity's Build Settings when ready for deployment

## Unity-Specific Considerations

- Version control ignores Unity-generated files (Library, Temp, etc.)
- Asset serialization uses text-based .asset and .unity files
- Meta files (.meta) track Unity's asset import settings
- Package dependencies managed through Unity Package Manager
- DOTween requires proper setup and initialization for animation sequences