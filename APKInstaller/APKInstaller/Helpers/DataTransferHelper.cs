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
            dataPackage.Properties.Title = title;
            dataPackage.Properties.Description = description;
            return dataPackage;
        }

        private static async Task<DataPackage> GetFileDataPackage(string filePath, string fileName, string description)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

            IEnumerable<IStorageFile> files = new List<StorageFile> { file };

            DataPackage dataPackage = new();
            dataPackage.SetStorageItems(files);
            dataPackage.Properties.Title = fileName;
            dataPackage.Properties.Description = description;

            return dataPackage;
        }

        private static DataPackage GetUrlDataPackage(Uri uri, string displayName, string description)
        {
            string htmlFormat = HtmlFormatHelper.CreateHtmlFormat($"<a href='{uri}'>{displayName}</a>");

            DataPackage dataPackage = new();
            dataPackage.SetWebLink(uri);
            dataPackage.SetText(uri.ToString());
            dataPackage.SetHtmlFormat(htmlFormat);
            dataPackage.Properties.Title = displayName;
            dataPackage.Properties.Description = description;

            return dataPackage;
        }

        private static async Task<DataPackage> GetBitmapDataPackage(string bitmapPath, string bitmapName, string description)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(bitmapPath);

            RandomAccessStreamReference bitmap = RandomAccessStreamReference.CreateFromFile(file);

            DataPackage dataPackage = new();
            dataPackage.SetBitmap(bitmap);
            dataPackage.Properties.Title = bitmapName;
            dataPackage.Properties.Description = description;

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
