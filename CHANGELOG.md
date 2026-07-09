# Changelog

## 0.3.4

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
