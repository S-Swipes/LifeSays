# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D game project called "LifeSays" built with Unity 6000.0.54f1. The project uses the Universal Render Pipeline (URP) for 2D rendering and includes the Unity Input System for advanced input handling.

## Unity Project Structure

- **Unity Version**: 6000.0.54f1
- **Render Pipeline**: Universal Render Pipeline (URP) 2D
- **Input System**: Unity's new Input System (not Legacy)
- **Scene Setup**: Single sample scene at `Assets/Scenes/SampleScene.unity`

## Key Unity Packages

- Universal Render Pipeline (URP) 17.0.4
- Unity Input System 1.14.1
- 2D Feature package (Animation, Tilemap, Sprite tools)
- TextMeshPro for UI text
- Unity Test Framework for testing

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

## Architecture Notes

### Rendering Setup
- Configured for 2D games with URP 2D renderer
- Custom render pipeline assets in `Assets/Settings/`
- Volume profiles for post-processing effects

### No Custom Code Yet
- Project appears to be a fresh Unity template
- No custom C# scripts currently present
- Ready for game development implementation

### Input Architecture
- Uses Unity's new Input System (Action-based)
- Centralized input configuration in `.inputactions` file
- Supports multiple control schemes simultaneously
- Designed for cross-platform input handling

## Development Workflow

1. Open project in Unity Editor
2. Create/modify scenes in `Assets/Scenes/`
3. Add custom scripts to `Assets/Scripts/` (create folder as needed)
4. Configure input actions via the Input Actions window
5. Test in Play Mode within Unity Editor
6. Build via Unity's Build Settings when ready for deployment

## Unity-Specific Considerations

- Version control ignores Unity-generated files (Library, Temp, etc.)
- Asset serialization uses text-based .asset and .unity files
- Meta files (.meta) track Unity's asset import settings
- Package dependencies managed through Unity Package Manager