from __future__ import print_function
from xturing.datasets.instruction_dataset import InstructionDataset
from xturing.models import BaseModel
from IPython.core.magic import (Magics, magics_class, line_magic)

from Microsoft.Formula.CommandLine import CommandInterface, CommandLineProgram
from System.IO import StringWriter
from System import Console  

import os

@magics_class
class FormulaMagics(Magics):

    @line_magic
    def init(self, line):
        self.sw = StringWriter()
        Console.SetOut(self.sw)
        Console.SetError(self.sw)

        self.model = BaseModel.load_from_local(os.path.abspath("./FormulaLLM"))

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
        temp_str = self.sw.ToString()
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def list(self, line):
        self.ci.DoCommand("ls")
        temp_str = self.sw.ToString()
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def extract(self, line):
        self.ci.DoCommand("extract " + line)
        temp_str = self.sw.ToString()
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def query(self, line):
        self.ci.DoCommand("query " + line)
        temp_str = self.sw.ToString()
        self.sw.GetStringBuilder().Clear()
        print(temp_str)

    @line_magic
    def solve(self, line):
        self.ci.DoCommand("solve " + line)
        temp_str = self.sw.ToString()
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

        prompt = "Below is an instruction that describes a task, paired with an input that provides further context. Write a response that appropriately completes the request.\n\n### Instruction:\n{instruction}\n\n### Input:\n{text}\n\n### Response:"

        data = {
            "instruction": [line],
            "text": [txt],
            "target": [""]
        }

        instruction_dataset = InstructionDataset(data, promt_template=prompt)

        output = self.model.generate(dataset=instruction_dataset)

        for i,o in enumerate(output):
            print("Generated output by the model:")
            print()
            print(output[i])