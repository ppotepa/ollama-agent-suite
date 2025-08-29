using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Services
{
    /// <summary>
    /// Session-scoped service providing session context and safe operations
    /// Registered as scoped service and injected into tools
    /// </summary>
    public class SessionScope : ISessionScope
    {
        private readonly ISessionFileSystem _sessionFileSystem;
        private readonly ILogger<SessionScope> _logger;
        private string _currentWorkingDirectory;

        public string SessionId { get; private set; }
        public ISessionFileSystem FileSystem => _sessionFileSystem;
        public string SessionRoot => _sessionFileSystem.GetSessionRoot(SessionId);
        public string WorkingDirectory => _currentWorkingDirectory;

        public SessionScope(ISessionFileSystem sessionFileSystem, ILogger<SessionScope> logger)
        {
            _sessionFileSystem = sessionFileSystem;
            _logger = logger;
            SessionId = string.Empty;
            _currentWorkingDirectory = string.Empty;
        }

        /// <summary>
        /// Initialize the session scope with session ID
        /// Called by the agent when starting a session
        /// </summary>
        public void Initialize(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            SessionId = sessionId;
            _currentWorkingDirectory = _sessionFileSystem.GetCurrentDirectory(sessionId);
            
            _logger.LogDebug("SessionScope initialized for session {SessionId} with working directory {WorkingDirectory}", 
                SessionId, _currentWorkingDirectory);
        }

        public bool IsPathValid(string path)
        {
            if (string.IsNullOrEmpty(SessionId))
                throw new InvalidOperationException("SessionScope not initialized");

            return _sessionFileSystem.IsWithinSessionBoundary(SessionId, path);
        }

        public string GetSafePath(string relativePath)
        {
            if (string.IsNullOrEmpty(SessionId))
                throw new InvalidOperationException("SessionScope not initialized");

            if (string.IsNullOrEmpty(relativePath))
                return _currentWorkingDirectory;

            var fullPath = Path.GetFullPath(Path.Combine(_currentWorkingDirectory, relativePath));
            
            if (!IsPathValid(fullPath))
                throw new UnauthorizedAccessException($"Path '{relativePath}' would escape session boundary");

            return fullPath;
        }

        public string ChangeDirectory(string relativePath)
        {
            if (string.IsNullOrEmpty(SessionId))
                throw new InvalidOperationException("SessionScope not initialized");

            var newDirectory = _sessionFileSystem.ChangeDirectory(SessionId, relativePath);
            _currentWorkingDirectory = newDirectory;
            
            _logger.LogDebug("SessionScope changed working directory to {WorkingDirectory}", _currentWorkingDirectory);
            
            return _currentWorkingDirectory;
        }
    }
}
