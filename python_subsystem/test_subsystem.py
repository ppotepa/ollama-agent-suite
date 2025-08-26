"""
Test script for the Python subsystem
"""
import requests
import json

BASE_URL = "http://localhost:8008"

def test_single_query():
    """Test a single query without context"""
    print("Testing single query...")
    response = requests.post(f"{BASE_URL}/process", json={
        "model": "llama2",
        "instruction": "What is the capital of France?",
        "parameters": {"temperature": 0.1}
    })
    
    if response.status_code == 200:
        result = response.json()
        print(f"✓ Single query result: {result['result'][:100]}...")
    else:
        print(f"✗ Single query failed: {response.text}")

def test_chat_session():
    """Test chat initialization and context management"""
    print("\nTesting chat session...")
    
    # Initialize chat
    init_response = requests.post(f"{BASE_URL}/chat/init", json={
        "model": "llama2",
        "system_prompt": "You are a helpful assistant. Keep responses brief."
    })
    
    if init_response.status_code != 200:
        print(f"✗ Chat init failed: {init_response.text}")
        return
    
    chat_id = init_response.json()["chat_id"]
    print(f"✓ Chat initialized with ID: {chat_id[:8]}...")
    
    # Send first message
    msg1_response = requests.post(f"{BASE_URL}/process", json={
        "model": "llama2",
        "instruction": "My name is John. What's yours?",
        "chat_id": chat_id,
        "parameters": {"temperature": 0.7}
    })
    
    if msg1_response.status_code == 200:
        result1 = msg1_response.json()
        print(f"✓ First message result: {result1['result'][:100]}...")
    else:
        print(f"✗ First message failed: {msg1_response.text}")
        return
    
    # Send follow-up message
    msg2_response = requests.post(f"{BASE_URL}/process", json={
        "model": "llama2",
        "instruction": "What's my name?",
        "chat_id": chat_id,
        "parameters": {"temperature": 0.7}
    })
    
    if msg2_response.status_code == 200:
        result2 = msg2_response.json()
        print(f"✓ Follow-up message result: {result2['result'][:100]}...")
    else:
        print(f"✗ Follow-up message failed: {msg2_response.text}")
    
    # Cleanup
    cleanup_response = requests.delete(f"{BASE_URL}/chat/{chat_id}")
    if cleanup_response.status_code == 200:
        print(f"✓ Chat session cleaned up")
    else:
        print(f"✗ Cleanup failed: {cleanup_response.text}")

if __name__ == "__main__":
    print("Python Subsystem Test Suite")
    print("=" * 40)
    print("Make sure the server is running with: uvicorn main:app --host 127.0.0.1 --port 8008")
    print()
    
    try:
        test_single_query()
        test_chat_session()
        print("\n✓ All tests completed!")
    except requests.exceptions.ConnectionError:
        print("✗ Could not connect to server. Make sure it's running on localhost:8008")
    except Exception as e:
        print(f"✗ Test failed with error: {e}")
