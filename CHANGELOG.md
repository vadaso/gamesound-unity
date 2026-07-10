# Changelog

## 0.3.9

- Make **Update Imported** fetch the latest project manifest before checking imported sounds, matching **Refresh from GameSound** instead of reusing a cached list.

## 0.3.8

- Fix the production API origin to `https://gamesound.ai` internally and remove API host rows from Unity setup/connection UI.
- Store the scoped access token only for the current Unity editor session and remove legacy persisted credentials.
- Restrict import roots to project-relative `Assets/` paths and sanitize remote folder/file paths across platforms.
- Allow only Unity-supported audio extensions so remote metadata cannot create arbitrary asset file types.
- Download into a validated temporary file before replacing an existing AudioClip, preserving the previous clip on network or size-validation failure.
- Refuse non-atomic update fallbacks, unsafe legacy clip paths, and Windows-reserved remote file/folder names.
- Scope imported asset lookup by GameSound project so the same sound used in multiple projects cannot overwrite shared metadata.
- Clear stale GameSound reference data and the AudioSource clip when a sound is unassigned.
- Make newly created emitters play on Object Start, remove duplicate `playOnAwake` playback, and make disable/destroy play triggers emit detached one-shots.
- Honor zero volume and zero minimum-distance values from project Unity settings.
- Hide the internal manifest version from the GameSound window.

## 0.3.7

- Hide internal project/item/sound IDs and version hashes from GameSound Unity inspectors.
- Make the inspector Play button use Unity Editor audio preview outside Play Mode, so created emitters can be auditioned reliably.
- Keep runtime Play/Stop behavior unchanged for Play Mode and built games.

## 0.3.6

- Disable Auto Refresh by default so Unity does not periodically block the editor with audio import/progress UI.
- Slow optional Auto Refresh to a 30-minute interval and label it as **Auto Refresh (30m)** in the editor.
- Update README/manual copy to explain that manual refresh is the default workflow and Auto Refresh is opt-in.

## 0.3.5

- Replace the old web-command workflow with one Unity-owned refresh flow: **Refresh from GameSound**, **Auto Refresh**, and **Update Imported**.
- Refresh imported sound metadata from the latest manifest even when the audio file version did not change.
- Remove hidden `Documentation~` / `Samples~` / empty test `.meta` files that caused Unity package-cache warnings.
- Remove unimplemented heartbeat calls so the editor no longer logs repeated 404 warnings.
- Reduce the GameSound window header height and remove the large web-sync-focused green hero copy.
- Validate downloaded file size against `Content-Length` when available.
- Avoid a duplicate audio reimport pass to reduce repeated Unity MP3 import warnings.
- Clean public package README and documentation for GitHub/UPM installation.

## 0.3.3

- Use fixed production GameSound API host.
- Add browser/device login flow with server-side logout revocation.
- Sync GameSound project manifests into Unity.
- Import sounds as stable `GameSoundAsset` references.
- Create `GameSoundEventEmitter` components backed by Unity `AudioSource`.
- Add Unity package `.meta` files for immutable package installs.
