from __future__ import print_function
import os
from IPython.core.magic import (Magics, magics_class, line_magic)

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console  

@magics_class
class FormulaMagics(Magics):

    @line_magic
    def init(self, line):
        self.sw = StringWriter()
        Console.SetOut(self.sw)
        Console.SetError(self.sw)

        sink = CommandLineProgram.ConsoleSink()
        chooser = CommandLineProgram.ConsoleChooser()
        self.ci = CommandInterface(sink, chooser)
        self.ci.DoCommand("wait on")
        self.sw.GetStringBuilder().Clear()
        print("Successfully initialized formula.")

    @line_magic
    def help(self, line):
        print("Formula magic commands:")
        print("\t%init          - Initializes the formula command interface.")
        print("\t%load filepath - Loads and compiles a file that is not yet loaded. Use: load filepath.")
        print("\t%help          - Prints this message.")
    
    @line_magic
    def load(self, line):
        self.ci.DoCommand("load " + line)
        temp_str = self.sw.ToString()
        self.sw.GetStringBuilder().Clear()
        print(temp_str)