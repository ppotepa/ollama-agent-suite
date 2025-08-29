namespace Ollama.Domain.Tools
{
    /// <summary>
    /// Popular development download sources
    /// Used by download tools to specify source type
    /// </summary>
    public enum DownloadSource
    {
        /// <summary>
        /// Automatic detection based on URL
        /// </summary>
        Auto = 0,
        
        /// <summary>
        /// GitHub repositories (github.com)
        /// </summary>
        GitHub = 1,
        
        /// <summary>
        /// GitLab repositories (gitlab.com)
        /// </summary>
        GitLab = 2,
        
        /// <summary>
        /// Bitbucket repositories (bitbucket.org)
        /// </summary>
        Bitbucket = 3,
        
        /// <summary>
        /// Azure DevOps repositories
        /// </summary>
        AzureDevOps = 4,
        
        /// <summary>
        /// NuGet packages (nuget.org)
        /// </summary>
        NuGet = 5,
        
        /// <summary>
        /// NPM packages (npmjs.com)
        /// </summary>
        NPM = 6,
        
        /// <summary>
        /// PyPI packages (pypi.org)
        /// </summary>
        PyPI = 7,
        
        /// <summary>
        /// Generic HTTP/HTTPS download
        /// </summary>
        Http = 8,
        
        /// <summary>
        /// FTP download
        /// </summary>
        Ftp = 9,
        
        /// <summary>
        /// Maven packages
        /// </summary>
        Maven = 10,
        
        /// <summary>
        /// Docker Hub images
        /// </summary>
        DockerHub = 11
    }
    
    /// <summary>
    /// Helper methods for download source operations
    /// </summary>
    public static class DownloadSourceExtensions
    {
        /// <summary>
        /// Detect download source from URL
        /// </summary>
        public static DownloadSource DetectFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return DownloadSource.Auto;

            var lowerUrl = url.ToLowerInvariant();
            
            return lowerUrl switch
            {
                var u when u.Contains("github.com") => DownloadSource.GitHub,
                var u when u.Contains("gitlab.com") => DownloadSource.GitLab,
                var u when u.Contains("bitbucket.org") => DownloadSource.Bitbucket,
                var u when u.Contains("dev.azure.com") || u.Contains("visualstudio.com") => DownloadSource.AzureDevOps,
                var u when u.Contains("nuget.org") => DownloadSource.NuGet,
                var u when u.Contains("npmjs.com") => DownloadSource.NPM,
                var u when u.Contains("pypi.org") => DownloadSource.PyPI,
                var u when u.Contains("maven.org") => DownloadSource.Maven,
                var u when u.Contains("docker.io") || u.Contains("hub.docker.com") => DownloadSource.DockerHub,
                var u when u.StartsWith("ftp://") => DownloadSource.Ftp,
                var u when u.StartsWith("http://") || u.StartsWith("https://") => DownloadSource.Http,
                _ => DownloadSource.Auto
            };
        }
        
        /// <summary>
        /// Get display name for download source
        /// </summary>
        public static string GetDisplayName(this DownloadSource source)
        {
            return source switch
            {
                DownloadSource.GitHub => "GitHub Repository",
                DownloadSource.GitLab => "GitLab Repository", 
                DownloadSource.Bitbucket => "Bitbucket Repository",
                DownloadSource.AzureDevOps => "Azure DevOps Repository",
                DownloadSource.NuGet => "NuGet Package",
                DownloadSource.NPM => "NPM Package",
                DownloadSource.PyPI => "Python Package (PyPI)",
                DownloadSource.Maven => "Maven Package",
                DownloadSource.DockerHub => "Docker Hub Image",
                DownloadSource.Http => "HTTP Download",
                DownloadSource.Ftp => "FTP Download",
                _ => "Auto-Detect"
            };
        }
    }
}
