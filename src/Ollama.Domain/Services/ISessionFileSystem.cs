using System;
using System.Collections.Generic;
using System.IO;

namespace Ollama.Domain.Services;

/// <summary>
/// Provides secure file system operations within session boundaries
/// Each session is isolated to its /cache/[sessionId]/ directory
/// </summary>
public interface ISessionFileSystem
{
    /// <summary>
    /// Get the session's root directory (read-only)
    /// </summary>
    string GetSessionRoot(string sessionId);

    /// <summary>
    /// Get current working directory within session
    /// </summary>
    string GetCurrentDirectory(string sessionId);

    /// <summary>
    /// Change directory within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>New current directory</returns>
    /// <exception cref="UnauthorizedAccessException">When trying to escape session boundary</exception>
    string ChangeDirectory(string sessionId, string relativePath);

    /// <summary>
    /// Create a directory within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>Full path of created directory</returns>
    string CreateDirectory(string sessionId, string relativePath);

    /// <summary>
    /// Write text to a file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <param name="content">File content</param>
    /// <returns>Full path of written file</returns>
    string WriteFile(string sessionId, string relativePath, string content);

    /// <summary>
    /// Write bytes to a file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <param name="content">File content as bytes</param>
    /// <returns>Full path of written file</returns>
    string WriteFile(string sessionId, string relativePath, byte[] content);

    /// <summary>
    /// Read text from a file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>File content as text</returns>
    string ReadFile(string sessionId, string relativePath);

    /// <summary>
    /// Read bytes from a file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>File content as bytes</returns>
    byte[] ReadFileBytes(string sessionId, string relativePath);

    /// <summary>
    /// Check if file exists within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>True if file exists</returns>
    bool FileExists(string sessionId, string relativePath);

    /// <summary>
    /// Check if directory exists within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>True if directory exists</returns>
    bool DirectoryExists(string sessionId, string relativePath);

    /// <summary>
    /// List files in directory within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory (empty for current)</param>
    /// <param name="searchPattern">Search pattern (default: "*")</param>
    /// <returns>List of file paths relative to session root</returns>
    IEnumerable<string> ListFiles(string sessionId, string relativePath = "", string searchPattern = "*");

    /// <summary>
    /// List directories within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory (empty for current)</param>
    /// <returns>List of directory paths relative to session root</returns>
    IEnumerable<string> ListDirectories(string sessionId, string relativePath = "");

    /// <summary>
    /// Delete file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>True if file was deleted</returns>
    bool DeleteFile(string sessionId, string relativePath);

    /// <summary>
    /// Delete directory within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <param name="recursive">Whether to delete recursively</param>
    /// <returns>True if directory was deleted</returns>
    bool DeleteDirectory(string sessionId, string relativePath, bool recursive = false);

    /// <summary>
    /// Get file information within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>File information or null if not found</returns>
    FileInfo? GetFileInfo(string sessionId, string relativePath);

    /// <summary>
    /// Get directory information within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="relativePath">Relative path from current directory</param>
    /// <returns>Directory information or null if not found</returns>
    DirectoryInfo? GetDirectoryInfo(string sessionId, string relativePath);

    /// <summary>
    /// Copy file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="sourceRelativePath">Source relative path</param>
    /// <param name="destinationRelativePath">Destination relative path</param>
    /// <param name="overwrite">Whether to overwrite existing file</param>
    /// <returns>Full path of copied file</returns>
    string CopyFile(string sessionId, string sourceRelativePath, string destinationRelativePath, bool overwrite = false);

    /// <summary>
    /// Move file within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="sourceRelativePath">Source relative path</param>
    /// <param name="destinationRelativePath">Destination relative path</param>
    /// <returns>Full path of moved file</returns>
    string MoveFile(string sessionId, string sourceRelativePath, string destinationRelativePath);

    /// <summary>
    /// Get the size of the session directory
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Total size in bytes</returns>
    long GetSessionDirectorySize(string sessionId);

    /// <summary>
    /// Validate that a working directory is within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="workingDirectory">Working directory to validate</param>
    /// <returns>True if directory is within session boundaries</returns>
    bool IsWorkingDirectoryValid(string sessionId, string workingDirectory);

    /// <summary>
    /// Check if the specified path is within session boundaries
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="path">Path to validate</param>
    /// <returns>True if path is within session boundaries</returns>
    bool IsWithinSessionBoundary(string sessionId, string path);

    /// <summary>
    /// Get a safe working directory for external commands - always returns session root
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Safe working directory within session boundaries</returns>
    string GetSafeWorkingDirectory(string sessionId);

    /// <summary>
    /// Clean up session directory
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    void CleanupSession(string sessionId);

    /// <summary>
    /// Clear the entire cache directory (useful for development mode)
    /// </summary>
    void ClearEntireCache();
}
