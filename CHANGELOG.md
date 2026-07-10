# Changelog

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
