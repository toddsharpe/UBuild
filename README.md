# UBuild

Very simple build system for small projects, accepting various unix style toolchains (for cross-compilation, etc).

Builds are always full builds. There is no dependency graph and nothing is cached between runs, which keeps
the tool small and its output predictable; compiles run in parallel to make that cheap.

## Install

Download a release and put it on your PATH:

```
curl -sL https://github.com/toddsharpe/UBuild/releases/latest/download/ubuild-linux-x64.tar.gz | tar xz
```

The binary is natively compiled and self contained, so no .NET runtime is required. Releases are
linux-x64 only; elsewhere, build from source.

To build from source instead:

```
dotnet publish Src/UBuild/UBuild.csproj -c Release -r linux-x64 -o publish
```

## Commands

Run from the directory holding `Env_build.json`.

| Command | What it does |
| --- | --- |
| `UBuild build` | Build a project (`-p`) or a single exe (`-e`). Defaults to every project. |
| `UBuild run` | Build one exe (`-e`) and execute it. |
| `UBuild package` | Zip a project (`-p`) into `bin/` plus `dat/`. |
| `UBuild clean` | Delete the object and exe output directories. |
| `UBuild list` | Show the toolchains, scripts and projects defined here. |
| `UBuild --version` | Print the version and exit, without reading a config. |

Options: `-e/--exe`, `-p/--project`, `-t/--toolchain`, `-s/--script`, `-a/--args`, `-f/--file`,
`-j/--jobs`, `-v/--verbose`, `--unity`, `--no-unity`.

```
UBuild build                           # build every project, each exe once
UBuild build -p Hosted                 # build a project
UBuild build -p Hosted -t Host         # ...only the exes that project builds with Host
UBuild build -e Hosted/MyHosted        # build a single exe
UBuild run -e Hosted/MyHosted          # build it and run it
UBuild run -e Linux/Test -a "--quick"  # ...passing it arguments
UBuild build -p Hosted -j 4            # cap concurrent compiler processes (default: processor count)
UBuild build -p Hosted -v              # print every command
```

`-e` takes the toolchain from the project entries that name that exe, so `-t` is only needed when
they disagree. `run` exits with the program's own exit code, so it can gate a test suite.

Exes build at once, and `-j` is the budget for every compiler process across all of them rather than
per exe. Each exe's output is held and printed in one piece, so concurrent builds stay readable, and
a build that fails names every exe that failed rather than stopping at the first.

An exe listed by several projects is built once per `UBuild build`, not once per project.

## Layout

```
Env_build.json          the environment: directories, projects, toolchains
Exes/<path>_exe.json    one file per exe: sources and flags
Src/                    sources ("Sources")
Configs/                files packaged into dat/ ("Configs")
build_obj/              objects, per toolchain and exe ("Output" + _obj)
build_exe/              linked binaries ("Output" + _exe)
```

All config files are JSON and accept `#` line comments.

## Env_build.json

```json
{
    "Output": "build",
    "Sources": "Src",
    "Exes": "Exes",
    "Configs": "Configs",

    "IncludeDirs": [ "Include" ],       # applied to every exe
    "Defines": [ "VERSION_BUMP=1" ],    # applied to every exe

    "Projects": [
        {
            "Name": "Hosted",
            "Exes": [
                { "Name": "Hosted/MyHosted", "Toolchain": "Host" },
                { "Name": "Ground/Web", "Script": "Blazor" }
            ],
            "Configs": [ "File1", "Dir1" ]
        }
    ],

    "Scripts": [
        { "Name": "Blazor", "Location": "Scripts/Blazor_build.sh" }
    ],

    "Toolchains": [
        { "Name": "Host", "Bin": "/usr/bin" },
        {
            "Name": "Stm32",
            "Bin": "/usr/bin",
            "Prefix": "arm-none-eabi-",
            "Ext": ".elf",
            "Flags": [ "-mthumb", "-mcpu=cortex-m7" ],
            "LinkFlags": [ "-specs=nano.specs" ]
        }
    ]
}
```

A project entry names either a `Toolchain` (compiled by UBuild) or a `Script` (handed to a bash script,
for targets UBuild does not build itself). `"Toolchain": "ALL"` builds that exe with every toolchain.

A toolchain is a `Bin` directory plus a `Prefix`; the tools are derived from it, so `Prefix: "arm-none-eabi-"`
gives `arm-none-eabi-gcc`, `arm-none-eabi-objcopy` and so on.

`Bin` and `CXX` expand `${VAR}` and `${VAR:-fallback}` from the environment, as do include directories,
so a pinned toolchain or a sibling checkout can live wherever it was put rather than being symlinked
into the path the config names. Without a fallback an unset variable is left literal, so the failure
names it rather than showing a gap.

