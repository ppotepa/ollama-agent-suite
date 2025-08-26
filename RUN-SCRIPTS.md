# Ollama Agent Suite - Run Scripts

This directory contains convenient scripts to run the Ollama Agent Suite with intelligent mode.

## Available Scripts

### 🔄 Interactive Mode
**`run-interactive.ps1`** (PowerShell) or **`run-interactive.bat`** (Batch)
- Continuously prompts for queries
- Runs each query in intelligent mode with AI planning
- Shows execution time and status
- Type 'exit' or 'quit' to exit

**Usage:**
```powershell
# PowerShell
.\run-interactive.ps1

# Command Prompt
run-interactive.bat
```

### ⚡ Quick Query
**`run-query.ps1`** (PowerShell)
- Run a single query quickly
- Can be used with parameters or interactive input
- Shows execution time and status

**Usage:**
```powershell
# With parameter
.\run-query.ps1 "What is 2 + 2?"

# Interactive input
.\run-query.ps1
```

## Features

All scripts include:
- ✅ **Auto-build**: Automatically builds the solution if needed
- 🎯 **Intelligent Mode**: Uses AI planning with llama3.1:8b-instruct
- 📊 **Process Isolation**: Python subsystem runs in isolated process
- ⏱️ **Timing**: Shows execution duration
- 🎨 **Color Output**: Easy-to-read colored console output
- 🛡️ **Error Handling**: Graceful error handling and reporting

## Example Queries

Try these sample queries:

### Simple Math
```
What is 2 + 2?
Calculate the square root of 144
```

### Explanations
```
Explain quantum computing in simple terms
How does machine learning work?
What is the difference between AI and ML?
```

### Code Generation
```
Write a Python function to sort a list
Create a REST API endpoint in C#
Generate a SQL query to find duplicate records
```

### Analysis
```
Analyze this code for potential bugs
Review this algorithm for efficiency
Suggest improvements for this design pattern
```

## Requirements

- .NET 9.0 SDK
- Python with uvicorn, fastapi, and httpx
- Ollama with llama3.1:8b and llama3.1:8b-instruct models
- PowerShell 5.1+ (for .ps1 scripts)

## Notes

- The intelligent mode uses sophisticated AI planning to break down complex queries
- Python subsystem automatically starts/stops in isolated process
- All model usage and execution steps are logged with emojis for clarity
- Configuration is automatically resolved from solution root
