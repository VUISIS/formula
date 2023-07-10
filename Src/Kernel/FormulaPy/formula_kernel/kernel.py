from ipykernel.kernelbase import Kernel
import re
import os
from .utils import cmd_regex

import os
from pythonnet import load
load("coreclr", runtime_config=os.path.abspath('./formula_kernel/CommandLine/runtimeconfig.json'))
import clr
clr.AddReference(os.path.abspath('./formula_kernel/CommandLine/CommandLine.dll'))

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console  

class FormulaKernel(Kernel):
    implementation = 'formula'
    implementation_version = '1.0'
    language = 'python' 
    language_version = '3.10'
    language_info = {'name': 'python',
                     'mimetype': 'text/plain',
                     'extension': '.py'}
    banner = "Formula 2.0"

    def __init__(self, **kwargs):
        """Initialize the kernel."""
        super().__init__(**kwargs)

        self.sw = StringWriter()
        Console.SetOut(self.sw)
        Console.SetError(self.sw)

        sink = CommandLineProgram.ConsoleSink()
        chooser = CommandLineProgram.ConsoleChooser()
        self.ci = CommandInterface(sink, chooser)
        self.ci.DoCommand("wait on")
        self.sw.GetStringBuilder().Clear()
    
    def load(self, path):
        return self.ci.DoCommand("load " + path)
    
    def help(self):
        output = "Formula python commands:\n"
        output += "  loadf(path) - Loads and compiles a file that is not yet loaded. Use: load filepath.\n"
        output += "  helpf()     - Prints this message."
        return output

    def send_command_output(self, silent, command_name, data):
        if not silent:
            self.send_response(
                self.iopub_socket,
                'stream', {
                    'name': 'stdout',
                    'data': ('Running formula command {n}'.format(n=command_name))
                })

            content = {
                'source': 'kernel',
                'data': {
                    'text/plain': data
                },
                'metadata' : {
                    'text/plain' : {}
                }
            }

            self.send_response(self.iopub_socket,
                'display_data', content)


    def do_execute(self, code, silent,
                   store_history=True,
                   user_expressions=None,
                   allow_stdin=False):
        return_status = "error"
        functions = code.split('\n')
        for function in functions:
            match = re.search(cmd_regex, function)
            if match:
                type, params = match.groups()
                if type:
                    match type:
                        case 'loadf':
                            if params:
                                param = params.split(",")
                                param[0] = re.sub('\"', '', param[0])
                                if os.path.isfile(param[0]):
                                    status = self.load(param[0].strip())
                                    if status:
                                        self.send_command_output(silent, type, self.sw.ToString())
                                        self.sw.GetStringBuilder().Clear()
                                        return_status = 'ok'
                                    else:
                                        return_status = 'error'
                                else:
                                    return_status = 'error'
                            else:
                                return_status = 'error'
                        case 'helpf':
                            status = self.help()
                            self.send_command_output(silent, type, status)  
                            return_status = 'ok'
            else:
                self.send_command_output(silent, "NONE", "No matching command found.")
        return {    
            'status': return_status,
            'execution_count': self.execution_count,
            'payload': [],
            'user_expressions': {},
        }