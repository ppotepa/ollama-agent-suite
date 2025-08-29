using Microsoft.Extensions.Logging;
using Ollama.Domain.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ollama.Infrastructure.Services;

/// <summary>
/// Secure file system implementation that isolates sessions to their cache directories
/// </summary>
public class SessionFileSystem : ISessionFileSystem
{
    private readonly ILogger<SessionFileSystem> _logger;
    private readonly ConcurrentDictionary<string, string> _sessionCurrentDirectories = new();
    private readonly string _baseCacheDirectory;

    public SessionFileSystem(ILogger<SessionFileSystem> logger)
    {
        _logger = logger;
        _baseCacheDirectory = GetBaseCacheDirectory();
        EnsureCacheDirectoryExists();
    }

    public string GetSessionRoot(string sessionId)
    {
        ValidateSessionId(sessionId);
        return Path.Combine(_baseCacheDirectory, sessionId);
    }

    public string GetCurrentDirectory(string sessionId)
    {
        ValidateSessionId(sessionId);
        var sessionRoot = GetSessionRoot(sessionId);
        
        if (!_sessionCurrentDirectories.TryGetValue(sessionId, out var currentDir))
        {
            // Initialize with session root
            currentDir = sessionRoot;
            _sessionCurrentDirectories.TryAdd(sessionId, currentDir);
            
            // Ensure the session directory exists
            Directory.CreateDirectory(sessionRoot);
            _logger.LogDebug("Initialized session {SessionId} with root directory: {SessionRoot}", sessionId, sessionRoot);
        }

        return currentDir;
    }

    public string ChangeDirectory(string sessionId, string relativePath)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(relativePath);

        var currentDir = GetCurrentDirectory(sessionId);
        var sessionRoot = GetSessionRoot(sessionId);
        
        // Handle special cases
        if (relativePath == "." || string.IsNullOrEmpty(relativePath))
        {
            return currentDir;
        }
        
        if (relativePath == "..")
        {
            var parent = Directory.GetParent(currentDir);
            if (parent != null && ValidatePathIsWithinSessionBounds(parent.FullName, sessionRoot))
            {
                var newCurrentDir = parent.FullName;
                _sessionCurrentDirectories.AddOrUpdate(sessionId, newCurrentDir, (key, existing) => newCurrentDir);
                _logger.LogDebug("Session {SessionId}: Changed directory to {Directory}", sessionId, GetRelativePathFromSessionRoot(newCurrentDir, sessionRoot));
                return newCurrentDir;
            }
            else
            {
                // Can't go above session root
                _logger.LogWarning("Session {SessionId}: Attempted to navigate above session root", sessionId);
                return currentDir;
            }
        }

        // Handle absolute path relative to session root
        if (Path.IsPathRooted(relativePath))
        {
            throw new UnauthorizedAccessException("Absolute paths are not allowed. Use relative paths only.");
        }

        // Combine and resolve the path
        var targetPath = Path.GetFullPath(Path.Combine(currentDir, relativePath));
        
        // Ensure the target path is within session boundaries
        if (!ValidatePathIsWithinSessionBounds(targetPath, sessionRoot))
        {
            throw new UnauthorizedAccessException($"Path '{relativePath}' would escape session boundary");
        }

