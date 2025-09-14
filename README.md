┌─────────────────────────────────────────────────────────────┐
│                    CLEAN ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐                                            │
│  │ Interface   │  CLI Program                               │
│  │   Layer     │  → Entry Point                             │
│  └─────────────┘  → Argument Parsing                        │
│         │         → User Interaction                        │
│         ▼                                                    │
│  ┌─────────────┐                                            │
│  │ Application │  Orchestrator                              │
│  │   Layer     │  → Strategy Selection                      │
│  └─────────────┘  → Execution Coordination                  │
│         │         → Mode Registry                           │
│         ▼                                                    │
│  ┌─────────────┐                                            │
│  │   Domain    │  Pure Business Logic                       │
│  │   Layer     │  → Agents & Strategies                     │
│  └─────────────┘  → Execution Models                        │
│         │         → Tool Contracts                          │
│         ▼                                                    │
│  ┌───────────────┐                                          │
│  │ Infrastructure │  External Integrations                  │
│  │     Layer      │  → Ollama Client                        │
│  └───────────────┘  → File System Tools                     │
│         │         → Session Management                      │
│         ▼                                                    │
│  ┌─────────────┐                                            │
│  │ Bootstrap   │  Dependency Injection                      │
│  │   Layer     │  → Service Registration                    │
│  └─────────────┘  → Configuration Setup                     │
└─────────────────────────────────────────────────────────────┘
