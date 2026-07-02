import json
import os
import sys
from openai import OpenAI

SYSTEM_PROMPT = "You are a helpful AI assistant."

MODEL = "llama-3.3-70b-versatile"


def main():

    try:
        payload = json.load(sys.stdin)
    except Exception as e:
        print(json.dumps({"reply": str(e)}))
        return

    api_key = os.getenv("GROQ_API_KEY")

    if not api_key:
        print(json.dumps({
            "reply": "GROQ_API_KEY not found."
        }))
        return

    client = OpenAI(
        api_key=api_key,
        base_url="https://api.groq.com/openai/v1"
    )

    try:

        if payload.get("messages"):

            messages = [
                {
                    "role": "system",
                    "content": SYSTEM_PROMPT
                }
            ]

            messages.extend(payload["messages"])

        else:

            prompt = payload.get("prompt", "").strip()

            if prompt == "":
                print(json.dumps({
                    "reply": "Please enter a message."
                }))
                return

            messages = [
                {
                    "role": "system",
                    "content": SYSTEM_PROMPT
                },
                {
                    "role": "user",
                    "content": prompt
                }
            ]

        response = client.chat.completions.create(
            model=MODEL,
            messages=messages,
            temperature=0.7,
            max_tokens=1024
        )

        print(json.dumps({
            "reply": response.choices[0].message.content
        }))

    except Exception as e:

        print(json.dumps({
            "reply": f"Groq Error: {e}"
        }))


if __name__ == "__main__":
    main()