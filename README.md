> **Branch notice:** This branch includes additional gameplay changes on top of the build and release work kept on `Reborn`:
>
> - Ice fishing hole state sync, including fixes for scene transitions and menu scene detection.
> - Animal corpse sync on death. The owner keeps its local vanilla corpse, remote clients spawn the proper carcass prefab, and spawner-less animals are cleaned up and synced.
>
> Corpse sync is currently Phase 1: late join, scene restart, and corpse harvest/pickup/removal are not synced yet.

> **Fork notice:** This is a fork of [Filigrani/SkyCoop](https://github.com/Filigrani/SkyCoop), focused on adding dedicated server builds for Linux/Windows and the GitHub Actions release workflow. All original mod work belongs to the upstream project.

# Game Version Compatibility

| Branch | Game Versions | Status |
|--------|--------------|--------|
| [Reborn](https://github.com/Filigrani/SkyCoop/tree/Reborn) | 2.59+ | Playable |
| [BeforeTFTFT](https://github.com/Filigrani/SkyCoop/tree/BeforeTFTFT) | 2.01 – 2.02 | Legacy Version |
| [main](https://github.com/Filigrani/SkyCoop/tree/main) | — | Legacy codebase (abandoned) |

> For setup help, join the [Discord](https://discord.gg/ydmufU2HHj).

# SkyCoop Reborn

**Sky Co-op Reborn** is multiplayer mod for [The Long Dark](https://store.steampowered.com/app/305620) game, that powered by [Melon Loader](https://github.com/LavaGang/MelonLoader) by [Lava Gang](https://github.com/LavaGang).

Reborn is not just updated versions of the mod from 2.02 versions of the game but writen by hand from ground up entirely new mod, to support newest versions of the game [The Long Dark](https://store.steampowered.com/app/305620), and latest [Melon Loader](https://github.com/LavaGang/MelonLoader) version.

# Running a dedicated server

Prebuilt server archives are published on the [Releases](../../releases/latest) page:

- Linux: `linux-x64`, `linux-arm64`, `linux-arm`
- Windows: `win-x64`, `win-x86`

Extract the archive for your platform and start the server:

**Linux**

```bash
chmod +x SkyCoopServerHost
./SkyCoopServerHost
```

**Windows**

```powershell
.\SkyCoopServerHost.exe
```

The server listens on UDP port `37855` by default. Open or forward that port if clients connect over the internet. Stop the server with `Ctrl+C`; it saves the world before shutting down.

## Server configuration

The archive includes a `Server.json` next to the executable:

```json
{
  "ServerName": "SkyCoop Reborn Server",
  "MaxPlayers": 10,
  "Seed": 0,
  "Port": 37855,
  "VoicePort": 0,
  "ExperienceMode": "Stalker",
  "Cheats": false,
  "AdminPassword": "",
  "JoinPassword": "",
  "TrustLoopback": true
}
```

| Field | Description |
|-------|-------------|
| `ServerName` | Name shown to clients. |
| `MaxPlayers` | Maximum number of players. |
| `Seed` | World seed. `0` generates a random seed. |
| `Port` | UDP port the server listens on. |
| `VoicePort` | Voice chat port. `0` disables voice chat. |
| `ExperienceMode` | Experience mode, e.g. `Stalker`. |
| `Cheats` | Whether cheats are allowed. |
| `AdminPassword` | Password that grants operator commands via `sv_cmd login <password>`. Empty means no password login; loopback peers are operators when `TrustLoopback` is true. |
| `JoinPassword` | Connection password clients must supply. Empty means the default key is used. |
| `TrustLoopback` | When true (default), connections from the server machine are operators without logging in. |

With `JoinPassword` set, clients enter the password as part of the server address: `password@host:port`. The address can also be `host:port` or `host` (the default port is `37855`). The password is everything before the last `@`, so it may contain `@` characters; the host may be an IPv4 address, a hostname, or an IPv6 address in brackets, e.g. `password@[::1]:37855`. `JoinPassword` protects only the game port; the voice server still uses the default voice key and is not protected by it.

Operators log in from the in-game console with `sv_cmd login <password>`. Successful login unlocks server operator commands sent through `sv_cmd`. Failed logins are throttled and repeated failures disconnect the client. Set `TrustLoopback` to `false` to require the admin password even for connections from the server machine.

Passwords are never sent to clients and are never written to the log. Set a strong `AdminPassword` and `JoinPassword` for internet-facing servers.

The server uses a `SkyModData` folder next to the executable. Saves are stored in `SkyModData/ServerSaves`. Game modes, maps, loot tables, and weather profiles are loaded from the matching folders under `SkyModData`.

Release archives are self-contained: the GitHub Actions workflow downloads the upstream Reborn `SkyModData` (game modes, weather, gear and wildlife spawns, and dupe lists) from the pinned upstream release and bundles it into every server archive. The upstream release tag is set with `UPSTREAM_RELEASE_TAG` in `.github/workflows/release.yml`; update it when syncing to a newer upstream Reborn release. Upstream does not currently ship `Maps` or `LootTables`, so the server creates those folders empty and you can add custom data there.

Local source builds do not download the upstream data. Copy it into `SkyModData` yourself if you want to run a locally built server.

## Building the server from source

With the .NET 6 SDK installed:

```bash
dotnet publish SkyCoopServerHost/SkyCoopServerHost.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o publish/linux-x64
```

Replace `linux-x64` with `linux-arm64`, `linux-arm`, `win-x64`, or `win-x86` to build for another platform. Tagged builds are produced automatically by the GitHub Actions release workflow.

# FAQ

- Q. **Is reborn made by same team as old mod?**
	A. Yes

- Q. **Is it AI slop?**
	A. No, mod is written by hand, almost 2 years of work of uneployeed labour was put in to it, you can look at [commits](https://github.com/Filigrani/SkyCoop/commits/Reborn/) to see history of our changes and obserer how reborn step-by-step end up being the modification it is today.

- Q. **Reborn supports DLCs?**
	A. Mod suppots playing simultaneously with players who don't have any DLCs, and with owners of Tales From The Far Territory and Iron Road, in any combinations. However, not all the conent of this DLCs is currently 100% synced.

- Q. **Is it 100% complete?**
	A. No, however major part of the work has already been done. Full list of features will be listed soon.

More FAQ in our [Discord](https://discord.gg/ydmufU2HHj).

We don't own [The Long Dark](https://store.steampowered.com/app/305620)! [The Long Dark](https://store.steampowered.com/app/305620) belong to [Hinterland Studio Inc](https://hinterlandgames.com/).
