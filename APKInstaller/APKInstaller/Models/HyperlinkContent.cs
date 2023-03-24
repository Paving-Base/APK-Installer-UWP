using System;

namespace APKInstaller.Models
{
    public class HyperlinkContent
    {
        public string Content { get; set; }
        public Uri NavigateUri { get; set; }

        public HyperlinkContent(string content, Uri navigateUri)
        {
            Content = content;
            NavigateUri = navigateUri;
        }

        public void Deconstruct(out string content, out Uri navigateUri)
        {
            content = Content;
            navigateUri = NavigateUri;
        }
    }
}
