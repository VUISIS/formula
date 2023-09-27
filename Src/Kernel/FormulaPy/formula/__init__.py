__version__ = '0.0.1'

from pythonnet import load
load("coreclr")
import clr
d = __file__.replace("__init__.py","")
clr.AddReference(d + "CommandLine/VUISIS.Formula.x64.dll")

from .formula_magic import FormulaMagics

def load_ipython_extension(ipython):
    ipython.register_magics(FormulaMagics)