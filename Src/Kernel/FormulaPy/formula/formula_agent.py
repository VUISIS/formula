from langchain.chat_models import ChatOpenAI
import os
from langchain.agents.agent import AgentExecutor
from langchain.agents.openai_functions_agent.base import OpenAIFunctionsAgent
from langchain.schema.messages import SystemMessage
from langchain.memory import ConversationBufferMemory
from .formula_tools import FormulaCodeLLM, RepairFormulaCodeLLM
from .config import cfg
from .prompts import FIX_CODE_PREFIX, REPAIR_CODE_PREFIX

os.environ["OPENAI_API_KEY"] = cfg["OPENAI_API_KEY"]
#os.environ["OPENAI_API_TYPE"] = "azure"
#os.environ["OPENAI_API_VERSION"] = "2023-08-24"
#os.environ["OPENAI_API_BASE"] = "https://apim.app.vanderbilt.edu/openai-students/deployments/gpt-35-turbo/chat/completions?api-version=2023-08-24"



if cfg["LANGCHAIN_API_KEY"] != "":
	os.environ["LANGCHAIN_TRACING_V2"] ="true"
	os.environ["LANGCHAIN_ENDPOINT"] ="https://api.smith.langchain.com"
	os.environ["LANGCHAIN_API_KEY"] = cfg["LANGCHAIN_API_KEY"]
	os.environ["LANGCHAIN_PROJECT"] ="pt-oily-sultan-99"


llm = ChatOpenAI(temperature=0, model_name="gpt-3.5-turbo-16k")


system_message = SystemMessage(content=FIX_CODE_PREFIX)
_prompt = OpenAIFunctionsAgent.create_prompt(system_message=system_message)

repair_system_message = SystemMessage(content=REPAIR_CODE_PREFIX)
_repair_prompt = OpenAIFunctionsAgent.create_prompt(system_message=system_message)

explain_tools = [FormulaCodeLLM(llm=llm)]

agent = OpenAIFunctionsAgent(
    llm=llm,
    prompt=_prompt,
    tools=explain_tools,
    verbose=True
)

agent_executor = AgentExecutor.from_agent_and_tools(
        agent=agent,
        tools=explain_tools,
        verbose=True,
)

repair_tools = [RepairFormulaCodeLLM(llm=llm)]

agent_repair = OpenAIFunctionsAgent(
    llm=llm,
    prompt=_repair_prompt,
    tools=repair_tools,
    verbose=True
)

agent_executor_repair = AgentExecutor.from_agent_and_tools(
        agent=agent_repair,
        tools=repair_tools,
        verbose=True,
)

def run_agent_executor(code, output, explain_prompt):
    prompts = "{explain_prompt}\n\n{code}\n\n{output}".format(code=code, output=output, explain_prompt=explain_prompt)
    prompts.encode('unicode_escape')
    agent_executor.run(prompts)
    
def run_agent_executor_repair(code, output, repair_prompt):
    prompts = "{repair_prompt}\n\n{code}\n\n{output}".format(code=code, output=output, repair_prompt=repair_prompt)
    prompts.encode('unicode_escape')
    agent_executor_repair.run(prompts)