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
        
        // Backend Development - Code Generation (9000-9999)
        BackendCodeGeneration = 1L << 90,
        ControllerGeneration = 1L << 91,
        ServiceLayerGeneration = 1L << 92,
        ModelGeneration = 1L << 93,
        RepositoryGeneration = 1L << 94,
        MiddlewareGeneration = 1L << 95,
        ConfigurationGeneration = 1L << 96,
        APIDocumentationGeneration = 1L << 97,
        TestGeneration = 1L << 98,
        DTOGeneration = 1L << 99,
        
        // Database Development (10000-10999)
        EntityFrameworkGeneration = 1L << 100,
        MigrationGeneration = 1L << 101,
        DbContextGeneration = 1L << 102,
        SeedDataGeneration = 1L << 103,
        QueryGeneration = 1L << 104,
        DatabaseSchemaGeneration = 1L << 105,
        StoredProcedureGeneration = 1L << 106,
        IndexGeneration = 1L << 107,
        ViewGeneration = 1L << 108,
        TriggerGeneration = 1L << 109,
        
        // DevOps & Infrastructure (11000-11999)
        DockerfileGeneration = 1L << 110,
        KubernetesManifestGeneration = 1L << 111,
        CIPipelineGeneration = 1L << 112,
        EnvironmentConfiguration = 1L << 113,
        LoggingConfiguration = 1L << 114,
        SecurityConfiguration = 1L << 115,
        MonitoringConfiguration = 1L << 116,
        DeploymentScriptGeneration = 1L << 117,
        HealthCheckGeneration = 1L << 118,
        BackupScriptGeneration = 1L << 119,
        
        // Web API Specific (12000-12999)
        RESTAPIGeneration = 1L << 120,
        GraphQLGeneration = 1L << 121,
        MinimalAPIGeneration = 1L << 122,
        WebSocketGeneration = 1L << 123,
        APIVersioningSetup = 1L << 124,
        SwaggerGeneration = 1L << 125,
        CORSConfiguration = 1L << 126,
        RateLimitingSetup = 1L << 127,
        AuthenticationSetup = 1L << 128,
        AuthorizationSetup = 1L << 129,
        
        // Testing Infrastructure (13000-13999)
        UnitTestGeneration = 1L << 130,
        IntegrationTestGeneration = 1L << 131,
        PerformanceTestGeneration = 1L << 132,
        MockGeneration = 1L << 133,
        TestDataGeneration = 1L << 134,
        TestDatabaseSetup = 1L << 135,
        EndToEndTestGeneration = 1L << 136,
        LoadTestGeneration = 1L << 137,
        TestReportGeneration = 1L << 138,
        
        // Architecture Patterns (14000-14999)
        CQRSPatternGeneration = 1L << 140,
        MediatorPatternGeneration = 1L << 141,
        RepositoryPatternGeneration = 1L << 142,
        UnitOfWorkPatternGeneration = 1L << 143,
        FactoryPatternGeneration = 1L << 144,
        ObserverPatternGeneration = 1L << 145,
        StrategyPatternGeneration = 1L << 146,
        AdapterPatternGeneration = 1L << 147,
        DecoratorPatternGeneration = 1L << 148,
        
        // Microservices & Distributed Systems (15000-15999)
        MicroserviceGeneration = 1L << 150,
        ServiceMeshConfiguration = 1L << 151,
        MessageQueueSetup = 1L << 152,
        EventSourcingSetup = 1L << 153,
        DistributedCacheConfiguration = 1L << 154,
        ServiceDiscoverySetup = 1L << 155,
        CircuitBreakerImplementation = 1L << 156,
        RetryPolicyConfiguration = 1L << 157,
        BulkheadPatternImplementation = 1L << 158,
        
        // Common Capability Groups
        FileOperations = FileRead | FileWrite | FileCreate | FileDelete | FileCopy | FileMove | FileRename | FileAttributes,
        DirectoryOperations = DirectoryList | DirectoryCreate | DirectoryDelete | DirectoryMove | DirectoryCopy | DirectoryNavigate,
        NetworkOperations = NetworkDownload | NetworkUpload | NetworkRequest | NetworkAPI,
        AnalysisOperations = CodeAnalysis | FileSystemAnalysis | TextAnalysis | DataAnalysis,
        NavigationOperations = CursorNavigation | CursorLocation | PathResolution,
        RepositoryOperations = GitHubDownload | GitLabDownload | RepositoryClone | VersionControl,
        ArchiveOperations = ArchiveExtraction | ArchiveCompression | ZipOperations,
        
        // Backend Development Groups
        BackendDevelopment = BackendCodeGeneration | ControllerGeneration | ServiceLayerGeneration | ModelGeneration | RepositoryGeneration | MiddlewareGeneration,
        DatabaseDevelopment = EntityFrameworkGeneration | MigrationGeneration | DbContextGeneration | SeedDataGeneration | QueryGeneration | DatabaseSchemaGeneration,
        DevOpsDevelopment = DockerfileGeneration | KubernetesManifestGeneration | CIPipelineGeneration | EnvironmentConfiguration | DeploymentScriptGeneration,
        WebAPIDevelopment = RESTAPIGeneration | GraphQLGeneration | MinimalAPIGeneration | SwaggerGeneration | APIVersioningSetup | CORSConfiguration,
        TestingInfrastructure = UnitTestGeneration | IntegrationTestGeneration | PerformanceTestGeneration | MockGeneration | TestDataGeneration,
        ArchitecturalPatterns = CQRSPatternGeneration | MediatorPatternGeneration | RepositoryPatternGeneration | UnitOfWorkPatternGeneration,
        MicroservicesDevelopment = MicroserviceGeneration | ServiceMeshConfiguration | MessageQueueSetup | EventSourcingSetup | DistributedCacheConfiguration,
        SecurityDevelopment = AuthenticationSetup | AuthorizationSetup | SecurityConfiguration | RateLimitingSetup,
        ConfigurationManagement = ConfigurationGeneration | EnvironmentConfiguration | LoggingConfiguration | MonitoringConfiguration
    }
}
