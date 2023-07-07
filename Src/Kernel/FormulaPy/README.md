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
```bash
$ cd Src/CommandLine
$ dotnet build CommandLine.sln /p:Configuration=Release /p:Platform=x64|ARM64
$ dotnet ./bin/Release/<OS>/<PLATFORM>/net6.0/CommandLine.dll
```

### Copy CommandLine build files into the CommandLine folder.
```bash
$ cp -r ./bin/Release/<OS>/<PLATFORM>/net6.0/* ../Kernel/FormulaPy/CommandLine
```

### Install FormulaPy kernel
```bash
$ cd Src/Kernel
$ jupyter kernelspec install --user FormulaPy
```