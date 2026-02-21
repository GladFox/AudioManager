# Changelog

All notable changes to this package will be documented in this file.

## [0.1.2] - 2026-02-22
### Added
- Runtime `SoundEventDiscoveryRegistry` with auto-register on `SoundEvent.OnEnable/OnDisable`.
- New discovery preload APIs in `AudioManager`:
  - `CaptureDiscoveryMarker()`
  - `PreloadDiscovered(...)`
  - `PreloadDiscoveredSince(marker, ...)`
- Discovery diagnostics in debugger (`DiscoveredEventCount`, `DiscoveryRevision`, `LastDiscoveredPreloadCount`).
- Editor playmode hook to reset discovery registry before entering Play Mode.

### Changed
- Demo bootstrap now uses discovery preload with scope (`demo.dialogue`) instead of manual preload id list.
- Documentation and release artifacts aligned for `0.1.2`.

## [0.1.1] - 2026-02-21
### Changed
- Unified demo distribution: full example content is provided as package sample `Samples~/AudioManager`.
- Package sample metadata updated to `Audio Manager Example` with complete content folder.
- Documentation updated for sample import flow and UPM git installation URL.

## [0.1.0] - 2026-02-20
### Added
- Initial UPM packaging for AudioManager runtime/editor stack.
- Centralized playback API and pooling infrastructure.
- Addressables dynamic loading (`AudioContentService`, `AudioLoadHandle`, `AudioBank`).
- Editor tooling (`AudioProductionSetup`, `AudioValidator`, `AudioDebuggerWindow`).
- Package sample with full demo content folder (`Samples~/AudioManager`).
