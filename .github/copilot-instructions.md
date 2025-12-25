# GitHub Copilot / AI agent instructions — SVS-PelvicFin

Purpose: Give AI coding agents the repository-specific context they need to be productive quickly (architecture, build/deploy, conventions, and examples).

## Quick start (build & deploy) ✅
- Build a game target: `dotnet build ./SVS -c Debug` or `dotnet build ./AC -c Debug`. Workspace tasks are available: **dotnet: build debug** / **dotnet: build release**.
- Debug deploy: `Tasks.xml` "Deploy" target (runs after Debug build) copies the built DLL to `%GamePath%\BepInEx\plugins\`. `GamePath` is read from registry (HKCU\Software\ILLGAMES\<GameName>), or override with MSBuild: `dotnet build /p:GamePath="C:\Games\SamabakeScramble" -c Debug`.
- Release packaging: `Tasks.xml` "Release" target copies to `..\Release\$(GameName)\BepinEx\plugins` and zips `$(AssemblyName).zip` in repo root.
- If `SVS-Fishbone` (CoastalSmell) isn't present, `Tasks.xml` will auto-clone it (requires `git` in PATH).

## Big picture architecture 🔧
- Single shared implementation in `PelvicFin.cs` with game-specific partials in `AC/AC_PelvicFin.cs` and `SVS/SVS_PelvicFin.cs`. Each project produces its own DLL: `AC_PelvicFin.dll` and `SVS_PelvicFin.dll`.
- Projects set `<GameName>` and `<DefineConstants>` in their csproj. Code uses `#if <GameName>` to enable per-game features (e.g. `SonSize`).
- Plugin is a BepInEx IL2CPP plugin: `Plugin : BasePlugin`, decorated with `[BepInProcess]`, `[BepInPlugin]`, and depends on `CoastalSmell` via `[BepInDependency]`.
- UI and runtime helpers come from CoastalSmell (SVS-Fishbone): `UGUI`, `WindowConfig`, `SingletonInitializerExtension`, reactive patterns (System.Reactive and Cysharp UniTask).

## Project-specific conventions & patterns 💡
- Partial-Plugin pattern: Put cross-game logic in `PelvicFin.cs`; per-game constants in the respective partial class files. To add support for another game, add a new csproj and a small partial `Plugin` declaring `public const string Process = "NewGame"` and `GameName` in the csproj.
- UI pattern: props are grouped into enums `ToggleProp`, `RingProp`, `RangeProp`. Each enum has a `PrepareTemplate` and `Subscribe` implementation via extension methods (`ToggleExtension`, `RingExtension`, `RangeExtension`). When adding a UI control, add an enum value and implement its Transform/Get/Set mapping to `Human`.
- Keyboard shortcuts / config: Window initialized with `new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl)`; default is `Ctrl+P` (documented in README).
- Changes to public API or behavior should be reflected in `README.md` and release `Version` in `Plugin.Version` constant.

## Integrations & dependencies 🔗
- Essential packages: `BepInEx.Unity.IL2CPP`, `IllusionLibs.<Game>.AllPackages` (set per project), `System.Reactive`.
- Local project link: `Tasks.xml` references `SVS-Fishbone` (CoastalSmell) via `ProjectReference` using `GamePrefix`. Ensure `SVS-Fishbone` exists under sibling directory or let `Tasks.xml` clone it.
- NuGet feed configured in `nuget.config` includes `BepInEx` source.

## Debugging & testing tips 🐞
- No automated unit tests in repo. Manual test: build Debug, ensure the DLL is in the game's `BepInEx\plugins` folder, start the game, open character creation / h scene and press `Ctrl+P` to show the UI.
- Check `BepInEx/LogOutput.log` for plugin load messages and exceptions.
- When reproducing issues across Aicomi vs SamabakeScramble, remember conditional compilation (`#if`) can change behavior; test both DLLs.
- To run the build on CI or a machine without registry entries, pass `-p:GamePath` when building.

## Release process 📦
- Bump `Plugin.Version` in `PelvicFin.cs` then run a Release build (`dotnet build -c Release`) — `Tasks.xml` will prepare `$(AssemblyName).zip` in the repo root.
- Keep `README.md` migration notes up to date (e.g., `PelvicFin.dll` -> `SVS_PelvicFin.dll` / `AC_PelvicFin.dll`).

## What AI agents should and should not do 🤖
- Do: Make small, incremental changes, add/update UI following the `enum -> PrepareTemplate -> Subscribe -> Transform` pattern, update README and Version on releases, and run a Debug build before PR.
- Do: Reference `PelvicFin.cs`, `Tasks.xml`, `AC/` and `SVS/` files in PR descriptions to explain game-specifics.
- Don’t: Add or assume hidden runtime facts (game installation paths) — ask maintainers when the environment or test steps are unclear.

## Quick examples
- Add a new Toggle control named `Foo`:
  1. Add `Foo` to `ToggleProp` enum.
  2. Add `case ToggleProp.Foo => (human.IsFoo, human.SetFoo)` mapping in `ToggleExtension.Transform` (implement `IsFoo`/`SetFoo` using `human.data` or `human.GetRefTransform(...)` as needed).
  3. Rebuild Debug, deploy, test in-game via `Ctrl+P`.

---
If anything is unclear or you'd like additional examples (e.g., how to attach a debugger to the game process or how to add a new game target), tell me which section to expand and I will iterate. 🔁
