using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace AAPTForNet.Models
{
    public sealed class Icon
    {
        private const int hdpiWidth = 72;
        public const string DefaultName = "ic_launcher.png";

        internal static readonly Icon Default = new(DefaultName);

        /// <summary>
        /// Return absolute path to package icon if @isImage is true,
        /// otherwise return empty string
        /// </summary>
        public string RealPath { get; set; }

        /// <summary>
        /// Determines whether icon of package is an image
        /// </summary>
        public bool IsImage => !DefaultName.Equals(IconName, StringComparison.Ordinal) && !IsMarkup;

        internal bool IsMarkup => IconName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

        // Not real icon, it refer to another
        internal bool IsReference => IconName.StartsWith("0x");

        /// <summary>
        /// Icon name can be an asset image (real icon image),
        /// markup file (actually it's image, but packed to xml)
        /// or reference to another
        /// </summary>
        internal string IconName { get; set; }

        internal Icon(string? iconName)
        {
            IconName = iconName ?? string.Empty;
            RealPath = "ms-appx:///Assets/256x256.png";
        }

        internal async ValueTask<bool> IsHighDensityAsync()
        {
            if (!IsImage || !File.Exists(RealPath))
            {
                return false;
            }

            try
            {
                // Load from unsupported format will throw an exception.
                // But icon can be packed without extension
                StorageFile file = await StorageFile.GetFileFromPathAsync(RealPath);
                using IRandomAccessStreamWithContentType stream = await file.OpenReadAsync();
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                return decoder.PixelWidth > hdpiWidth;
            }
            catch
            {
                return false;
            }
        }

        public override string ToString() => IconName;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Icon ic && IconName == ic.IconName;

        public override int GetHashCode() => -489061483 + EqualityComparer<string>.Default.GetHashCode(IconName);
    }
}
