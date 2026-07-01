#!/usr/bin/env bash
set -e

PYTHON_DIR="$(cd "$(dirname "$0")" && pwd)"
VENV_DIR="$PYTHON_DIR/.venv"

printf "Creating Python virtual environment at %s...\n" "$VENV_DIR"
python -m venv "$VENV_DIR"

printf "Installing Python dependencies...\n"
"$VENV_DIR/bin/python" -m pip install --upgrade pip
"$VENV_DIR/bin/python" -m pip install -r "$PYTHON_DIR/requirements.txt"

cat <<'EOF'

Python environment is ready.
Run the backend with the same shell session or export the API key before starting the app:

  export OPENAI_API_KEY="your_api_key_here"
  # or
  export GROQ_API_KEY="your_api_key_here"

Then start the backend from the project root:

  cd ../
  dotnet run

EOF
