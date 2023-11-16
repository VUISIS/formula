# FORMULA 2.0 - Jupyter Notebook Kernel

### Requirements
```bash
python >= 3.10
dotnet 6.0
```

### Install and Set Env
```bash
dotnet tool install --global VUISIS.Formula.<x64|ARM64> 

Set the environment variable OPENAI_API_KEY before running.
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