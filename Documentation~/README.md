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
5. Click **Load Manifest** to fetch the current project audio list.
6. Use **Preview**, **Import / Update**, **Create Emitter**, or drag `⇱` into the Scene view.
7. Use **Sync Changed** to update imported clips whose GameSound version changed.
8. Use **Fetch Commands** only when the GameSound web workspace explicitly queued Unity commands.

## Imported assets

Audio and metadata are stored below `Assets/GameSound` by default. Updating an existing imported sound reuses the previous `AudioClip` path so scene references stay stable.

## Components

- `GameSoundEventEmitter`: main FMOD-style emitter component backed by Unity `AudioSource`.
- `GameSoundAudioSource`: backward-compatible base component name.
- `GameSoundAsset`: imported audio metadata + `AudioClip` reference.
- `GameSoundSoundReference`: serialized project/item/sound/version reference copied onto emitters.

## Troubleshooting

- If the package does not update, remove the old entry from `Packages/packages-lock.json` or use Package Manager > Update.
- If login expires, click **Login in Browser** again.
- If Unity reports a truncated MP3, the HTTP download was checked by the importer; re-export/re-upload the original MP3 if the warning persists.
