import json
import os
import sys
from openai import OpenAI

SYSTEM_PROMPT = "You are a helpful AI assistant."

MODEL = "llama-3.1-8b-instant"


def clean_messages_for_text_model(messages):
    """Remove image content from messages since the model is text-only."""
    cleaned = []
    for msg in messages:
        content = msg.get("content", "")
        role = msg.get("role", "user")

        if isinstance(content, list):
            text_parts = []
            for item in content:
                if isinstance(item, dict):
                    if item.get("type") == "text":
                        text_parts.append(item.get("text", ""))
                    elif item.get("type") == "image_url":
                        text_parts.append("[Image content omitted - model does not support image input]")
            content = " ".join(text_parts) if text_parts else ""

        cleaned.append({"role": role, "content": str(content)})
    return cleaned


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

            messages.extend(clean_messages_for_text_model(payload["messages"]))

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
        error_message = str(e)
        
        # Check if it's a rate limit error
        if "429" in error_message or "rate_limit" in error_message.lower():
            print(json.dumps({
                "reply": "Rate limit reached #43212"
            }))
        else:
            print(json.dumps({
                "reply": f"Groq Error: {e}"
            }))


if __name__ == "__main__":
    main()