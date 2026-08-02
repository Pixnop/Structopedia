# Structopedia

[![ci](https://github.com/Pixnop/Structopedia/actions/workflows/ci.yml/badge.svg)](https://github.com/Pixnop/Structopedia/actions/workflows/ci.yml)

Structopedia puts worldgen structures in the Vintage Story survival handbook. Each structure
gets its own page with an interactive 3D preview of the schematic the game actually places at
worldgen, so a ruin or a trader post can be inspected before it is ever found in a world.
Structures added by installed mods are picked up the same way, since the mod reads whatever
schematics are registered rather than a hardcoded list. Everything runs client-side and
nothing about worldgen or gameplay is changed.

Pre-release. This repository currently holds the build setup, an empty client mod system and
the test and CI plumbing.

## Requirements

The .NET 10 SDK, and a Vintage Story 1.22.6 install to compile against. Set the
`VINTAGE_STORY` environment variable to the directory holding `VintagestoryAPI.dll`. That
directory also needs `Mods/VSSurvivalMod.dll`, which is where the handbook and the structure
worldgen code live. A server install works as well as a client one, which is what CI uses.

## Build

```
dotnet build Structopedia.slnx -c Release
```

Warnings are errors across the solution, and public members need XML documentation. Internal
and private members do not: see `stylecop.json`.

## Test

```
dotnet test tests/Structopedia.Pure.Tests -c Release
```

Unit tests only. They run against the API assembly without launching the game, so they stay
fast enough to run on every commit. CI runs the same two commands on every push and pull
request.

## Package

```
dotnet build src/Structopedia/Structopedia.csproj -c Release -t:PackMod
```

Writes `release/Structopedia-<version>.zip`, holding the mod dll, `modinfo.json` and the
`assets` tree once there is one. The game dlls are left out, the game provides them at
runtime.

## Layout

```
src/Structopedia/                 the mod
tests/Structopedia.Pure.Tests/    unit tests
```

## License

See [LICENSE](LICENSE).
