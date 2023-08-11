from __future__ import print_function
from IPython.core.magic import (Magics, magics_class, line_magic)

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console

from .SelfRepairLLM.src.main import run_agent_executor

@magics_class
class FormulaMagics(Magics):

    def __init__(self, shell, data):
        super(FormulaMagics, self).__init__(shell)
        self.data = data
        self.states = {
            "uninit": 0,
            "init": 1,
            "load": 2,
            "solve": 3
        }
        self.state = 0

    @line_magic
    def init(self, line):
        if self.state > 0:
            print("Already initialized.")
            return
        self.sw = StringWriter()
        self.total_sw_output = ""
        Console.SetOut(self.sw)
        Console.SetError(self.sw)

        sink = CommandLineProgram.ConsoleSink()
        chooser = CommandLineProgram.ConsoleChooser()
        self.ci = CommandInterface(sink, chooser)
        self.ci.DoCommand("wait on")
        self.sw.GetStringBuilder().Clear()
        print("Successfully initialized formula.")
        self.state = 1

    @line_magic
    def help(self, line):
        print("Formula magic commands:")
        print("\t%init          - Initializes the formula command interface.")
        print("\t%load filepath - Loads and compiles a file that is not yet loaded. Use: load filepath.")
        print("\t%help          - Prints this message.")
        print("\t%list          - Lists environment objects. Use: ls [vars | progs | tasks]")
        print("\t%extract       - Extract and install a result. Use: extract (app_id | solv_id n) output_name [render_class render_dll]")
        print("\t%query         - Start a query task. Use: query model goals")
        print("\t%solve         - Start a solve task. Use: solve partial_model max_sols goals")
        print("\t%explain       - Runs an LLM to generate an explaination for the solution.")
        print("\t%set           - Sets a variable. Use: set var term.")
        print("\t%delete        - Deletes a variable. Use: del var.")
        print("\t%save          - Saves the module modname into file.")
        print("\t%unload        - Unloads an installed program and all dependent programs. Use: unload [prog | *]")
        print("\t%tunload       - Unloads a task. Use: tunload [id | *]")
        print("\t%reload        - Reloads an installed program and all dependent programs. Use: reload [prog | *]")
        print("\t%print         - Prints the installed program with the given name. Use: print progname")
        print("\t%det           - Prints details about the compiled module with the given name. Use: det modname")
        print("\t%types         - Prints inferred variable types. Use: types modname")
        print("\t%render        - Tries to render the module. Use: render modname")
        print("\t%verbose       - Changes verbosity. Use: verbose (on | off)")
        print("\t%wait          - Changes waiting behavior. Use: wait (on | off) to block until task completes")
        print("\t%apply         - Start an apply task. Use: apply transformstep")
        print("\t%stats         - Prints task statistics. Use: stats task_id [top_k_rule]")
        print("\t%generate      - Generate C# data model. Use: generate modname")
        print("\t%truth         - Test if a ground term is derivable under a model/apply. Use: truth task_id [term]")
        print("\t%proof         - Enumerate proofs that a ground term is derivable under a model/apply. Use: proof task_id [term]")
        print("\t%confhelp      - Provides help about module configurations and settings")
        print("\t%watch         - Use: watch [off | on | prompt] to control watch behavior")
        print("\t%core          - Prints reduced rule set for domains / transforms. Use: core module_name")
        print("\t%downgrade     - Attempts to downgrade a (partial) model to Formula V1. Use: downgrade module_name")

    @line_magic
    def load(self, line):
        if self.state < 1:
            print("Has not been initialized. Use %init")
            return
        self.file = line
        self.ci.DoCommand("load " + line)
        self.total_sw_output += "load " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)
        print('hello world')
        self.state = 2


    @line_magic
    def list(self, line):
        self.ci.DoCommand("ls")
        self.total_sw_output += "ls"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def extract(self, line):
        self.ci.DoCommand("extract " + line)
        self.total_sw_output += "extract " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def query(self, line):
        if self.state < 3:
            print("Please initialize and load a file before running query.")
            return
        self.ci.DoCommand("query " + line)
        self.total_sw_output += "query " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def solve(self, line):
        if self.state < 3:
            print("Please initialize and load a file before running solve.")
            return
        self.ci.DoCommand("solve " + line)
        self.total_sw_output += "solve " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)
        self.state = 3

    @line_magic
    def explain(self, line):
        if self.state < 3:
            print("Please run a solve command before running explain.")
            return
        f = open(self.file, 'r')
        txt = ""
        try:
            txt = f.read()
        finally:
            f.close()

        print('Running executor...')
        run_agent_executor(txt, self.total_sw_output, line)

    @line_magic
    def set(self, line):
        self.ci.DoCommand("set " + line)
        self.total_sw_output += "set " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def delete(self, line):
        self.ci.DoCommand("del " + line)
        self.total_sw_output += "del " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def unload(self, line):
        self.ci.DoCommand("unload " + line)
        self.total_sw_output += "unload " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)
        self.state = 1
        print("Remember to load a file before proceeding.")

    @line_magic
    def tunload(self, line):
        self.ci.DoCommand("tunload " + line)
        self.total_sw_output += "tunload " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def reload(self, line):
        self.ci.DoCommand("reload")
        self.total_sw_output += "reload\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def save(self, line):
        self.ci.DoCommand("save " + line)
        self.total_sw_output += "save " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def print(self, line):
        self.ci.DoCommand("print " + line)
        self.total_sw_output += "print " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def det(self, line):
        self.ci.DoCommand("det " + line)
        self.total_sw_output += "det " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def render(self, line):
        self.ci.DoCommand("render " + line)
        self.total_sw_output += "render " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def verbose(self, line):
        self.ci.DoCommand("verbose " + line)
        self.total_sw_output += "verbose " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def wait(self, line):
        self.ci.DoCommand("wait " + line)
        self.total_sw_output += "wait " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def watch(self, line):
        self.ci.DoCommand("watch " + line)
        self.total_sw_output += "watch " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def types(self, line):
        self.ci.DoCommand("types " + line)
        self.total_sw_output += "types " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def truth(self, line):
        self.ci.DoCommand("truth " + line)
        self.total_sw_output += "truth " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def proof(self, line):
        self.ci.DoCommand("proof " + line)
        self.total_sw_output += "proof " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def apply(self, line):
        self.ci.DoCommand("apply " + line)
        self.total_sw_output += "apply " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)
    
    @line_magic
    def stats(self, line):
        self.ci.DoCommand("stats " + line)
        self.total_sw_output += "stats " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def generate(self, line):
        self.ci.DoCommand("generate " + line)
        self.total_sw_output += "generate " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def confhelp(self, line):
        self.ci.DoCommand("confhelp " + line)
        self.total_sw_output += "confhelp " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def core(self, line):
        self.ci.DoCommand("core " + line)
        self.total_sw_output += "core " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def downgrade(self, line):
        self.ci.DoCommand("downgrade " + line)
        self.total_sw_output += "downgrade " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)
