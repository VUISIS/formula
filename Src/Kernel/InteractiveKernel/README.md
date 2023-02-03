# FORMULA 2.0 Jupyter Notebook Kernel
Formal Specifications for Verification and Synthesis

## Building FORMULA Kernel

In order to build FORMULA, you will need .NET core installed.

Clone modified jupyter-core submodule into the Src\Kernel folder.

```bash
cd to top directory
git submodule init
git submodule update --remote
```

To build the interactive kernel, from Src\Kernel\InteractiveKernel:

```bash
dotnet build InteractiveKernel.csproj /p:Platform=x64|ARM64 /p:Configuration=Release|Debug
```

Install jupyter globally or install with dotnet and --sys-prefix if using conda or venv.

```bash
sudo -H pip3 install jupyter
```

To install the kernelspec to jupyter, from Src\Kernel\InteractiveKernel:

```bash
dotnet pack --no-build /p:Platform=x64|ARM64 /p:Configuration=Debug|Release

dotnet nuget add source Src/Kernel/InteractiveKernel/bin/<Configuration>/<OS>/<Platform>

dotnet tool install -g Microsoft.Jupyter.VUISIS.Formula

dotnet formula install
```