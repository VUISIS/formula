from __future__ import print_function
from IPython.core.magic import (Magics, magics_class, line_magic)

from pathlib import Path

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console

from .formula_agent import run_agent_executor

@magics_class
class FormulaMagics(Magics):

    def __init__(self, shell, data=None):
        super(FormulaMagics, self).__init__(shell)
        self.sw = StringWriter()
        self.total_sw_output = ""
        Console.SetOut(self.sw)
        Console.SetError(self.sw)
        self.file = None
        self.explain = False

        sink = CommandLineProgram.ConsoleSink()
        chooser = CommandLineProgram.ConsoleChooser()
        self.ci = CommandInterface(sink, chooser)
        self.ci.DoCommand("wait on")
        self.sw.GetStringBuilder().Clear()
        print("Successfully initialized formula.")

    @line_magic
    def help(self):
        print("Formula magic commands:")
        print("%load filepath - Loads and compiles a file that is not yet loaded. Use: load filepath.")
        print("%help          - Prints this message.")
        print("%ls            - Lists environment objects. Use: ls [vars | progs | tasks]")
        print("%extract       - Extract and install a result. Use: extract (app_id | solv_id n) output_name [render_class render_dll]")
        print("%query         - Start a query task. Use: query model goals")
        print("%solve         - Start a solve task. Use: solve partial_model max_sols goals")
        print("%explain       - Runs an LLM to generate an explaination for the solution.")
        print("%repair        - Runs an LLM to repair the loaded formula DSL program.")
        print("%set           - Sets a variable. Use: set var term.")
        print("%delete        - Deletes a variable. Use: del var.")
        print("%save          - Saves the module modname into file.")
        print("%unload        - Unloads an installed program and all dependent programs. Use: unload [prog | *]")
        print("%tunload       - Unloads a task. Use: tunload [id | *]")
        print("%reload        - Reloads an installed program and all dependent programs. Use: reload [prog | *]")
        print("%print         - Prints the installed program with the given name. Use: print progname")
        print("%det           - Prints details about the compiled module with the given name. Use: det modname")
        print("%types         - Prints inferred variable types. Use: types modname")
        print("%render        - Tries to render the module. Use: render modname")
        print("%verbose       - Changes verbosity. Use: verbose (on | off)")
        print("%wait          - Changes waiting behavior. Use: wait (on | off) to block until task completes")
        print("%apply         - Start an apply task. Use: apply transformstep")
        print("%stats         - Prints task statistics. Use: stats task_id [top_k_rule]")
        print("%generate      - Generate C# data model. Use: generate modname")
        print("%truth         - Test if a ground term is derivable under a model/apply. Use: truth task_id [term]")
        print("%proof         - Enumerate proofs that a ground term is derivable under a model/apply. Use: proof task_id [term]")
        print("%confhelp      - Provides help about module configurations and settings")
        print("%watch         - Use: watch [off | on | prompt] to control watch behavior")
        print("%core          - Prints reduced rule set for domains / transforms. Use: core module_name")
        print("%downgrade     - Attempts to downgrade a (partial) model to Formula V1. Use: downgrade module_name")

    def run_command(self, cmd, args=None):
        line = ""
        if args == None:
            line = cmd
        else:
            line = cmd + " " + args
            
        self.ci.DoCommand(line)
        
        temp_str = self.sw.ToString()
        if cmd == "extract":
            self.total_sw_output += temp_str
        elif cmd == "load" and self.file != None: 
            f = open(self.file.absolute(), 'r')
            self.total_sw_output += f.read()
            f.close()
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def load(self, line=None):
        if line == None:
            print("No 4ml file passed to load.")
            return
        
        file = Path(line)
        if file.is_file():
            self.run_command("load", file.absolute())
            self.file = file
            return
        
        print("File is invalid or does not exist.")

    @line_magic
    def ls(self):
        self.run_command("ls")

    @line_magic
    def extract(self, line=None):
        self.run_command("extract", line)

    @line_magic
    def query(self, line=None):
        self.run_command("query", line)

    @line_magic
    def solve(self, line=None):
        if self.file == None:
            print("Load 4ml file to proceed.")
            return
        self.run_command("solve", line)

    @line_magic
    def explain(self, line=None):
        if self.file == None:
            print("Load 4ml file to proceed.")
            return
        f = open(self.file, 'r')
        txt = ""
        try:
            txt = f.read()
        finally:
            f.close()

        print('Running executor...')
        run_agent_executor(txt, self.total_sw_output, line)
        self.explain = True
        
    @line_magic
    def repair(self):
        if self.explain:
            print("Repair")
        else:
            print("Run explain first")

    @line_magic
    def explain_and_repair(self):
        print("Explain and repair.")

    @line_magic
    def set(self, line=None):
        self.run_command("set", line)

    @line_magic
    def delete(self, line=None):
        self.run_command("del", line)

    @line_magic
    def unload(self, line=None):
        self.file = None
        self.run_command("unload", line)

    @line_magic
    def tunload(self, line=None):
        self.run_command("tunload", line)

    @line_magic
    def reload(self):
        self.run_command("reload")

    @line_magic
    def save(self, line=None):
        self.run_command("save", line)

    @line_magic
    def print(self, line=None):
        self.run_command("print", line)

    @line_magic
    def det(self, line=None):
        self.run_command("det", line)

    @line_magic
    def render(self, line=None):
        self.run_command("render", line)

    @line_magic
    def verbose(self, line=None):
        self.run_command("verbose", line)

    @line_magic
    def wait(self, line=None):
        self.run_command("wait", line)

    @line_magic
    def watch(self, line=None):
        self.run_command("watch", line)

    @line_magic
    def types(self, line=None):
        self.run_command("type", line)

    @line_magic
    def truth(self, line=None):
        self.run_command("truth", line)

    @line_magic
    def proof(self, line=None):
        self.run_command("proof", line)

    @line_magic
    def apply(self, line=None):
        self.run_command("apply", line)
    
    @line_magic
    def stats(self, line=None):
        self.run_command("stats", line)

    @line_magic
    def generate(self, line=None):
        self.run_command("generate", line)

    @line_magic
    def confhelp(self, line=None):
        self.run_command("confhelp", line)

    @line_magic
    def core(self, line=None):
        self.run_command("core", line)

    @line_magic
    def downgrade(self, line=None):
        self.run_command("downgrade", line)
