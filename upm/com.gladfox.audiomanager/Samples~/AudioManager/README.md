# Audio Manager Example

This sample contains the full `Assets/AudioManager` demo folder:
- `Data/` (`AudioMain.mixer`)
- `Demo/` (`AudioDemoScene.unity`, bootstrap script, dialogue prefab component, demo clips, demo sound events)
- `Resources/Audio/` (`AudioConfig.asset`, `DemoDialoguePrefab.prefab`)

## How To Run
1. Import the sample from Package Manager.
2. Open `Demo/AudioDemoScene.unity`.
3. Enter Play Mode and verify demo controls in the scene UI.
4. Press `8` (or click `Open/Close Dialogue`) to test full lifecycle:
   - dynamic prefab load from `Resources/Audio/DemoDialoguePrefab`
   - `PreloadDiscoveredSince(..., acquireScope: true, scopeId: "demo.dialogue")`
   - `ReleaseScope("demo.dialogue")` and delayed unload after close

## Notes
- The sample expects package runtime/editor code from `com.gladfox.audiomanager`.
- Addressables settings live in the consumer app project.
- Demo UI is implemented with runtime `uGUI` (no `OnGUI`/IMGUI).
- Bootstrap uses discovery preload for dialog audio without manual id lists.
