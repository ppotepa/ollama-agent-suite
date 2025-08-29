# Multi-Format Response Parsing System

## Overview

The Ollama Agent Suite now includes a robust multi-format fallback parsing system to handle various LLM response formats when JSON parsing fails. This solves the critical issue where LLM responses containing code blocks with unescaped curly braces would break JSON parsing.

## Problem Solved

**Original Issue**: LLM responses containing C# code with curly braces (`{`, `}`) were causing JSON parsing failures:
```
Error: '{' is invalid after a value. Expected either ',', '}', or ']'. Path: $ | LineNumber: 11 | BytePositionInLine: 877
```

**Root Cause**: Code blocks in JSON strings contained unescaped braces that confused the JSON parser about object boundaries.

## Solution: Multi-Format Fallback System

### Architecture

The system tries multiple parsing strategies in order of preference:

1. **JSON Format** (Primary) - Standard JSON extraction and parsing
2. **YAML Format** - YAML-like key:value parsing 
3. **Key-Value Format** - Simple key=value or key:value pairs
4. **Markdown Format** - Markdown headers with content sections
5. **Plain Text Format** - Last resort, treats entire response as text

### Implementation Details

#### Location
`src/Ollama.Infrastructure/Strategies/PessimisticAgentStrategy.cs`

#### Key Methods

1. **`TryParseWithFallbacks(string response, string? sessionId)`**
   - Orchestrates the fallback sequence
   - Logs which strategy succeeded
   - Returns `JsonElement?` or null if all fail

2. **`TryParseJsonFormat(string response)`**
   - Enhanced JSON extraction with better error handling
   - Uses existing `ExtractJsonFromResponse` and `NormalizeJsonForParsing`
   - Handles malformed JSON more gracefully

3. **`TryParseYamlFormat(string response)`**
   - Parses YAML-like structures: `key: value`
   - Handles multiline values with `|` syntax
   - Automatically converts to JSON structure

4. **`TryParseKeyValueFormat(string response)`**
   - Supports both `key=value` and `key: value` formats
   - Type detection for booleans, numbers, strings
   - Useful for simple LLM responses

5. **`TryParseMarkdownFormat(string response)`**
   - Extracts content from markdown headers (`##`, `**`)
   - Detects task completion from keywords
   - Converts structured markdown to JSON

6. **`TryParsePlainTextFormat(string response)`**
   - Ultimate fallback for any text response
   - Detects code patterns to improve response quality
   - Always returns valid JSON structure

## Supported Input Formats

### 1. Standard JSON
```json
{
  "taskCompleted": true,
  "response": "Generated code successfully",
  "nextStep": null
}
```

### 2. YAML-Like Format
```yaml
taskCompleted: true
response: |
  Here's your code:
  ```csharp
  public class Example { }
  ```
nextStep: null
```

### 3. Key-Value Format
```
taskCompleted=true
response=Generated a simple controller class
nextStep=null
```

### 4. Markdown Format
```markdown
## Task Status
Complete

## Response
Here's your web API controller:

```csharp
[ApiController]
public class UserController : ControllerBase
{
    [HttpGet]
    public IActionResult GetUsers() => Ok();
}
```
```

### 5. Plain Text
```
The task is complete. Here's a simple controller:

using Microsoft.AspNetCore.Mvc;

[ApiController]
public class UserController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Hello");
}
```

## Benefits

### 1. **Robustness**
- System no longer fails on malformed JSON
- Graceful degradation through multiple parsing strategies
- Comprehensive error logging for debugging

### 2. **Flexibility**
- Supports various LLM output styles
- Accommodates different model personalities
- Future-proof for new response formats

### 3. **Backward Compatibility**
- Existing JSON responses work unchanged
- Legacy validation logic preserved
- No breaking changes to existing functionality

### 4. **Enhanced User Experience**
- Consistent response structure regardless of input format
- Better error messages when all parsing fails
- Automatic format detection and conversion

## Logging and Monitoring

The system provides comprehensive logging at different levels:

- **Debug**: Each parsing strategy attempt
- **Information**: Successful parsing strategy used
- **Warning**: Individual strategy failures
- **Error**: Complete parsing failure with details

Example log output:
```
info: Attempting JSON Format parsing for session abc123
warn: JSON Format parsing failed for session abc123: Invalid character
info: Attempting YAML Format parsing for session abc123
info: Successfully parsed response using YAML Format for session abc123
```

## Error Handling

When all parsing strategies fail:
1. Error logged with session context
2. Fallback response created with diagnostic information
3. System continues operation (no crashes)
4. Raw response excerpt included for debugging

## Configuration

The system automatically activates when:
- JSON parsing encounters errors
- Response contains code blocks with braces
- LLM outputs non-standard formats

No additional configuration required - it's a built-in enhancement to the existing validation system.

## Testing

### Manual Testing
You can test different formats by simulating LLM responses:

```bash
# Test YAML format
echo "taskCompleted: true
response: Generated code
nextStep: null" > test_yaml.txt

# Test key-value format  
echo "taskCompleted=true
response=Task completed successfully" > test_kv.txt
```

### Integration Testing
The system is automatically tested during normal operation:
- Standard JSON responses continue to work
- Malformed responses are gracefully handled
- All formats produce consistent output structure

## Future Enhancements

Potential improvements for future versions:

1. **XML Format Support** - For responses in XML structure
2. **CSV Format Support** - For tabular data responses
3. **Custom Format Registration** - Allow plugins to register new formats
4. **Format Probability Detection** - Machine learning to predict best format
5. **Performance Optimization** - Caching and parallel parsing strategies

## Migration Notes

This enhancement is:
- **Zero-impact** on existing functionality
- **Automatic** - no code changes required
- **Additive** - only improves existing capabilities
- **Logged** - full visibility into parsing decisions

The multi-format fallback system represents a significant improvement in system reliability and user experience, ensuring that LLM communication remains robust regardless of response format variations.
