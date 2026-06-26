# Y WONDER GREEN FARM

Unity farming/life-sim demo project for the Y Wonder Hub ecosystem.

This repository is intended for internal development, QA handoff, and build
handoff. The `main` branch should be treated as the current stable build
candidate. Active integration work should happen on `dev` first.

## Current Demo Scope

The current demo focuses on:

- Farming and animal husbandry loops.
- Build mode for farm plots, paths, and pens.
- Inventory, shops, crafting/workshop-style UI, tasks, mailbox, leaderboard,
  piggy bank, events, and daily rewards.
- Tutorial/profile flow with existing-character login support.
- Minimal backend connectivity for authentication and player profile flags.
- Android/Windows builds from Unity Editor.
- iOS CI setup through CodeMagic.

This is not yet a full online game backend. See "Backend Status" below.

## Requirements

- Unity Editor `6000.3.15f1`.
- Git and Git LFS.
- Android Build Support, OpenJDK, Android SDK/NDK if building APK/AAB.
- iOS Build Support if exporting an iOS Xcode project from Unity.
- macOS + Xcode, or CodeMagic, for actual iOS `.ipa` signing/build output.

Visual Studio is useful for local C# editing, but it is not required for the
CodeMagic iOS workflow itself.

## Clone and Open

```bash
git clone <repo-url>
cd BaChuKhuRung3D
git lfs pull
```

Open the project with Unity Hub using Unity `6000.3.15f1`.

If Unity asks to switch platform, only switch to the target you actually need:

- Android for APK/AAB testing.
- Windows/Mac/Linux for desktop testing.
- iOS only when exporting an Xcode project or validating iOS player settings.

## Branches

- `main`: stable build candidate for QA/build handoff and CodeMagic.
- `dev`: integration branch for new work before it is merged to `main`.
- `feat/*`: feature/history branches. Do not assume these are release-ready.

## Build Notes

### Windows

Use Unity Editor build settings for Standalone.

### Android

Use Unity Editor with Android Build Support installed. Check player settings,
orientation, package name, and demo/test flags before producing an APK/AAB.

### iOS

The repository includes `codemagic.yaml` and
`Assets/_Project/Editor/BuildScript.cs` for CodeMagic iOS export/build.

The CodeMagic workflow expects Unity `6000.3.15f1` and Unity license variables
configured in CodeMagic, for example:

```text
UNITY_SERIAL
UNITY_EMAIL
UNITY_PASSWORD
```

iOS signing is not stored in this repository. The build owner must configure
Apple signing in CodeMagic or Xcode:

- Bundle ID.
- Apple Developer Team.
- Certificate/provisioning profile, or App Store Connect API key.

Never commit Unity, Apple, VPS, or backend secrets into this repository.

## Backend Status

The Unity client has a REST backend layer and a minimal demo backend flow.

Currently covered:

- `auth/register`
- `auth/login`
- `player/profile`
- `tutorialCompleted`
- `characterCreated`

Important limitation: this is not yet the final online backend for all game
state. Inventory, POS, farm/build state, animal/crop persistence, realtime
multiplayer interaction, chat persistence, server time, IAP, and anti-cheat
are separate backend scope items.

For demo/testing, the client still contains local/offline fallback behavior.

## Test Accounts

Demo account credentials are managed internally. Do not add real usernames,
passwords, API keys, certificates, or server passwords to this README.

Ask the project owner for current QA credentials and test data.

## Useful Project Paths

- `Assets/_Project/`: primary project scripts, UI, prefabs, and resources.
- `Assets/_Project/Docs_KichBan/`: customer scenario and spreadsheet-derived
  design documents.
- `docs/`: technical notes, changelog mirror, API/backend notes, recovery docs.
- `server/`: minimal Node backend stub used for demo/backend proof.
- `ProjectSettings/`: Unity project settings.
- `codemagic.yaml`: CodeMagic CI workflow for iOS.

## Recovery and Handoff Docs

When resuming work, read these first:

1. `RULES.md`
2. `docs/CONTEXT_RECOVERY.md`
3. `Assets/_Project/Docs_KichBan/LoTrinh_Demo_Thu2.md`
4. `task.md`
5. `CHANGELOG.md`

## Known Limitations

- Disease, vaccine, and treatment cost logic is intentionally incomplete until
  customer data is finalized.
- Backend integration is currently minimal and does not represent the full
  online-game architecture.
- iOS builds require external Apple signing setup.
- Before a real release build, verify demo/test loadout flags, backend URL, app
  identifier, icons, build scenes, and platform-specific player settings.

