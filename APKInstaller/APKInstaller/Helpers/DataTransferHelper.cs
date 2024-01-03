using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace APKInstaller.Helpers
{
    /// <summary>
    /// Class providing functionality to support generating and copying protocol activation URIs.
    /// </summary>
    public static class DataTransferHelper
    {
        private static DataPackage GetTextDataPackage(string text, string title, string description)
        {
            DataPackage dataPackage = new();
            dataPackage.SetText(text);
            if (title != null) { dataPackage.Properties.Title = title; }
            if (description != null) { dataPackage.Properties.Description = description; }
            return dataPackage;
        }

        private static async Task<DataPackage> GetFileDataPackage(string filePath, string fileName, string description)
        {
            StorageFile file = await (filePath.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) && filePath.TryGetUri(out Uri uri)
                ? StorageFile.GetFileFromApplicationUriAsync(uri)
                : StorageFile.GetFileFromPathAsync(filePath));
            IEnumerable<IStorageFile> files = [file];

            DataPackage dataPackage = new();
            dataPackage.SetStorageItems(files);
            if (fileName != null) { dataPackage.Properties.Title = fileName; }
            if (description != null) { dataPackage.Properties.Description = description; }

            return dataPackage;
        }

        private static DataPackage GetUrlDataPackage(Uri uri, string displayName, string description)
        {
            string htmlFormat = HtmlFormatHelper.CreateHtmlFormat($"<a href='{uri}'>{displayName}</a>");

            DataPackage dataPackage = new();
            dataPackage.SetWebLink(uri);
            dataPackage.SetText(uri.ToString());
            dataPackage.SetHtmlFormat(htmlFormat);
            if (displayName != null) { dataPackage.Properties.Title = displayName; }
            if (description != null) { dataPackage.Properties.Description = description; }

            return dataPackage;
        }

        private static async Task<DataPackage> GetBitmapDataPackage(string bitmapPath, string bitmapName, string description)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(bitmapPath);
            RandomAccessStreamReference bitmap = RandomAccessStreamReference.CreateFromFile(file);

            DataPackage dataPackage = new();
            dataPackage.SetBitmap(bitmap);
            if (bitmapName != null) { dataPackage.Properties.Title = bitmapName; }
            if (description != null) { dataPackage.Properties.Description = description; }

            return dataPackage;
        }

        public static void Copy(this DataPackage dataPackage) => Clipboard.SetContentWithOptions(dataPackage, null);

        public static void CopyText(string text, string title, string description = null)
        {
            DataPackage dataPackage = GetTextDataPackage(text, title, description);
            dataPackage.Copy();
        }

        public static async void CopyFile(string filePath, string fileName, string description = null)
        {
            DataPackage dataPackage = await GetFileDataPackage(filePath, fileName, description);
            dataPackage.Copy();
        }

        public static async void CopyBitmap(string bitmapPath, string bitmapName, string description = null)
        {
            DataPackage dataPackage = await GetFileDataPackage(bitmapPath, bitmapName, description);
            dataPackage.Copy();
        }

        public static void Share(this DataPackage dataPackage)
        {
            if (DataTransferManager.IsSupported())
            {
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += (sender, args) => args.Request.Data = dataPackage;
                // Show the Share UI
                DataTransferManager.ShowShareUI();
            }
        }

        public static void ShareURL(Uri url, string displayName, string description = null)
        {
            DataPackage dataPackage = GetUrlDataPackage(url, displayName, description);
            dataPackage.Share();
        }

        public static async void ShareFile(string filePath, string fileName, string description = null)
        {
            DataPackage dataPackage = await GetFileDataPackage(filePath, fileName, description);
            dataPackage.Share();
        }
    }
}
