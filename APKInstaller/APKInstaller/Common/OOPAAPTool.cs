using AAPTForNet;
using AAPTForNet.Models;
using APKInstaller.Helpers;
using APKInstaller.Metadata;
using APKInstaller.Projection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace APKInstaller.Common
{
    public class OOPAAPTool : AAPTool
    {
        private ServerManager serverManager = null;
        private ServerManager ServerManager
        {
            get
            {
                if (serverManager?.IsServerRunning == true)
                {
                    return serverManager;
                }
                serverManager = APKInstallerProjectionFactory.ServerManager;
                return serverManager;
            }
        }

        protected override bool HasDumpOverride { get; } = true;

        protected override async Task<DumpModel> DumpByOverrideAsync(
            string path,
            string args,
            DumpTypes type,
            Func<string, int, bool> callback)
        {
            string fileName = Path.Combine(AppPath + @"\Tools\aapt.exe");
            List<string> output = [];    // Messages from output stream
            string arguments = string.Empty;

            switch (type)
            {
                case DumpTypes.Manifest:
                    arguments = $"dump badging \"{path}\"";
                    break;
                case DumpTypes.Resources:
                    arguments = $"dump --values resources \"{path}\"";
                    break;
                case DumpTypes.XmlTree:
                    arguments = $"dump xmltree \"{path}\" {args}";
                    break;
                default:
                    return new DumpModel(path, false, output);
            }

            await ServerManager.DumpAsync(fileName, arguments, callback == null ? null : new DumpDelegate(callback), output, Encoding.UTF8.CodePage);

            // Dump xml tree get only 1 message when failed, the others are 2.
            bool isSuccess =
                type != DumpTypes.XmlTree
                    ? output.Count > 2
                    : output.Count > 0;
            return new DumpModel(path, isSuccess, output);
        }

        protected override async Task<StorageFile> CreateHardLinkAsync(StorageFile file)
        {
            try
            {
                StorageFile example = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("example", CreationCollisionOption.OpenIfExists);
                string path = Path.Combine(TempPath, file.Name);
                ServerManager.CreateFileSymbolic(path, file.Path, example.Path);
                if (File.Exists(path))
                {
                    return await StorageFile.GetFileFromPathAsync(path);
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(OOPAAPTool)).Warn(ex.ExceptionToMessage(), ex);
            }
            return null;
        }
    }
}