```json
{ "Name": "Stm32", "Bin": "${ARM_GCC_BIN:-/usr/bin}", "Prefix": "arm-none-eabi-" }
```

## Exe files

`Exes/Hosted/MyHosted_exe.json` describes one binary. Sources are relative to the sources directory and
accept a `*` wildcard.

```json
{
    "Name": "MyHosted",
    "Sources": [
        "Hosted/MyHosted/*.cpp",
        "Core/*.cpp",
        "Stm32/startup.s"
    ],
    "IncludeDirs": [ "Src/External/printf" ],
    "Defines": [ "STM32H753xx" ],
    "Flags": [ "-O3", "-Wall" ],
    "CppFlags": [ "-std=c++20", "-fno-exceptions" ],
    "LinkFlags": [ "-Wl,-Map=$BinFile.map" ],
    "PreBuild": [ "$Bash: Scripts/GenerateMeta.sh" ],
    "PostBuild": [ "$ObjCopy: -O binary $BinFile.elf $BinFile.bin" ]
}
```

`Sources` and `IncludeDirs` expand `$Toolchain` and `$ExeName` too, which is how a file a pre-build
step generates belongs to the exe and toolchain that generated it rather than being shared with
whoever else names the same path:

```json
    "PreBuild":    [ "$Bash:./generate.sh gen/$Toolchain/$ExeName" ],
    "Sources":     [ "../gen/$Toolchain/$ExeName/program.cpp" ],
    "IncludeDirs": [ "gen/$Toolchain/$ExeName" ]
```

`.c` compiles with gcc, `.cc`/`.cpp` with g++, `.s` with gcc as assembler; any other extension is an
error rather than a silently dropped file. Objects land under `build_obj/<toolchain>/<exe>/`, so the
same source used by two exes or two toolchains never collides.

### Extends

An exe can take another exe's lists as its own starting point, which is what keeps a board's recipe
down to the lines that are actually about that board.

```json
{
    "Name": "FlightH7_1v1",
    "Extends": "Stm32/Base",
    "Sources": [ "Stm32/Boards/BasicFc1v1.cpp" ],
    "Defines": [ "HSE_VALUE=8000000UL" ]
}
```

Lists concatenate, base first; `Name`, `Unity` and `UnityBatchSize` take the derived value when it
sets one. It is one level deep: a base that itself extends something is an error. A base exe is
never built on its own, so it needs no `main`.

### Unity builds

Opt in per exe to compile the C++ sources as a few generated files that `#include` them, instead of one
translation unit each. Headers are then parsed once per batch rather than once per source, which is the
whole point; since every build is a full build, there is no incremental cost to trade away.

```json
    "Unity": true,
    "UnityBatchSize": 8,                # sources per generated file, 0 for the default (8)
    "UnityExclude": [ "External/*" ]    # compiled on their own, as usual
```

`--unity` and `--no-unity` override the config, which is the honest way to time it both ways:

```
UBuild build -p FlightH7 --unity
UBuild build -p FlightH7 --no-unity
```

Only C++ batches. C and assembly always compile on their own, and generated files land in
`build_obj/<toolchain>/<exe>/unity_N.cpp` so `clean` removes them.

Things to know before turning it on:

- **Sources stop being isolated.** A `using namespace` leaks into the rest of its batch, and two files
  that each define the same macro differently would silently take the first definition. UBuild refuses
  to build that case and names both files; exclude one to resolve it. Anonymous namespaces and file
  statics that collide by name will fail to compile.
- **Third party code is the usual casualty.** Excluding vendored sources is a good default, since it is
  the code you least want to patch.
- **Codegen changes.** More inlining means the binary is not byte identical, so on embedded targets
  re-check flash usage and timing.
- Compile errors still report the original file and line, but the first error stops a whole batch.
- `compile_commands.json` keeps describing each source file on its own, so editors are unaffected.

### Pre and post build steps

A step is `$ToolchainProperty: args`, where the property names a tool on the toolchain (`$Gcc`, `$ObjCopy`,
`$ObjDump`, `$Size`, `$Stat`, `$Bash`, ...). Arguments expand `$BinFile`, `$OutDir`, `$ExeName` and
`$Toolchain`, and the
same values are exported as environment variables along with every toolchain path, plus `$SrcDir` and
`$OutFile` — the built artifact, extension included, which `$BinFile` does not carry.

Only the first colon separates the tool from its arguments, so an argument may contain one. Steps run
without a shell, so redirection and pipelines belong in a script reached through `$Bash`.

## Editor support

Every build writes `compile_commands.json` next to `Env_build.json`, so clangd and IDEs resolve includes
exactly as the build does. Each build merges its own entries into what is already there, so building one
exe re-indexes that exe and leaves the rest of the tree alone. `UBuild clean` deletes the file, which is
how a source that no longer exists leaves the index.
