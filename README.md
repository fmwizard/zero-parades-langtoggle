# Zero Parades Language Toggle

A mod that lets you swap *Zero Parades*' text language with one keypress, the way *Disco Elysium* does it.

**Tested on game version**: `1.0.36599.K01`

## Install

1. Download the latest release zip.
2. Extract it into your *Zero Parades* install folder (the one containing `ZeroParades.exe`).
3. Launch the game (for Steam users, launch from your Steam library, not by double-clicking `ZeroParades.exe` in the game folder).

   The first launch takes longer than normal: BepInEx downloads Unity base libraries from the internet and generates IL2CPP wrapper assemblies. A small console window may appear briefly — that's BepInEx, it's expected. Subsequent launches are normal speed.

4. In game, press **`;`** (semicolon) to toggle between languages.

By default, the mod toggles between **English** (`en`) and **Simplified Chinese** (`zh_cn`).

You can change both the hotkey and target languages in:
`BepInEx\config\zp.langtoggle.cfg`

See the [Configuration](#configuration) section below for details.

### If the game won't launch (or exits immediately)

This is almost always antivirus software blocking BepInEx. BepInEx loads mods through a `winhttp.dll` proxy that injects code into the game process, which antivirus heuristics often flag as suspicious.

Fix:

1. Add your *Zero Parades* install folder to your antivirus exclusion list.
   - Windows Defender: *Settings → Privacy & security → Windows Security → Virus & threat protection → Manage settings → Exclusions → Add a folder*.
2. Delete `<game folder>\BepInEx\unity-libs\` if it exists — antivirus may have left it half-extracted.
3. Launch the game again.

If it still fails, check `<game folder>\BepInEx\LogOutput.log` for the actual error and open an issue with the log attached.

## Configuration

Edit `<game folder>\BepInEx\config\zp.langtoggle.cfg`:

```ini
[General]
ToggleKey = Semicolon
LanguageA = en
LanguageB = zh_cn
ShowNotification = true
```

### Languages shipped with the game

| Code | Language |
|------|----------|
| `en` | English (source text) |
| `zh_cn` | Simplified Chinese |
| `de` | German |
| `ru` | Russian |
| `es_mx` | Spanish (Mexico) |

Other ISO codes (`zh_tw`, `en_us`, `fr`, `ja`, `ko`, etc.) are accepted by the config but the game has no translation for them — you will see fallback English or empty strings.

### Hotkey

`ToggleKey` accepts any Unity [KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) name, e.g. `F8`, `BackQuote`, `L`.

## Uninstall

Delete the following from your game folder. Your saves are not touched (your language preference stays in the game's own `gamesettings.sav`):

- `BepInEx\`
- `dotnet\`
- `winhttp.dll`
- `doorstop_config.ini`

## Troubleshooting

- **Hotkey does nothing**: Make sure `ToggleKey` in `BepInEx\config\zp.langtoggle.cfg` is a valid Unity `KeyCode` name (`Semicolon`, `F8`, `BackQuote`, etc.).
- **Invalid LanguageA/B in config**: You typed an unknown language code. Pick one from the table above.
- **Mod stops working after a game update**: Please open an issue with the game version and your log.

## Building from source

Requirements:

1. .NET 6 SDK
2. A local install of *Zero Parades*
3. [BepInEx-Unity.IL2CPP](https://builds.bepinex.dev/projects/bepinex_be) v6.0.0-be.755 or newer extracted into the game folder, and the game launched once so BepInEx generates `BepInEx\interop\` (the wrapper assemblies the build references)

Build and deploy to the game's plugin folder:

```powershell
dotnet build -c Release -p:GameDir='<full path to game folder>'
```

You can omit `-p:GameDir` only if your game is at the standard Windows Steam path (`C:\Program Files (x86)\Steam\steamapps\common\Zero Parades`).

## License

- This mod: MIT (see `LICENSE`)
- BepInEx (bundled with releases): LGPL 2.1
