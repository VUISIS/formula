__version__ = '0.0.1'

import os
os.environ["FORMULA_KERNEL"] = "0"
from pythonnet import load
load("coreclr")
import clr
d = __file__.replace("__init__.py","")
clr.AddReference(d + "CommandLine/CommandLine.dll")

from .formula_magic import FormulaMagics

def load_ipython_extension(ipython):
    ipython.register_magics(FormulaMagics)