using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Logs;
using APKInstaller.Projection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APKInstaller.Common
{
    public class AdbCommandClient(string adbPath, bool isForce = false, ILogger<AdbCommandLineClient> logger = null) : AdbCommandLineClient(adbPath, isForce, logger)
    {
        protected override int RunProcess(string filename, string command, ICollection<string> errorOutput, ICollection<string> standardOutput) =>
            (int)APKInstallerProjectionFactory.ServerManager.RunProcess(filename, command, AsVector(errorOutput), AsVector(standardOutput));

        protected override Task<int> RunProcessAsync(string filename, string command, ICollection<string> errorOutput, ICollection<string> standardOutput, CancellationToken cancellationToken = default) =>
            APKInstallerProjectionFactory.ServerManager.RunProcessAsync(filename, command, AsVector(errorOutput), AsVector(standardOutput)).AsTask(cancellationToken).ContinueWith(x => (int)x.Result);

        private IList<T> AsVector<T>(ICollection<T> collection) => collection switch
        {
            IList<T> list => list,
            not null => new CollectionVector<T>(collection),
            _ => null,
        };

        [EditorBrowsable(EditorBrowsableState.Never)]
        private readonly struct CollectionVector<T>(ICollection<T> values) : IList<T>
        {
            T IList<T>.this[int index]
            {
                get => values.ElementAt(index);
                set => throw new NotImplementedException();
            }

            public int Count => values.Count;

            public bool IsReadOnly => values.IsReadOnly;

            public void Add(T item) => values.Add(item);

            public void Clear() => values.Clear();

            public bool Contains(T item) => values.Contains(item);

            public void CopyTo(T[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);

            public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

            int IList<T>.IndexOf(T item) => throw new NotImplementedException();

            void IList<T>.Insert(int index, T item) => throw new NotImplementedException();

            public bool Remove(T item) => values.Remove(item);

            void IList<T>.RemoveAt(int index) => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)values).GetEnumerator();
        }
    }
}
