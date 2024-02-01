# FORMULA 2.0 - Formal Specifications for Verification and Synthesis
[![build](https://github.com/VUISIS/formula-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/VUISIS/formula-dotnet/actions/workflows/build.yml)

## Building and running FORMULA
### Dotnet Tool Install
To install Formula 2.0 as a dotnet tool, run

For x64
```bash
$ dotnet tool install --global VUISIS.Formula.x64 
```

For arm64
```bash
$ dotnet tool install --global VUISIS.Formula.ARM64 
```

Tool Command
```bash
Note: For Linux and MacOS, you may need to add the dotnet tools path to the system path.
$ formula
```

### With Nix flakes (macOS/Linux)
To build and run the command line interpreter with Nix flakes, run

```bash
$ nix run github:VUISIS/formula-dotnet
```

### With .NET on x64 or Apple Silicon ARM64
To build the command line interpreter, run the following commands from Src/CommandLine.

```bash
$ dotnet build CommandLine.sln /p:Configuration=Debug|Release /p:Platform=x64|ARM64

For Windows x64
$ dotnet ./bin/<Configuration>/Windows/x64/net6.0/VUISIS.Formula.x64.dll

For Linux x64
$ dotnet ./bin/<Configuration>/Linux/x64/net6.0/VUISIS.Formula.x64.dll

For MacOS x64
$ dotnet ./bin/<Configuration>/MacOS/x64/net6.0/VUISIS.Formula.x64.dll

For MacOS ARM64
$ dotnet ./bin/<Configuration>/MacOS/ARM64/net6.0/VUISIS.Formula.ARM64.dll
```

To run unit tests with Formula, run the following command from
Src/Tests.

### Building Nuget Package & Installing Tool Locally
To build the formula nuget package, run from the folder Src/CommandLine dotnet pack. Run dotnet tool to install the program and add the folder containing the nupkg with --add-source.

```bash
$ dotnet pack /p:Configuration=Debug|Release /p:Platform=x64|ARM64
$ dotnet tool install --add-source ../../nupkg -g formula 
```

```bash
$ dotnet test Tests.csproj /p:Configuration=Debug|Release /p:Platform=x64|ARM64

For specific tests
$ dotnet test Tests.csproj /p:Configuration=Debug|Release /p:Platform=x64|ARM64 --filter "FullyQualifiedName=<NAMESPACE>.<CLASS>.<METHOD>"
```

You can exit the command line interpreter with the "exit" command.
