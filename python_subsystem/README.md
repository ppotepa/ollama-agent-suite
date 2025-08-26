# Python Subsystem for Ollama-Agent-Suite

This subsystem provides language processing and LLM interaction capabilities for the main C# application. It is designed to be started and managed by the C# host, and communicates via HTTP (FastAPI).

## Features
- Receives instructions and parameters from C#
- Talks to Ollama API for LLM inference
- Supports chat initialization with context management
- Each query can be a separate chat or part of ongoing conversation
- Returns results in a simple JSON format
- Stateless, lightweight, and easy to extend

## Quickstart
1. Install dependencies:
   ```sh
   pip install -r requirements.txt
   ```
2. Run the server:
   ```sh
   uvicorn main:app --host 127.0.0.1 --port 8008
   ```

## API
- POST `/chat/init` — Initialize a new chat session with optional system prompt
- POST `/process` — Process instruction (with or without chat context)
- DELETE `/chat/{chat_id}` — Clean up a chat session

### Chat Initialization
```json
POST /chat/init
{
  "model": "llama2",
  "system_prompt": "You are a helpful assistant specialized in code analysis."
}
```

### Processing with Context
```json
POST /process
{
  "model": "llama2",
  "instruction": "Analyze this code",
  "chat_id": "uuid-from-init",
  "parameters": {"temperature": 0.7}
}
```

### Single Query (no context)
```json
POST /process
{
  "model": "llama2",
  "instruction": "What is 2+2?",
  "parameters": {"temperature": 0.1}
}
```

## Project Structure
- `main.py` — FastAPI server entry point with chat management
- `ollama_client.py` — Handles communication with Ollama and chat sessions
- `requirements.txt` — Python dependencies
