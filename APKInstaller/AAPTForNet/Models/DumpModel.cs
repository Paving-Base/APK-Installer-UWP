using System.Collections.Generic;

namespace AAPTForNet.Models
{
    public sealed class DumpModel(string path, bool success, List<string> msg)
    {
        public string FilePath { get; } = path;
        public bool IsSuccess { get; } = success;
        public List<string> Messages { get; } = msg;
    }
}
