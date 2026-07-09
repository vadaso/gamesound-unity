# GameSound for Unity

Unity Editor package for syncing GameSound project audio into Unity and creating emitter-ready `AudioSource` components.

## Install

Open **Window > Package Manager**, click **+**, choose **Install package from git URL**, and enter:

```txt
https://github.com/vadaso/gamesound-unity.git#v0.3.3
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.gamesound.unity": "https://github.com/vadaso/gamesound-unity.git#v0.3.3"
  }
}
```

## Usage

1. Open **Window > GameSound > Setup Wizard**.
2. Open **Window > GameSound**.
3. Click **Login in Browser** and approve the GameSound Unity connection.
4. Load a GameSound project, import sounds, or create GameSound emitters.

The package connects to `https://gamesound.ai`.
