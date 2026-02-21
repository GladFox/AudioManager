# Audio Manager Example

This sample contains the full `Assets/AudioManager` demo folder:
- `Data/` (`AudioMain.mixer`)
- `Demo/` (`AudioDemoScene.unity`, bootstrap script, demo clips, demo sound events)
- `Resources/Audio/AudioConfig.asset`

## How To Run
1. Import the sample from Package Manager.
2. Open `Demo/AudioDemoScene.unity`.
3. Enter Play Mode and verify demo controls in the scene UI.

## Notes
- The sample expects package runtime/editor code from `com.gladfox.audiomanager`.
- Addressables settings live in the consumer app project.
- Bootstrap uses discovery preload (`PreloadDiscovered`) for dialog audio, without manual id lists.
