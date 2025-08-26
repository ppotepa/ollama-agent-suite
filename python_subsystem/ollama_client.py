import httpx
from typing import Dict, List, Optional

class OllamaClient:
    def __init__(self, base_url: str = "http://localhost:11434"):
        self.base_url = base_url
        self.chat_sessions: Dict[str, Dict] = {}
    
    def init_chat(self, chat_id: str, model: str, system_prompt: Optional[str] = None):
        """Initialize a new chat session"""
        messages = []
        if system_prompt:
            messages.append({"role": "system", "content": system_prompt})
        
        self.chat_sessions[chat_id] = {
            "model": model,
            "messages": messages
        }
    
    def chat_message(self, chat_id: str, message: str, parameters: dict = {}) -> str:
        """Send a message within an existing chat context"""
        if chat_id not in self.chat_sessions:
            raise ValueError(f"Chat session {chat_id} not found")
        
        session = self.chat_sessions[chat_id]
        session["messages"].append({"role": "user", "content": message})
        
        payload = {
            "model": session["model"],
            "messages": session["messages"],
            "stream": False,
            **parameters
        }
        
        try:
            response = httpx.post(f"{self.base_url}/api/chat", json=payload, timeout=60)
            response.raise_for_status()
            data = response.json()
            
            assistant_message = data.get("message", {}).get("content", "")
            
            # Add assistant response to chat history
            session["messages"].append({"role": "assistant", "content": assistant_message})
            
            return assistant_message
        except Exception as e:
            raise RuntimeError(f"Ollama chat API error: {e}")
    
    def single_query(self, model: str, instruction: str, parameters: dict = {}) -> str:
        """Send a single query without maintaining context"""
        url = f"{self.base_url}/api/generate"
        payload = {
            "model": model,
            "prompt": instruction,
            "stream": False,
            **parameters
        }
        
        try:
            response = httpx.post(url, json=payload, timeout=60)
            response.raise_for_status()
            data = response.json()
            return data.get("response", "")
        except Exception as e:
            raise RuntimeError(f"Ollama API error: {e}")
    
    def cleanup_chat(self, chat_id: str):
        """Clean up a chat session"""
        if chat_id in self.chat_sessions:
            del self.chat_sessions[chat_id]

# Legacy function for backward compatibility
def run_ollama(model: str, instruction: str, parameters: dict) -> str:
    client = OllamaClient()
    return client.single_query(model, instruction, parameters)
