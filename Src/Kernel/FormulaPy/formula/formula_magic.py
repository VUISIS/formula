from __future__ import print_function
from IPython.core.magic import (Magics, magics_class, line_magic)
from IPython.core.magic_arguments import (argument, magic_arguments, parse_argstring)

import re, os

from formulallm.formula_agent import run_agent_executor, run_agent_executor_repair
from formulallm.formula_program import FormulaInterface

@magics_class
class FormulaMagics(Magics):

    def __init__(self, shell, data=None):
        super(FormulaMagics, self).__init__(shell)
        self.fi = FormulaInterface()
        print("Successfully initialized formula.")

    @line_magic
    def help(self, line):
        print("Formula magic commands:")
        print("%load (l)       - Loads and compiles a file that is not yet loaded. Use: load filepath.")
        print("%help (h)       - Prints this message.")
        print("%list (ls)      - Lists environment objects. Use: ls [vars | progs | tasks]")
        print("%extract (ex)   - Extract and install a result. Use: extract (app_id | solv_id n) output_name [render_class render_dll]")
        print("%query (qr)     - Start a query task. Use: query model goals")
        print("%solve (sl)     - Start a solve task. Use: solve partial_model max_sols goals")
        print("%explain (exp)  - Runs an LLM to generate an explaination for the solution. Use: exp solv_id prompt")
        print("%repair (rep)   - Runs an LLM to repair the loaded formula DSL program. Use: rep solv_id")
        print("%set (s)        - Sets a variable. Use: set var term.")
        print("%delete (d)     - Deletes a variable. Use: del var.")
        print("%save (sv)      - Saves the module modname into file.")
        print("%unload (ul)    - Unloads an installed program and all dependent programs. Use: unload [prog | *]")
        print("%tunload (tul)  - Unloads a task. Use: tunload [id | *]")
        print("%reload (rl)    - Reloads an installed program and all dependent programs. Use: reload [prog | *]")
        print("%print (p)      - Prints the installed program with the given name. Use: print progname")
        print("%det (dt)       - Prints details about the compiled module with the given name. Use: det modname")
        print("%types (typ)    - Prints inferred variable types. Use: types modname")
        print("%render (r)     - Tries to render the module. Use: render modname")
        print("%verbose (v)    - Changes verbosity. Use: verbose (on | off)")
        print("%wait (w)       - Changes waiting behavior. Use: wait (on | off) to block until task completes")
        print("%apply (ap)     - Start an apply task. Use: apply transformstep")
        print("%stats (st)     - Prints task statistics. Use: stats task_id [top_k_rule]")
        print("%generate (gn)  - Generate C# data model. Use: generate modname")
        print("%truth (tr)     - Test if a ground term is derivable under a model/apply. Use: truth task_id [term]")
        print("%proof (pr)     - Enumerate proofs that a ground term is derivable under a model/apply. Use: proof task_id [term]")
        print("%confhelp (ch)  - Provides help about module configurations and settings")
        print("%watch (wch)    - Use: watch [off | on | prompt] to control watch behavior")
        print("%core (cr)      - Prints reduced rule set for domains / transforms. Use: core module_name")
        print("%downgrade (dg) - Attempts to downgrade a (partial) model to Formula V1. Use: downgrade module_name")

    def run_command(self, cmd, args=None):
        line = ""
        if args == None:
            line = cmd
        else:
            line = cmd + " " + args
                    
        temp_str = self.fi.run_command(line)
        if cmd == "extract":
            self.total_sw_output = temp_str
                
        print(temp_str)

    @line_magic
    def load(self, line):
        file = os.path.abspath(line)
        if os.path.isfile(file):
            f = open(file, 'r')
            try:
                self.file_txt = f.read()
            finally:
                f.close()
            self.run_command("load", file)
            return
        
        print("File is invalid or does not exist.")
        
    @line_magic
    def l(self, line):
        self.load(line)

    @line_magic
    def list(self, line):
        self.run_command("list", line)
        
    @line_magic
    def ls(self, line):
        self.list(line)

    @line_magic
    def extract(self, line):
        self.run_command("extract", line)
        
    @line_magic
    def ex(self, line):
        self.extract(line)

    @line_magic
    def query(self, line):
        self.run_command("query", line)
        
    @line_magic
    def qr(self, line):
        self.query(line)

    @line_magic
    def solve(self, line):
        if self.file_txt == None:
            print("Load 4ml file to proceed.")
            return
        self.run_command("solve", line)
   
    @line_magic
    def sl(self, line):
        self.solve(line)
        
    @magic_arguments()
    @argument(
        "--explain-prompt",
        "-ep",
        required=True,
        help=("Input explain prompt."),
    )
    @line_magic
    def explain(self, line):
        if self.file_txt == None:
            print("Load 4ml file to proceed.")
            return
        
        args = parse_argstring(self.explain, line)
        
        print('Running executor...')
        run_agent_executor(self.file_txt, self.total_sw_output, args.explain_prompt)

    @line_magic
    def exp(self, line):
        self.explain(line)
        
    @magic_arguments()
    @argument(
        "--repair-prompt",
        "-rp",
        required=True,
        help=("Input repair prompt."),
    )
    @argument(
        "--save",
        "-s",
        required=False,
        help=("Save repair code."),
    )
    @line_magic
    def repair(self, line):
        print('Running repair...')
            
        args = parse_argstring(self.repair, line)
        
        out = run_agent_executor_repair(self.file_txt, self.total_sw_output, args.repair_prompt)
        
        if args.save:
            m = re.search('\`\`\`(code|formula|python)?.+\{(.+)\}', out, re.DOTALL)        
            if m:
                capt = m.group(2)
                m2 = re.findall('(.+)\s\/\/.+', capt)
                if len(m2) > 0:
                    new_txt = self.file_txt
                    for l in m2:
                        temp = re.sub(r'\s\d+', r'\\s\\d+', l)
                        temp = temp.removesuffix('.') 
                        temp = temp.replace("(", "\(")
                        temp = temp.replace(")", "\)")
                        l = l.removesuffix('.') 
                        new_txt = re.sub(temp, l, new_txt)

                    f = open(args.save, 'w')
                    try:
                        f.write(new_txt)
                    finally:
                        f.close()
                else:
                    print('No match found in partial model. Unable to save.')
            else:
                    print('No match found in GPT generated code. Unable to save.')

    @line_magic
    def rep(self, line):
        self.repair(line)

    @line_magic
    def set(self, line):
        self.run_command("set", line)
        
    @line_magic
    def s(self, line):
        self.set(line)

    @line_magic
    def delete(self, line):
        self.run_command("del", line)
        
    @line_magic
    def d(self, line):
        self.delete(line)

    @line_magic
    def unload(self, line):
        self.file_txt = None
        self.total_sw_output = "END"    
        self.run_command("unload", line)
        
    @line_magic
    def ul(self, line):
        self.unload(line)

    @line_magic
    def tunload(self, line):
        self.run_command("tunload", line)

    @line_magic
    def tul(self, line):
        self.tunload(line)

    @line_magic
    def reload(self, line):
        self.run_command("reload")
        
    @line_magic
    def rl(self, line):
        self.reload()

    @line_magic
    def save(self, line):
        self.run_command("save", line)
        
    @line_magic
    def sv(self, line):
        self.save(line)

    @line_magic
    def print(self, line):
        self.run_command("print", line)
        
    @line_magic
    def p(self, line):
        self.print(line)

    @line_magic
    def det(self, line):
        self.run_command("det", line)
        
    @line_magic
    def dt(self, line):
        self.det(line)

    @line_magic
    def render(self, line):
        self.run_command("render", line)
        
    @line_magic
    def r(self, line):
        self.render(line)

    @line_magic
    def verbose(self, line):
        self.run_command("verbose", line)
        
    @line_magic
    def v(self, line):
        self.verbose(line)

    @line_magic
    def wait(self, line):
        self.run_command("wait", line)

    @line_magic
    def w(self, line):
        self.wait(line)

    @line_magic
    def watch(self, line):
        self.run_command("watch", line)

    @line_magic
    def wch(self, line):
        self.watch(line)

    @line_magic
    def types(self, line):
        self.run_command("type", line)

    @line_magic
    def typ(self, line):
        self.types(line)

    @line_magic
    def truth(self, line):
        self.run_command("truth", line)
        
    @line_magic
    def tr(self, line):
        self.truth(line)

    @line_magic
    def proof(self, line):
        self.run_command("proof", line)
        
    @line_magic
    def pr(self, line):
        self.proof(line)

    @line_magic
    def apply(self, line):
        self.run_command("apply", line)
        
    @line_magic
    def ap(self, line):
        self.apply(line)
    
    @line_magic
    def stats(self, line):
        self.run_command("stats", line)
        
    @line_magic
    def st(self, line):
        self.stats(line)

    @line_magic
    def generate(self, line):
        self.run_command("generate", line)
        
    @line_magic
    def gn(self, line):
        self.generate(line)

    @line_magic
    def confhelp(self, line):
        self.run_command("confhelp", line)
        
    @line_magic
    def ch(self, line):
        self.confhelp(line)

    @line_magic
    def core(self, line):
        self.run_command("core", line)
        
    @line_magic
    def cr(self, line):
        self.core(line)

    @line_magic
    def downgrade(self, line):
        self.run_command("downgrade", line)
        
    @line_magic
    def dg(self, line):
        self.downgrade(line)
