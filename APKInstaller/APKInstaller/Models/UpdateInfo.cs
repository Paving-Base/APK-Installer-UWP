using System;
using System.Text.Json.Serialization;

namespace APKInstaller.Models
{
    public class UpdateInfo
    {
        [JsonPropertyName("url")]
        public string ApiUrl { get; init; }
        [JsonPropertyName("html_url")]
        public string ReleaseUrl { get; init; }
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; }
        [JsonPropertyName("prerelease")]
        public bool IsPreRelease { get; init; }
        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; init; }
        [JsonPropertyName("assets")]
        public Asset[] Assets { get; init; }
        [JsonPropertyName("body")]
        public string Changelog { get; init; }
        [JsonIgnore]
        public bool IsExistNewVersion { get; set; }
        [JsonIgnore]
        public SystemVersionInfo Version { get; set; }
    }

    public class Asset
    {
        [JsonPropertyName("url")]
        public string Url { get; init; }
        [JsonPropertyName("name")]
        public string Name { get; init; }
        [JsonPropertyName("size")]
        public long Size { get; init; }
        [JsonPropertyName("download_count")]
        public long DownloadCount { get; init; }
        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; init; }
        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; init; }
    }
}
