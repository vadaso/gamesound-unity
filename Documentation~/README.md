# GameSound for Unity

Unity Editor package for syncing GameSound project sounds into a Unity project and placing them as GameSound Event Emitters backed by Unity `AudioSource`.

## Git install

Add the package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamesound.unity": "https://github.com/vadaso/gamesound-unity.git#v0.3.3"
  }
}
```

## Workflow

1. Open **Window > GameSound > Setup Wizard** once to validate the fixed GameSound host, import folder, and scene AudioListener.
2. Open **Window > GameSound**.
3. Click **Login in Browser** and approve the Unity device on the GameSound site. The package always uses `https://gamesound.ai`.
4. Load projects and select a project.
5. Click **Load Manifest** to fetch the current project sounds.
6. Use the **Sound Browser** to search/fold by folder, preview, import, or create a `GameSoundEventEmitter`.
7. Drag the `⇱` handle from a sound row into the Scene view to create an emitter at the drop point.
8. If the GameSound web workspace queued a Unity sync request, click **Fetch Web Commands** in Unity to run and acknowledge it.

Imported files are stored under `Assets/GameSound` by default. Existing imported clips are overwritten in-place on re-sync so Unity `.meta` GUID references remain stable. **Sync Changed** skips sounds whose imported `GameSoundAsset.VersionHash` already matches the current manifest.

## Runtime components

- `GameSoundEventEmitter` — FMOD-style placement component that wraps Unity `AudioSource`, supports play/stop triggers, stop mode, distance, volume, loop, and pitch variation.
- `GameSoundAudioSource` — backwards-compatible base component name. Prefer `GameSoundEventEmitter` for new scene objects.
- `GameSoundSoundReference` — stable server reference data copied from the imported `GameSoundAsset` so the component can display project/item/sound/version metadata.

Random pitch values are clamped to positive values. Unity only supports negative `AudioSource.pitch` for uncompressed/decompressed clips, so GameSound treats pitch variation as a positive multiplier such as `0.95..1.05`.

## Manual sync model

v0.3 intentionally does not keep a persistent polling/WebSocket connection. The web app creates commands such as `sync_project`, `sync_item`, `create_audio_source`, or `refresh_manifest`, and the Unity user pulls them manually with **Fetch Web Commands**. This avoids background editor network work and keeps the first version easy to debug.

## Authentication safety

Unity stores only the short-lived access token needed by the editor package. Refresh tokens are not persisted by the package and the server requires users to login again after token expiry.
