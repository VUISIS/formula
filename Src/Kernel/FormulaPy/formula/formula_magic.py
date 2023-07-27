from __future__ import print_function
from IPython.core.magic import (Magics, magics_class, line_magic)

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console

import sys
print(sys.path)
# TODO: make sure to fix pathing issues
sys.path.append("C:\\Users\\agarg\\Documents\\formula\\formula_new\\formula\\Src\\Kernel\\FormulaPy")
sys.path.append("C:\\Users\\agarg\\Documents\\formula\\formula_new\\formula\\Src\\Kernel\\FormulaPy\\formula\\SelfRepairLLM\\src")
# from SelfRepairLLM.src.main import run_agent_executor
from main import run_agent_executor

import os

@magics_class
class FormulaMagics(Magics):

    @line_magic
    def init(self, line):
        self.sw = StringWriter()
        self.total_sw_output = ""
        Console.SetOut(self.sw)
        Console.SetError(self.sw)

        # self.model = BaseModel.load_from_local(os.path.abspath("./FormulaLLM"))

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
        print("\t%list          - Lists environment objects. Use: ls [vars | progs | tasks]")
        print("\t%extract       - Extract and install a result. Use: extract (app_id | solv_id n) output_name [render_class render_dll]")
        print("\t%query         - Start a query task. Use: query model goals")
        print("\t%solve         - Start a solve task. Use: solve partial_model max_sols goals")
        print("\t%explain       - Runs an LLM to generate an explaination for the solution.")

    @line_magic
    def load(self, line):
        self.file = line
        self.ci.DoCommand("load " + line)
        self.total_sw_output += "load " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)
        print('hello world')


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
        self.ci.DoCommand("query " + line)
        self.total_sw_output += "query " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def solve(self, line):
        self.ci.DoCommand("solve " + line)
        self.total_sw_output += "solve " + line + "\n"
        temp_str = self.sw.ToString()
        self.total_sw_output += temp_str
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def explain(self, line):
        f = open(self.file, 'r')
        txt = ""
        try:
            txt = f.read()
        finally:
            f.close()

        print('running executor')
        run_agent_executor(txt, self.total_sw_output, line)

        # prompt = "Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.\n\n### Instruction:\n{instruction}\n\n### Input:\n{text}\n\n### Response:"

        # data = {
        #     "instruction": [line],
        #     "text": [txt],
        #     "target": [""]
        # }

        # instruction_dataset = InstructionDataset(data, promt_template=prompt)

        # output = self.model.generate(dataset=instruction_dataset)


        # for i,o in enumerate(output):
        #     print("Generated output by the model:")
        #     print()
        #     print(output[i])