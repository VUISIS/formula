__version__ = '0.0.1'

import os
from pythonnet import load
load("coreclr", runtime_config=os.path.abspath('./formula/CommandLine/runtimeconfig.json'))
import clr
clr.AddReference(os.path.abspath('./formula/CommandLine/CommandLine.dll'))

from .formula_magic import FormulaMagics

def load_ipython_extension(ipython):
    ipython.register_magics(FormulaMagics)