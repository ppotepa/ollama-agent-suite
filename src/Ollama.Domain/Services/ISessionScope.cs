using System;

namespace Ollama.Domain.Services
{
    /// <summary>
    /// Provides session-scoped access to session context and services
    /// Injected into tools to provide automatic session awareness
    /// </summary>
    public interface ISessionScope
    {
        /// <summary>
        /// Current session identifier
        /// </summary>
        string SessionId { get; }
        
        /// <summary>
        /// Session file system for safe file operations
        /// </summary>
        ISessionFileSystem FileSystem { get; }
        
        /// <summary>
        /// Current working directory within session
        /// </summary>
        string WorkingDirectory { get; }
        
        /// <summary>
        /// Session root directory (read-only)
        /// </summary>
        string SessionRoot { get; }
        
        /// <summary>
        /// Initialize the session scope with session ID
        /// Called by tools to ensure correct session context
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        void Initialize(string sessionId);
        
        /// <summary>
        /// Validate that a path is within session boundaries
        /// </summary>
        /// <param name="path">Path to validate</param>
        /// <returns>True if path is safe</returns>
        bool IsPathValid(string path);
        
        /// <summary>
        /// Get a safe path within session boundaries
        /// </summary>
        /// <param name="relativePath">Relative path from working directory</param>
        /// <returns>Absolute path within session</returns>
        string GetSafePath(string relativePath);
        
        /// <summary>
        /// Change working directory within session
        /// </summary>
        /// <param name="relativePath">Relative path to change to</param>
        /// <returns>New working directory</returns>
        string ChangeDirectory(string relativePath);
    }
}
