import json
import os
import sys


def main():
    try:
        payload = json.load(sys.stdin)
        prompt = payload.get("prompt", "").strip()
    except Exception as exc:
        print(json.dumps({"reply": f"Invalid request: {exc}"}))
        return

    if not prompt:
        print(json.dumps({"reply": "Type a message to start the AI chat."}))
        return

    api_key = os.getenv("OPENAI_API_KEY") or os.getenv("GROQ_API_KEY")

    if not api_key:
        print(json.dumps({
            "reply": (
                "AI chat is ready, but no API key was found. "
                "Set OPENAI_API_KEY or GROQ_API_KEY in your environment to use the real model."
            )
        }))
        return

    try:
        import openai

        client = openai.OpenAI(api_key=api_key)

        # If messages provided, use them; otherwise use single prompt
        messages_payload = None
        if isinstance(payload, dict) and "messages" in payload:
            messages_payload = payload.get("messages")

        if messages_payload:
            # assume messages_payload is a list of {role, content}
            messages = []
            # ensure a system message exists at the start
            messages.append({"role": "system", "content": "You are a helpful assistant."})
            for m in messages_payload:
                role = m.get("role") if isinstance(m, dict) else "user"
                content = m.get("content") if isinstance(m, dict) else str(m)
                messages.append({"role": role, "content": content})

            response = client.chat.completions.create(
                model="gpt-3.5-turbo",
                messages=messages,
                max_tokens=300,
                temperature=0.7,
            )
        else:
            response = client.chat.completions.create(
                model="gpt-3.5-turbo",
                messages=[
                    {"role": "system", "content": "You are a helpful assistant."},
                    {"role": "user", "content": prompt},
                ],
                max_tokens=300,
                temperature=0.7,
            )

        assistant_message = response.choices[0].message.content.strip()
        print(json.dumps({"reply": assistant_message}))
    except ModuleNotFoundError:
        print(json.dumps({
            "reply": "The OpenAI Python SDK is not installed. Run `pip install openai` in backend/BackendApi/Python/.venv/bin/python -m pip install openai."
        }))
    except Exception as exc:
        print(json.dumps({"reply": f"AI request failed: {exc}"}))


if __name__ == "__main__":
    main()
