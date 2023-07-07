import os
from pythonnet import load
load("coreclr", runtime_config=os.path.abspath('./runtimeconfig.json'))
import clr
clr.AddReference(os.path.abspath('./CommandLine/CommandLine.dll'))

from ipykernel.kernelbase import Kernel
import re

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console  

class FormulaKernel(Kernel):
    implementation = 'Formula'
    implementation_version = '1.0'
    language = 'python' 
    language_version = '3.10'
    language_info = {'name': 'python',
                     'mimetype': 'text/plain',
                     'extension': '.py'}
    banner = "Formula 2.0"

    sw = StringWriter()
    Console.SetOut(sw)
    Console.SetError(sw)

    sink = CommandLineProgram.ConsoleSink()
    chooser = CommandLineProgram.ConsoleChooser()
    ci = CommandInterface(sink, chooser)
    ci.DoCommand("wait on")
    sw.FlushAsync()
    
    def load(self, path):
        return self.ci.DoCommand("load " + path)
    
    def help(self):
        output = "Formula python commands:\n"
        output += "load(path) - Loads and compiles a file that is not yet loaded. Use: load filepath.\n"
        output += "help() - Prints this message."
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
        return_status = {}
        functions = code.split('\n')
        for function in functions:
            match = re.search(r'(load|help)\((\w(\,\w)*)\)', function)
            if match:
                type = match.group(1)
                params = match.group(2).split(',')

                match type:
                    case 'load':
                        if not os.path.isabs(params[0]):
                            params[0] = os.path.abspath(params[0])
                        
                        if os.path.isfile(params[0]):
                            status = load(params[0].strip())
                            if status:
                                self.send_command_output(silent, type, self.sw.ToString())
                                self.sw.Clear()  
                                return_status = 'ok'
                            else:
                                return_status = 'error'
                        else:
                            return_status = 'error'
                    case 'help':
                        status = help()
                        self.send_command_output(silent, type, status)  
                        return_status = 'ok'
        return {    
            'status': return_status,
            'execution_count': self.execution_count,
            'payload': [],
            'user_expressions': {},
        }

if __name__ == '__main__':
    from ipykernel.kernelapp import IPKernelApp
    IPKernelApp.launch_instance(
        kernel_class=FormulaKernel)