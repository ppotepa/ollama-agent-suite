using System;

namespace Ollama.Domain.Tools.Attributes
{
    /// <summary>
    /// Defines capabilities that tools can have using a hierarchical enum structure
    /// </summary>
    [Flags]
    public enum ToolCapability : long
    {
        None = 0,
        
        // File Operations (1-999)
        FileRead = 1L << 0,
        FileWrite = 1L << 1,
        FileCreate = 1L << 2,
        FileDelete = 1L << 3,
        FileCopy = 1L << 4,
        FileMove = 1L << 5,
        FileRename = 1L << 6,
        FileAttributes = 1L << 7,
        
        // Directory Operations (1000-1999)
        DirectoryList = 1L << 10,
        DirectoryCreate = 1L << 11,
        DirectoryDelete = 1L << 12,
        DirectoryMove = 1L << 13,
        DirectoryCopy = 1L << 14,
        DirectoryNavigate = 1L << 15,
        
        // Network Operations (2000-2999)
        NetworkDownload = 1L << 20,
        NetworkUpload = 1L << 21,
        NetworkRequest = 1L << 22,
        NetworkAPI = 1L << 23,
        
        // Analysis Operations (3000-3999)
        CodeAnalysis = 1L << 30,
        FileSystemAnalysis = 1L << 31,
        TextAnalysis = 1L << 32,
        DataAnalysis = 1L << 33,
        
        // Mathematical Operations (4000-4999)
        MathCalculation = 1L << 40,
        MathEvaluation = 1L << 41,
        StatisticalAnalysis = 1L << 42,
        
        // System Operations (5000-5999)
        SystemCommand = 1L << 50,
        SystemProcess = 1L << 51,
        SystemEnvironment = 1L << 52,
        
        // Navigation Operations (6000-6999)
        CursorNavigation = 1L << 60,
        CursorLocation = 1L << 61,
        PathResolution = 1L << 62,
        
        // Repository Operations (7000-7999)
        GitHubDownload = 1L << 70,
        GitLabDownload = 1L << 71,
        RepositoryClone = 1L << 72,
        VersionControl = 1L << 73,
        
        // Archive Operations (8000-8999)
        ArchiveExtraction = 1L << 80,
        ArchiveCompression = 1L << 81,
        ZipOperations = 1L << 82,
        
        // Common Capability Groups
        FileOperations = FileRead | FileWrite | FileCreate | FileDelete | FileCopy | FileMove | FileRename | FileAttributes,
        DirectoryOperations = DirectoryList | DirectoryCreate | DirectoryDelete | DirectoryMove | DirectoryCopy | DirectoryNavigate,
        NetworkOperations = NetworkDownload | NetworkUpload | NetworkRequest | NetworkAPI,
        AnalysisOperations = CodeAnalysis | FileSystemAnalysis | TextAnalysis | DataAnalysis,
        NavigationOperations = CursorNavigation | CursorLocation | PathResolution,
        RepositoryOperations = GitHubDownload | GitLabDownload | RepositoryClone | VersionControl,
        ArchiveOperations = ArchiveExtraction | ArchiveCompression | ZipOperations
    }
}
