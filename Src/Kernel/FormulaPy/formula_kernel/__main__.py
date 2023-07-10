from ipykernel.kernelapp import IPKernelApp
from . import FormulaKernel

IPKernelApp.launch_instance(kernel_class=FormulaKernel)