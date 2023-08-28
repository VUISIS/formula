from langchain.chat_models import ChatOpenAI
import os
from langchain.agents.agent import AgentExecutor
from langchain.agents.openai_functions_agent.base import OpenAIFunctionsAgent
from langchain.schema.messages import SystemMessage
from langchain.memory import ConversationBufferMemory
from .formula_tools import DebugFormulaCodeLLM, LoadFormulaCode, QueryFormulaCode, DecodeFormulaCodeLLM
from .config import cfg
from .prompts import FIX_CODE_PREFIX, QUERY_PROMPT

os.environ["OPENAI_API_KEY"] = cfg["OPENAI_API_KEY"]


if cfg["LANGCHAIN_API_KEY"] != "":
	os.environ["LANGCHAIN_TRACING_V2"] ="true"
	os.environ["LANGCHAIN_ENDPOINT"] ="https://api.smith.langchain.com"
	os.environ["LANGCHAIN_API_KEY"] = cfg["LANGCHAIN_API_KEY"]
	os.environ["LANGCHAIN_PROJECT"] ="pt-oily-sultan-99"


llm = ChatOpenAI(temperature=0, model="gpt-3.5-turbo-16k")


system_message = SystemMessage(content=FIX_CODE_PREFIX)
_prompt = OpenAIFunctionsAgent.create_prompt(system_message=system_message)
memory = ConversationBufferMemory(memory_key="chat_history", return_messages=True)

tools = [LoadFormulaCode, QueryFormulaCode, DecodeFormulaCodeLLM(llm=llm), DebugFormulaCodeLLM(llm=llm)]

agent = OpenAIFunctionsAgent(
    llm=llm,
    prompt=_prompt,
    tools=tools,
    memory=memory,
    verbose=True
    )

agent_executor = AgentExecutor.from_agent_and_tools(
        agent=agent,
        tools=tools,
        verbose=True,
    )

def run_agent_executor(code, output, additional_details):
    query = QUERY_PROMPT.format(code=code, interpreter_output=output)
    if additional_details:
        query += f"\n\nHere are some additional details to keep in mind when trying to figure \
out what is wrong with the code:\n\n{additional_details}"
    agent_executor.run(query)