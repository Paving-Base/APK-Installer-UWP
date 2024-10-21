using System;
using System.Text.Json.Serialization;

namespace APKInstaller.Models
{
    public class UpdateInfo
    {
        [JsonPropertyName("url")]
        public string ApiUrl { get; set; }
        [JsonPropertyName("html_url")]
        public string ReleaseUrl { get; set; }
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
        [JsonPropertyName("prerelease")]
        public bool IsPreRelease { get; set; }
        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; set; }
        [JsonPropertyName("assets")]
        public Asset[] Assets { get; set; }
        [JsonPropertyName("body")]
        public string Changelog { get; set; }
        [JsonIgnore]
        public bool IsExistNewVersion { get; set; }
        [JsonIgnore]
        public SystemVersionInfo Version { get; set; }
    }

    public class Asset
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("size")]
        public int Size { get; set; }
        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }
        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; }
    }
}