        // Create directory if it doesn't exist
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
            _logger.LogDebug("Session {SessionId}: Created directory {Directory}", sessionId, GetRelativePathFromSessionRoot(targetPath, sessionRoot));
        }

        _sessionCurrentDirectories.AddOrUpdate(sessionId, targetPath, (key, existing) => targetPath);
        _logger.LogDebug("Session {SessionId}: Changed directory to {Directory}", sessionId, GetRelativePathFromSessionRoot(targetPath, sessionRoot));
        
        return targetPath;
    }

    public string CreateDirectory(string sessionId, string relativePath)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(relativePath);

        var fullPath = GetSecurePath(sessionId, relativePath);
        Directory.CreateDirectory(fullPath);
        
        _logger.LogInformation("Session {SessionId}: Created directory {Directory}", sessionId, GetRelativePathFromSessionRoot(fullPath, GetSessionRoot(sessionId)));
        return fullPath;
    }

    public string WriteFile(string sessionId, string relativePath, string content)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(relativePath);

        var fullPath = GetSecurePath(sessionId, relativePath);
        
        // Ensure parent directory exists
        var parentDir = Path.GetDirectoryName(fullPath);
        if (parentDir != null && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        File.WriteAllText(fullPath, content);
        
        _logger.LogInformation("Session {SessionId}: Wrote text file {File} ({Size} chars)", 
            sessionId, GetRelativePathFromSessionRoot(fullPath, GetSessionRoot(sessionId)), content.Length);
        
        return fullPath;
    }

    public string WriteFile(string sessionId, string relativePath, byte[] content)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(relativePath);

        var fullPath = GetSecurePath(sessionId, relativePath);
        
        // Ensure parent directory exists
        var parentDir = Path.GetDirectoryName(fullPath);
        if (parentDir != null && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        File.WriteAllBytes(fullPath, content);
        
        _logger.LogInformation("Session {SessionId}: Wrote binary file {File} ({Size} bytes)", 
            sessionId, GetRelativePathFromSessionRoot(fullPath, GetSessionRoot(sessionId)), content.Length);
        
        return fullPath;
    }

    public string ReadFile(string sessionId, string relativePath)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(relativePath);

        var fullPath = GetSecurePath(sessionId, relativePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        var content = File.ReadAllText(fullPath);
        
        _logger.LogDebug("Session {SessionId}: Read text file {File} ({Size} chars)", 
            sessionId, GetRelativePathFromSessionRoot(fullPath, GetSessionRoot(sessionId)), content.Length);
        
        return content;
    }

    public byte[] ReadFileBytes(string sessionId, string relativePath)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(relativePath);

        var fullPath = GetSecurePath(sessionId, relativePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        var content = File.ReadAllBytes(fullPath);
        
        _logger.LogDebug("Session {SessionId}: Read binary file {File} ({Size} bytes)", 
            sessionId, GetRelativePathFromSessionRoot(fullPath, GetSessionRoot(sessionId)), content.Length);
        
        return content;
    }

    public bool FileExists(string sessionId, string relativePath)
    {
        try
        {
            ValidateSessionId(sessionId);
            ValidateRelativePath(relativePath);
            var fullPath = GetSecurePath(sessionId, relativePath);
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    public bool DirectoryExists(string sessionId, string relativePath)
    {
        try
        {
            ValidateSessionId(sessionId);
            ValidateRelativePath(relativePath);
            var fullPath = GetSecurePath(sessionId, relativePath);
            return Directory.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<string> ListFiles(string sessionId, string relativePath = "", string searchPattern = "*")
    {
        ValidateSessionId(sessionId);
        
        var targetDir = string.IsNullOrEmpty(relativePath) 
            ? GetCurrentDirectory(sessionId) 
            : GetSecurePath(sessionId, relativePath);

        if (!Directory.Exists(targetDir))
        {
            return Enumerable.Empty<string>();
        }

        var sessionRoot = GetSessionRoot(sessionId);
        var files = Directory.GetFiles(targetDir, searchPattern)
            .Select(f => GetRelativePathFromSessionRoot(f, sessionRoot))
            .ToList();

        _logger.LogDebug("Session {SessionId}: Listed {Count} files in {Directory}", 
            sessionId, files.Count, GetRelativePathFromSessionRoot(targetDir, sessionRoot));

        return files;
    }

    public IEnumerable<string> ListDirectories(string sessionId, string relativePath = "")
    {
        ValidateSessionId(sessionId);
        
        var targetDir = string.IsNullOrEmpty(relativePath) 
            ? GetCurrentDirectory(sessionId) 
            : GetSecurePath(sessionId, relativePath);

        if (!Directory.Exists(targetDir))
        {
            return Enumerable.Empty<string>();
        }

        var sessionRoot = GetSessionRoot(sessionId);
        var directories = Directory.GetDirectories(targetDir)
            .Select(d => GetRelativePathFromSessionRoot(d, sessionRoot))
            .ToList();

        _logger.LogDebug("Session {SessionId}: Listed {Count} directories in {Directory}", 
            sessionId, directories.Count, GetRelativePathFromSessionRoot(targetDir, sessionRoot));

        return directories;
    }

    public bool DeleteFile(string sessionId, string relativePath)
    {
        try
        {
            ValidateSessionId(sessionId);
            ValidateRelativePath(relativePath);

            var fullPath = GetSecurePath(sessionId, relativePath);
            
            if (!File.Exists(fullPath))
            {
                return false;
            }

            File.Delete(fullPath);
            
            _logger.LogInformation("Session {SessionId}: Deleted file {File}", 
                sessionId, GetRelativePathFromSessionRoot(fullPath, GetSessionRoot(sessionId)));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Failed to delete file {File}", sessionId, relativePath);
            return false;
        }
    }

    public bool DeleteDirectory(string sessionId, string relativePath, bool recursive = false)
    {
        try
        {
            ValidateSessionId(sessionId);
            ValidateRelativePath(relativePath);

            var fullPath = GetSecurePath(sessionId, relativePath);
            var sessionRoot = GetSessionRoot(sessionId);
            
            // Don't allow deleting the session root itself
            if (string.Equals(fullPath, sessionRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Session {SessionId}: Attempted to delete session root directory", sessionId);
                return false;
            }

            if (!Directory.Exists(fullPath))
            {
                return false;
            }

            Directory.Delete(fullPath, recursive);
            
            _logger.LogInformation("Session {SessionId}: Deleted directory {Directory} (recursive: {Recursive})", 
                sessionId, GetRelativePathFromSessionRoot(fullPath, sessionRoot), recursive);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Failed to delete directory {Directory}", sessionId, relativePath);
            return false;
        }
    }

    public FileInfo? GetFileInfo(string sessionId, string relativePath)
    {
        try
        {
            ValidateSessionId(sessionId);
            ValidateRelativePath(relativePath);

            var fullPath = GetSecurePath(sessionId, relativePath);
            return File.Exists(fullPath) ? new FileInfo(fullPath) : null;
        }
        catch
        {
            return null;
        }
    }

    public DirectoryInfo? GetDirectoryInfo(string sessionId, string relativePath)
    {
        try
        {
            ValidateSessionId(sessionId);
            ValidateRelativePath(relativePath);

            var fullPath = GetSecurePath(sessionId, relativePath);
            return Directory.Exists(fullPath) ? new DirectoryInfo(fullPath) : null;
        }
        catch
        {
            return null;
        }
    }

    public string CopyFile(string sessionId, string sourceRelativePath, string destinationRelativePath, bool overwrite = false)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(sourceRelativePath);
        ValidateRelativePath(destinationRelativePath);

        var sourcePath = GetSecurePath(sessionId, sourceRelativePath);
        var destinationPath = GetSecurePath(sessionId, destinationRelativePath);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourceRelativePath}");
        }

        // Ensure destination directory exists
        var destDir = Path.GetDirectoryName(destinationPath);
        if (destDir != null && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        File.Copy(sourcePath, destinationPath, overwrite);

        var sessionRoot = GetSessionRoot(sessionId);
        _logger.LogInformation("Session {SessionId}: Copied file from {Source} to {Destination}", 
            sessionId, 
            GetRelativePathFromSessionRoot(sourcePath, sessionRoot),
            GetRelativePathFromSessionRoot(destinationPath, sessionRoot));

        return destinationPath;
    }

    public string MoveFile(string sessionId, string sourceRelativePath, string destinationRelativePath)
    {
        ValidateSessionId(sessionId);
        ValidateRelativePath(sourceRelativePath);
        ValidateRelativePath(destinationRelativePath);

        var sourcePath = GetSecurePath(sessionId, sourceRelativePath);
        var destinationPath = GetSecurePath(sessionId, destinationRelativePath);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourceRelativePath}");
        }

        // Ensure destination directory exists
        var destDir = Path.GetDirectoryName(destinationPath);
        if (destDir != null && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        File.Move(sourcePath, destinationPath);

        var sessionRoot = GetSessionRoot(sessionId);
        _logger.LogInformation("Session {SessionId}: Moved file from {Source} to {Destination}", 
            sessionId, 
            GetRelativePathFromSessionRoot(sourcePath, sessionRoot),
            GetRelativePathFromSessionRoot(destinationPath, sessionRoot));

        return destinationPath;
    }

    public long GetSessionDirectorySize(string sessionId)
    {
        ValidateSessionId(sessionId);
        
        var sessionRoot = GetSessionRoot(sessionId);
        if (!Directory.Exists(sessionRoot))
        {
            return 0;
        }

        return CalculateDirectorySize(sessionRoot);
    }

    public bool IsWorkingDirectoryValid(string sessionId, string workingDirectory)
    {
        try
        {
            ValidateSessionId(sessionId);
            var sessionRoot = GetSessionRoot(sessionId);
            return ValidatePathIsWithinSessionBounds(workingDirectory, sessionRoot);
        }
        catch
        {
            return false;
        }
    }

    public string GetSafeWorkingDirectory(string sessionId)
    {
        ValidateSessionId(sessionId);
        // Always return the session root for maximum security
        // This ensures external commands can never escape the session boundary
        var sessionRoot = GetSessionRoot(sessionId);
        
        // Ensure the session directory exists
        if (!Directory.Exists(sessionRoot))
        {
            Directory.CreateDirectory(sessionRoot);
            _logger.LogDebug("Created session root directory: {SessionRoot}", sessionRoot);
        }
        
        // Log for security auditing
        _logger.LogDebug("Session {SessionId}: Providing safe working directory: {WorkingDirectory}", 
            sessionId, sessionRoot);
        
        return sessionRoot;
    }

    public bool IsWithinSessionBoundary(string sessionId, string path)
    {
        try
        {
            ValidateSessionId(sessionId);
            var sessionRoot = GetSessionRoot(sessionId);
            return ValidatePathIsWithinSessionBounds(path, sessionRoot);
        }
        catch
        {
            return false;
        }
    }

    public void CleanupSession(string sessionId)
    {
        ValidateSessionId(sessionId);
        
        var sessionRoot = GetSessionRoot(sessionId);
        
        try
        {
            if (Directory.Exists(sessionRoot))
            {
                Directory.Delete(sessionRoot, true);
                _logger.LogInformation("Session {SessionId}: Cleaned up session directory", sessionId);
            }

            _sessionCurrentDirectories.TryRemove(sessionId, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Failed to cleanup session directory", sessionId);
            throw;
        }
    }

    // Private helper methods

    private string GetSecurePath(string sessionId, string relativePath)
    {
        var currentDir = GetCurrentDirectory(sessionId);
        var sessionRoot = GetSessionRoot(sessionId);
        
        if (Path.IsPathRooted(relativePath))
        {
            throw new UnauthorizedAccessException("Absolute paths are not allowed. Use relative paths only.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(currentDir, relativePath));
        
        if (!ValidatePathIsWithinSessionBounds(fullPath, sessionRoot))
        {
            throw new UnauthorizedAccessException($"Path '{relativePath}' would escape session boundary");
        }

        return fullPath;
    }

    private bool ValidatePathIsWithinSessionBounds(string path, string sessionRoot)
    {
        var normalizedPath = Path.GetFullPath(path);
        var normalizedRoot = Path.GetFullPath(sessionRoot);
        
        // Ensure the path starts with the session root and doesn't allow any escapes
        bool isWithinBounds = normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        
        // Additional security check: ensure no directory traversal attempts
        if (isWithinBounds)
        {
            // Check that the normalized path doesn't contain any ".." components after normalization
            // This prevents crafted paths that might escape after normalization
            var relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);
            isWithinBounds = !relativePath.StartsWith(".." + Path.DirectorySeparatorChar) && 
                           !relativePath.Contains(".." + Path.DirectorySeparatorChar) &&
                           relativePath != "..";
        }
        
        if (!isWithinBounds)
        {
            _logger.LogWarning("Session boundary violation detected: Path '{Path}' is outside session root '{SessionRoot}'", 
                normalizedPath, normalizedRoot);
        }
        
        return isWithinBounds;
    }

    private string GetRelativePathFromSessionRoot(string fullPath, string sessionRoot)
    {
        var normalizedPath = Path.GetFullPath(fullPath);
        var normalizedRoot = Path.GetFullPath(sessionRoot);
        
        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath; // Return as-is if not within session root
        }

        var relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);
        return relativePath == "." ? "." : relativePath;
    }

    private long CalculateDirectorySize(string directoryPath)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            return directoryInfo.GetFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    private void ValidateSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        // Validate session ID format to prevent path injection
        if (sessionId.Contains("..") || sessionId.Contains("/") || sessionId.Contains("\\") || 
            Path.GetInvalidFileNameChars().Any(c => sessionId.Contains(c)))
        {
            throw new ArgumentException("Invalid session ID format", nameof(sessionId));
        }
    }

    private void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return; // Empty paths are allowed (current directory)
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Absolute paths are not allowed", nameof(relativePath));
        }

        // Check for dangerous path components
        var normalizedPath = Path.GetFullPath(Path.Combine("dummy", relativePath));
        if (normalizedPath.Contains(".."))
        {
            // This should be caught by the boundary check, but let's be extra safe
            _logger.LogWarning("Potentially dangerous relative path detected: {RelativePath}", relativePath);
        }
    }

    private string GetBaseCacheDirectory()
    {
        try
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                // Look for solution file or .git folder as repo root markers
                if (File.Exists(Path.Combine(dir, "OllamaAgentSuite.sln")) || Directory.Exists(Path.Combine(dir, ".git")))
                    return Path.Combine(dir, "cache");

                var parent = Directory.GetParent(dir);
                if (parent == null)
                    break;
                dir = parent.FullName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not find repository root, using current directory");
        }

        // Fallback to current directory
        return Path.Combine(Directory.GetCurrentDirectory(), "cache");
    }

    private void EnsureCacheDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_baseCacheDirectory))
            {
                Directory.CreateDirectory(_baseCacheDirectory);
                _logger.LogInformation("Created base cache directory: {CacheDirectory}", _baseCacheDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create base cache directory: {CacheDirectory}", _baseCacheDirectory);
            throw;
        }
    }

    public void ClearEntireCache()
    {
        try
        {
            if (Directory.Exists(_baseCacheDirectory))
            {
                _logger.LogInformation("Clearing entire cache directory: {CacheDirectory}", _baseCacheDirectory);
                
                // Clear all session directories
                _sessionCurrentDirectories.Clear();
                
                // Delete all contents of the cache directory
                var directoryInfo = new DirectoryInfo(_baseCacheDirectory);
                foreach (var file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                
                _logger.LogInformation("Successfully cleared cache directory: {CacheDirectory}", _baseCacheDirectory);
            }
            else
            {
                _logger.LogInformation("Cache directory does not exist, nothing to clear: {CacheDirectory}", _baseCacheDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache directory: {CacheDirectory}", _baseCacheDirectory);
            throw;
        }
    }
}
