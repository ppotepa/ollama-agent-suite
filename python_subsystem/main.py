from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from ollama_client import OllamaClient
import uuid

app = FastAPI()
ollama_client = OllamaClient()

class ChatInitRequest(BaseModel):
    model: str
    system_prompt: str | None = None

class ChatInitResponse(BaseModel):
    chat_id: str
    error: str | None = None

class ProcessRequest(BaseModel):
    model: str
    instruction: str
    chat_id: str | None = None
    parameters: dict = {}

class ProcessResponse(BaseModel):
    result: str | None = None
    chat_id: str | None = None
    error: str | None = None

@app.post("/chat/init", response_model=ChatInitResponse)
def init_chat(req: ChatInitRequest):
    """Initialize a new chat session with optional system prompt"""
    try:
        chat_id = str(uuid.uuid4())
        ollama_client.init_chat(chat_id, req.model, req.system_prompt)
        return ChatInitResponse(chat_id=chat_id)
    except Exception as e:
        return ChatInitResponse(chat_id="", error=str(e))

@app.post("/process", response_model=ProcessResponse)
def process(req: ProcessRequest):
    """Process a single instruction, optionally within a chat context"""
    try:
        if req.chat_id:
            # Use existing chat context
            result = ollama_client.chat_message(req.chat_id, req.instruction, req.parameters)
        else:
            # Single query without context
            result = ollama_client.single_query(req.model, req.instruction, req.parameters)
        
        return ProcessResponse(result=result, chat_id=req.chat_id)
    except Exception as e:
        return ProcessResponse(error=str(e), chat_id=req.chat_id)

@app.delete("/chat/{chat_id}")
def cleanup_chat(chat_id: str):
    """Clean up a chat session"""
    try:
        ollama_client.cleanup_chat(chat_id)
        return {"status": "success"}
    except Exception as e:
        return {"error": str(e)}
