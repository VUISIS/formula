# FORMULA 2.0 - Jupyter Notebook Kernel

### Requirements
```bash
python 3.10
dotnet 6.0
```

### Pip Requirements
```bash
jupyter
pythonnet
```

### With .NET on x64 or Apple Silicon ARM64
To build the command line interpreter, run the following commands from Src/CommandLine.

```bash
$ cd Src/CommandLine
$ dotnet build CommandLine.sln /p:Configuration=Release /p:Platform=x64|ARM64
$ dotnet ./bin/Release/<OS>/<PLATFORM>/net6.0/CommandLine.dll
```

### Copy CommandLine build files into the CommandLine folder.
```bash
$ cp ./bin/Release/<OS>/<PLATFORM>/net6.0/* ../Kernel/FormulaPy/CommandLine
```