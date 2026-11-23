from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential

FOUNDRY_PROJECT_ENDPOINT = "https://ps-agent-project-resource.services.ai.azure.com/api/projects/ps-agent-project"
MODEL_DEPLOYMENT_NAME = "gpt-4.1"
SAMPLE_QUESTIONS = ["What is the fastest animal on Earth?", "Where can I see one?", "Are they endangered?", "Be creative"]


def main():
    project = AIProjectClient(endpoint=FOUNDRY_PROJECT_ENDPOINT, credential=DefaultAzureCredential())
    client = project.get_openai_client()

    response_api(client)  # Preferred approach for handling responses
    completion_api(client)  # Only legacy


def response_api(client):
    # Server-side state: each call is chained to the previous one via previous_response_id
    last_response_id = None
    index = 0
    while True:
        prompt, index = get_user_input(index)

        response = client.responses.create(
            model=MODEL_DEPLOYMENT_NAME,
            instructions="You are a helpful AI assistant that answers questions.",
            input=prompt,
            previous_response_id=last_response_id,
        )

        print(response.output_text)
        last_response_id = response.id

        if input("Press Enter to continue, q to quit ").lower() == "q":
            break


def completion_api(client):
    # Client-side state: we keep the full conversation and send it every time
    messages = [{"role": "system", "content": "You are a helpful AI assistant that answers questions."}]

    index = 0
    while True:
        prompt, index = get_user_input(index)
        messages.append({"role": "user", "content": prompt})

        response = client.chat.completions.create(model=MODEL_DEPLOYMENT_NAME, messages=messages)
        reply = response.choices[0].message.content
        print(reply)

        # Keep the assistant reply so the model sees the full conversation next turn
        messages.append({"role": "assistant", "content": reply})

        if input("Press Enter to continue, q to quit ").lower() == "q":
            break


def get_user_input(index):
    if index >= len(SAMPLE_QUESTIONS):
        index = 3
    prompt = input(f"Ask anything (default: {SAMPLE_QUESTIONS[index]}) ")

    if not prompt:
        prompt = SAMPLE_QUESTIONS[index]
        index += 1
    return prompt, index


if __name__ == "__main__":
    main()
