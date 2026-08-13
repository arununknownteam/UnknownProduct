#!/usr/bin/env python3
"""
Test script to verify the AI Chat API works through the backend.
This script simulates what the backend does - it passes the API key via environment variable.
"""

import json
import subprocess
import sys
import os

# Get the GROQ_API_KEY from environment (set by backend)
api_key = os.getenv("GROQ_API_KEY")

if not api_key:
    print("❌ ERROR: GROQ_API_KEY environment variable not found!")
    print("")
    print("This is expected when running manually. The API key is set by the .NET backend.")
    print("")
    print("To test properly:")
    print("1. Start the backend: cd backend/BackendApi && dotnet run")
    print("2. Test via API: curl -X POST http://localhost:5243/api/chat \\")
    print("   -H 'Authorization: Bearer YOUR_TOKEN' \\")
    print("   -H 'Content-Type: application/json' \\")
    print("   -d '{\"message\":\"Hello\"}'")
    print("")
    sys.exit(1)

# Path to the AI chat script
script_path = os.path.join(os.path.dirname(__file__), "backend", "BackendApi", "Python", "ai_chat.py")

# Test data
test_data = {
    "prompt": "Hello, how are you?"
}

# Run the script with the API key in environment
env = os.environ.copy()
env["GROQ_API_KEY"] = api_key

try:
    result = subprocess.run(
        [sys.executable, script_path],
        input=json.dumps(test_data),
        capture_output=True,
        text=True,
        env=env,
        timeout=30
    )
    
    print("=" * 50)
    print("Testing AI Chat with GROQ_API_KEY")
    print("=" * 50)
    print("")
    print(f"Input: {test_data}")
    print("")
    
    if result.returncode != 0:
        print(f"❌ Script failed with return code: {result.returncode}")
        print(f"Error: {result.stderr}")
        sys.exit(1)
    
    print(f"Output: {result.stdout}")
    
    # Parse response
    try:
        response = json.loads(result.stdout)
        if "reply" in response:
            print("")
            print("✅ SUCCESS! AI Chat is working!")
            print(f"Reply: {response['reply']}")
        else:
            print("")
            print("⚠️  Response doesn't contain 'reply' field")
            print(f"Response: {response}")
    except json.JSONDecodeError as e:
        print("")
        print(f"❌ Failed to parse JSON response: {e}")
        print(f"Output: {result.stdout}")
        sys.exit(1)

except subprocess.TimeoutExpired:
    print("")
    print("❌ Request timed out (30 seconds)")
    sys.exit(1)
except Exception as e:
    print("")
    print(f"❌ Error: {e}")
    sys.exit(1)