__version__ = '0.0.1'

from .formula_magic import FormulaMagics

def load_ipython_extension(ipython):
    ipython.register_magics(FormulaMagics)