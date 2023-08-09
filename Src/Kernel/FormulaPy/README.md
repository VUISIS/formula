# FORMULA 2.0 - Jupyter Notebook Kernel

### Requirements
```bash
python >= 3.10
dotnet 6.0
```

### With .NET on x64 or Apple Silicon ARM64
```bash
$ cd Src/CommandLine
$ dotnet build CommandLine.sln /p:Configuration=Release /p:Platform=x64|ARM64
```

### Copy CommandLine build files into the CommandLine folder.
```bash
$ cp -r Src/CommandLine/bin/Release/<OS>/<PLATFORM>/net6.0/* Src/Kernel/FormulaPy/CommandLine
```

### Install FormulaPy kernel
```bash
$ cd Src/Kernel/FormulaPy
$ pip install .
```

### Ubuntu 22.04 libhostfxr Issue
```bash
$ sudo ln -s /usr/lib/dotnet/host/fxr/<version>/libhostfxr.so /usr/lib/dotnet
$ conda install -c conda-forge libstdcxx-ng=12
```

### Add to config.py with OpenAI API key in SelfRepairLLM/src
```bash
$ cfg = {
	"OPENAI_API_KEY" : "",
	"ACTIVELOOP_TOKEN" : "(OPTIONAL)",
	"LANGCHAIN_API_KEY" : "(OPTIONAL)"
}
```