import os
import tempfile
from datetime import datetime

from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential

FOUNDRY_PROJECT_ENDPOINT = "https://ps-agent-project-resource.services.ai.azure.com/api/projects/ps-agent-project"
MODEL_DEPLOYMENT_NAME = "gpt-4.1"


# Runs on the SERVER: Python executes in a Foundry-hosted sandbox
def code_interpreter_tool(client):
    print("\n=== code_interpreter ===")
    response = client.responses.create(
        model=MODEL_DEPLOYMENT_NAME,
        instructions="You are an AI assistant that provides information. Use the python tool to run code for math problems.",
        input="What is the square root of 16?",
        tools=[{"type": "code_interpreter", "container": {"type": "auto"}}],
    )
    print(response.output_text)


# Runs on the SERVER: Foundry performs the web search
def web_search_tool(client):
    print("\n=== web_search ===")
    response = client.responses.create(
        model=MODEL_DEPLOYMENT_NAME,
        instructions="You are an AI assistant. Use web search when current information is required.",
        input="What are three major announcements from Microsoft Build this year?",
        tools=[{"type": "web_search"}],
    )
    print(response.output_text)


# Runs on the SERVER: file is indexed and searched in a Foundry-hosted vector store
def file_search_tool(client):
    print("\n=== file_search ===")

    # Create a small sample policy document, upload it and index it
    path = os.path.join(tempfile.gettempdir(), "expenses_policy.txt")
    with open(path, "w") as f:
        f.write("Expenses policy: Taxi rides may be claimed up to a maximum of 50 euros per ride. Meals are limited to 30 euros per day.")

    vector_store = client.vector_stores.create(name="policy-docs")
    with open(path, "rb") as f:
        uploaded = client.vector_stores.files.upload_and_poll(vector_store_id=vector_store.id, file=f)

    response = client.responses.create(
        model=MODEL_DEPLOYMENT_NAME,
        input="What's the maximum amount I can claim for a taxi ride?",
        tools=[{"type": "file_search", "vector_store_ids": [vector_store.id]}],
        include=["file_search_call.results"],
    )
    print(response.output_text)

    # Clean up
    client.vector_stores.delete(vector_store.id)
    client.files.delete(uploaded.id)


# Runs on the CLIENT: the model only requests the call, our Python code executes it
def function_tool(client):
    print("\n=== function ===")
    get_time_tool = {
        "type": "function",
        "name": "get_time",
        "description": "Get the current time",
        "parameters": {"type": "object", "properties": {}},
    }
    instructions = "Answer only with the tool output when a tool is used."

    last_response_id = None
    for prompt in ["Hello", "What time is it?"]:
        print(f"> {prompt}")
        response = client.responses.create(
            model=MODEL_DEPLOYMENT_NAME,
            instructions=instructions,
            input=prompt,
            tools=[get_time_tool],
            previous_response_id=last_response_id,
        )

        # Server returned a function_call: run it HERE, then send the result back
        while any(item.type == "function_call" for item in response.output):
            outputs = []
            for item in response.output:
                if item.type == "function_call":
                    print(f"(model called {item.name})")
                    outputs.append({"type": "function_call_output", "call_id": item.call_id, "output": get_time()})
            response = client.responses.create(
                model=MODEL_DEPLOYMENT_NAME,
                instructions=instructions,
                input=outputs,
                tools=[get_time_tool],
                previous_response_id=response.id,
            )

        print(response.output_text)
        last_response_id = response.id


# Executes locally on this machine, never on the server
def get_time():
    return datetime.now().strftime("%H:%M:%S")


def main():
    project = AIProjectClient(endpoint=FOUNDRY_PROJECT_ENDPOINT, credential=DefaultAzureCredential())
    client = project.get_openai_client()

    code_interpreter_tool(client)
    web_search_tool(client)
    file_search_tool(client)
    function_tool(client)


if __name__ == "__main__":
    main()
