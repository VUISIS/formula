__version__ = '0.0.1'

import os
from pythonnet import load
# TODO: Make sure to fix the coreclr path
# fix the double coreclr loading from formula_tools in SelfRepairLLM 
# load("coreclr", runtime_config=os.path.abspath('./formula/Src/Kernel/FormulaPy/formula/CommandLine/runtimeconfig.json'))
load("coreclr")
import clr
clr.AddReference(os.path.abspath('./formula_new/formula/Src/Kernel/FormulaPy/formula/CommandLine/CommandLine.dll'))

from .formula_magic import FormulaMagics

def load_ipython_extension(ipython):
    ipython.register_magics(FormulaMagics)