# GameSound for Unity Manual

## Install

Use Unity Package Manager > **Install package from git URL**:

```txt
https://github.com/vadaso/gamesound-unity.git
```

Or add this dependency to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamesound.unity": "https://github.com/vadaso/gamesound-unity.git"
  }
}
```

## Editor workflow

1. Open **Window > GameSound > Setup Wizard** once per Unity project.
2. Open **Window > GameSound**.
3. Click **Login in Browser** and approve the GameSound connection.
4. Click **Load Projects** and choose a project.
5. Click **Refresh from GameSound** to fetch the current project audio list.
6. Use **Preview**, **Import / Update**, **Create Emitter**, or drag `⇱` into the Scene view.
7. Keep **Auto Refresh (30m)** off for normal use. If you enable it, Unity checks already-imported clips and metadata every 30 minutes while the GameSound window is open.
8. Use **Update Imported** for an immediate manual update. It fetches the latest project manifest first, so it never relies on the list cached in the open window.

## Imported assets

Audio and metadata are stored below `Assets/GameSound` by default. Updating an existing imported sound reuses the previous `AudioClip` path so scene references stay stable. Metadata-only changes such as title, folder, source, type, and duration are refreshed without downloading the audio again.

The import root must remain below the Unity project's `Assets/` folder. Audio updates are downloaded to a temporary file and validated before the previous clip is replaced. Supported direct import formats are MP3, WAV, OGG, and AIF.

## Components

- `GameSoundEventEmitter`: main FMOD-style emitter component backed by Unity `AudioSource`.
- `GameSoundAudioSource`: backward-compatible base component name.
- `GameSoundAsset`: imported audio metadata + `AudioClip` reference.
- `GameSoundSoundReference`: internal serialized project/item/sound/version reference copied onto emitters. Internal IDs and hashes are hidden from normal inspectors.

## Troubleshooting

- If the package does not update, remove the old entry from `Packages/packages-lock.json` or use Package Manager > Update.
- If login expires, click **Login in Browser** again.
- Login credentials are kept only for the current Unity editor session. The plugin always connects to `https://gamesound.ai`; there is no API host setting.
- Auto Refresh is off by default. Enable it only if a 30-minute background check is acceptable for your project; Unity may briefly show import/progress UI when changed audio is updated.
- The emitter inspector Play button previews clips in Edit Mode using Unity Editor audio preview. In Play Mode it calls the runtime emitter Play method.
- Disable/Destroy play triggers use a detached one-shot and do not loop. Fade Out cannot continue after the owning GameObject is disabled or destroyed, so those stop triggers are immediate.
- Newly created emitters play on `ObjectStart` by default. Set **Play Trigger** to `None` or `Manual` for code-only playback.
- If Unity reports a truncated MP3, the HTTP download was checked by the importer; re-export/re-upload the original MP3 if the warning persists.
