using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace APKInstaller.Models
{
    public class UpdateInfo
    {
        [JsonProperty(PropertyName = "url")]
        public string ApiUrl { get; set; }
        [JsonProperty(PropertyName = "html_url")]
        public string ReleaseUrl { get; set; }
        [JsonProperty(PropertyName = "tag_name")]
        public string TagName { get; set; }
        [JsonProperty(PropertyName = "prerelease")]
        public bool IsPreRelease { get; set; }
        [JsonProperty(PropertyName = "created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty(PropertyName = "published_at")]
        public DateTime PublishedAt { get; set; }
        [JsonProperty(PropertyName = "assets")]
        public List<Asset> Assets { get; set; }
        [JsonProperty(PropertyName = "body")]
        public string Changelog { get; set; }
        public bool IsExistNewVersion { get; set; }
    }

    public class Asset
    {
        [JsonProperty(PropertyName = "size")]
        public int Size { get; set; }
        [JsonProperty(PropertyName = "browser_download_url")]
        public string Url { get; set; }
    }
}
