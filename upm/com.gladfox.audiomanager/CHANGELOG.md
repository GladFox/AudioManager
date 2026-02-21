# Changelog

All notable changes to this package will be documented in this file.

## [0.1.2] - Unreleased
### Planned
- Dynamic `SoundEvent` discovery registry for runtime-created/loaded events.
- New preload APIs for discovered events (full and marker-based).
- Dialog-friendly preload flow without manual `List<SoundEvent>`/`List<string>` assembly.
- Scope-aware lifecycle for discovered preload/unload.

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
