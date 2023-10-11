from typing import Any, Optional

from langchain.callbacks.manager import (
    CallbackManagerForToolRun,
)
from langchain.chat_models.base import BaseChatModel
from langchain.chains import SequentialChain
from langchain.tools.base import BaseTool
from .prompts import (
    EXPLAIN_QUERY_PROMPT,
    REPAIR_OUTPUT_RETURN,
    OUTPUT_RETURN,
    REPAIR_QUERY_PROMPT,
    FORMULA_CODE_LLM_DESC,
    REPAIR_FORMULA_CODE_LLM_DESC
)
from langchain import LLMChain, PromptTemplate
from langchain.tools import BaseTool, Tool

class FormulaCodeLLM(BaseTool):
    name = "FormulaCodeLLM"
    description = FORMULA_CODE_LLM_DESC
    llm: BaseChatModel

    def _run(self, arg, **kwargs) -> Any:
        explain_prompt = kwargs["prompt"]
        code = kwargs["code"]
        output = kwargs["output"]

        explain_template = EXPLAIN_QUERY_PROMPT
        
        prompt_template = PromptTemplate(
            input_variables=["prompt", "code", "output"],
            template=explain_template,
        )
        
        code_understander_chain = LLMChain(
            llm=self.llm, prompt=prompt_template, output_key="explanation"
        )

        out = code_understander_chain(kwargs)

        return_output = OUTPUT_RETURN.format(explanation=out['explanation'])

        return return_output

    async def _arun(
        self,
    ):
        raise NotImplementedError("custom_search does not support async")

class RepairFormulaCodeLLM(BaseTool):
    name = "RepairFormulaCodeLLM"
    description = REPAIR_FORMULA_CODE_LLM_DESC
    llm: BaseChatModel

    def _run(self, arg, **kwargs) -> Any:
        repair_prompt = kwargs["prompt"]
        code = kwargs["code"]
        output = kwargs["output"]
        
        repair_template = REPAIR_QUERY_PROMPT

        prompt_template = PromptTemplate(
            input_variables=["prompt", "code", "output"],
            template=repair_template,
        )
        
        repair_chain = LLMChain(
            llm=self.llm, prompt=prompt_template, output_key="repair"
        )
        
        out = repair_chain(kwargs)

        return_output = REPAIR_OUTPUT_RETURN.format(repair=out['repair'])

        return return_output

    async def _arun(
        self,
    ):
        raise NotImplementedError("custom_search does not support async")
