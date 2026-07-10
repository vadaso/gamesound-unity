# GameSound for Unity

GameSound for Unity is a Unity Editor package that imports audio from GameSound projects and turns it into Unity-native `AudioClip`, `GameSoundAsset`, and `AudioSource` emitter workflows.

## Features

- Browser login against `https://gamesound.ai` with a short-lived Unity editor access token.
- Project picker and manifest loader for GameSound project audio.
- Import/update sounds into `Assets/GameSound` while preserving existing Unity `.meta` references on re-sync.
- Search, source filter, folder grouping, browser preview, single import, and changed-only updates.
- Drag a sound from the GameSound window into the Scene view to create a `GameSoundEventEmitter`.
- `GameSoundEventEmitter` / `GameSoundAudioSource` components backed by Unity `AudioSource`.
- Play/stop triggers for start, enable, disable, destroy, trigger, collision, mouse, and manual use.
- Loop, volume, 2D/3D spatial blend, distance, fade-out stop mode, and positive pitch variation.
- **Refresh from GameSound** and **Update Imported** are the primary manual sync controls. Optional **Auto Refresh (30m)** is off by default and checks already-imported sounds only every 30 minutes while the GameSound window is open.

## Install

Open **Window > Package Manager**, click **+**, choose **Install package from git URL**, and enter:

```txt
https://github.com/vadaso/gamesound-unity.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamesound.unity": "https://github.com/vadaso/gamesound-unity.git"
  }
}
```

For a locked production project, pin a tag or commit hash after testing the current `main` version.

## Quick start

1. Open **Window > GameSound > Setup Wizard** and check the import folder / scene audio basics.
2. Open **Window > GameSound**.
3. Click **Login in Browser** and approve the Unity connection on GameSound.
4. Click **Load Projects**, select a project, then click **Refresh from GameSound**.
5. Use **Import / Update** for one sound, or **Update Imported** to manually update sounds you already imported. Leave **Auto Refresh (30m)** off unless you want Unity to check for updates every 30 minutes while this window is open.
6. Click **Create Emitter** or drag the `⇱` handle into the Scene view to place an emitter.

Imported audio defaults to:

```txt
Assets/GameSound/<Project Name>/<GameSound Folder>/<Sound Title>.<format>
Assets/GameSound/<Project Name>/<GameSound Folder>/<Sound Title>.gamesound.asset
```

## Runtime components

### GameSoundEventEmitter

Recommended component for scene placement. It requires Unity `AudioSource` and exposes GameSound metadata plus playback controls.

Supported triggers:

- `ObjectStart`, `ObjectEnable`, `ObjectDisable`, `ObjectDestroy`
- `TriggerEnter`, `TriggerExit`, `TriggerEnter2D`, `TriggerExit2D`
- `CollisionEnter`, `CollisionExit`, `CollisionEnter2D`, `CollisionExit2D`
- `MouseEnter`, `MouseExit`, `MouseDown`, `MouseUp`
- `Manual`

### GameSoundAudioSource

Backward-compatible base component. Use `GameSoundEventEmitter` for new objects unless you specifically need the old component name.

### GameSoundAsset / GameSoundSoundReference

`GameSoundAsset` is a ScriptableObject created during import. It stores the imported clip plus project/item/sound/version metadata. `GameSoundSoundReference` copies stable reference data onto scene components so inspectors can show where a sound came from.

## Notes

- The package intentionally uses one fixed production API host: `https://gamesound.ai`.
- Refresh tokens are not stored by the package. If the editor token expires, log in again.
- Auto Refresh is disabled by default because Unity audio imports can briefly block the editor and show progress bars. If enabled, it checks imported sounds every 30 minutes while the GameSound window is open.
- Unity may warn that a source MP3 is truncated if the original uploaded MP3 has inconsistent frame length metadata. The importer validates incomplete HTTP downloads, but malformed source audio should be re-exported/re-uploaded in GameSound for a clean Unity import.
- `Documentation~` is kept without Unity `.meta` files because Unity treats `~` package folders as hidden package documentation.

## Requirements

- Unity 2022.3 or newer.
- A GameSound account with access to at least one project.
