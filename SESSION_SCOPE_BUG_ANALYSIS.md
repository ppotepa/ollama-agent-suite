# Session Scope Issue - Root Cause Analysis & Fix

## 🚨 **CRITICAL BUG IDENTIFIED**

### **The Problem**
The `default-session` folder was used instead of the correct session ID (`cb17978e-28ea-498e-8a18-bdf0ee81e59f`) because of a **hardcoded placeholder scope** in the service registration.

### **Root Cause**
```csharp
// In ServiceRegistration.cs line 89
var placeholderScope = new SessionScope(sessionFileSystem, sessionScopeLogger);
placeholderScope.Initialize("default-session");  // 🐛 HARDCODED!

// All new AbstractTool-based tools were registered with this placeholder
toolRepository.RegisterTool(new DirectoryCreateTool(placeholderScope, ...));
```

### **Evidence from Logs**
1. **Session Started**: `cb17978e-28ea-498e-8a18-bdf0ee81e59f`
2. **Tool Used Wrong Path**: `cache\default-session\demo` (from placeholder)
3. **LLM Provided Wrong Path**: AI learned the wrong session from tool context

### **Why This Happened**
1. **Architectural Issue**: ToolRepository is a singleton registered at startup
2. **Session Mismatch**: Tools need session-specific scopes, but were created with hardcoded scope
3. **DI Limitation**: Can't inject scoped services into singleton services at startup

## 🔧 **IMMEDIATE FIX REQUIRED**

The current approach of hardcoding `"default-session"` is fundamentally broken. We need to implement proper session-aware tool instantiation.

### **Solution Options**

#### **Option 1: Factory Pattern (Recommended)**
Modify ToolRepository to support lazy tool instantiation with proper session scope.

#### **Option 2: Service Locator Pattern**
Tools resolve their own session scope at runtime using IServiceProvider.

#### **Option 3: Session-Aware Tools**
Tools accept session ID and create their own scope when needed.

## ⚠️ **CURRENT STATUS**

**The cursor navigation system works correctly** - the session safety is enforced properly. The issue is only that operations are happening in the wrong session directory due to the hardcoded placeholder.

### **What Works:**
- ✅ Session boundaries are enforced 
- ✅ Tools function correctly
- ✅ Security is maintained
- ✅ Build compiles successfully

### **What's Broken:**
- ❌ Wrong session directory used
- ❌ Files created in `default-session` instead of actual session
- ❌ Session context confusion for AI model

## 🚀 **NEXT STEPS**

1. **Immediate**: Remove hardcoded `"default-session"` placeholder
2. **Implement**: Proper session-aware tool instantiation
3. **Test**: Verify correct session directory usage
4. **Verify**: AI model gets correct session context

This is a **high-priority architectural fix** that needs to be implemented to ensure proper session isolation and cursor navigation functionality.
