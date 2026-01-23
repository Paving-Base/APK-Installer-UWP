using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf.Common;
using Zeroconf.DNS;
using Zeroconf.Interfaces;
using Zeroconf.Models;

namespace Zeroconf
{
    /// <summary>
    /// Looks for ZeroConf devices
    /// </summary>
    public static partial class ZeroconfResolver
    {
        private static readonly AsyncLock ResolverLock = new();
        private static readonly INetworkInterface NetworkInterface = new NetworkInterface();

        private static IEnumerable<string> BrowseResponseParser(Response response) =>
            response.RecordsPTR.Select(ptr => ptr.PTRDNAME);

        private static async ValueTask<IDictionary<string, Response>> ResolveInternal(
            ZeroconfOptions options,
            Action<string, Response>? callback,
            CancellationToken cancellationToken,
            params System.Net.NetworkInformation.NetworkInterface[]? netInterfacesToSendRequestOn)
        {
            byte[] requestBytes = GetRequestBytes(options);

            using IDisposable disposable = options.AllowOverlappedQueries
                ? new NullDisposable()
                : await ResolverLock.LockAsync();

            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<string, Response> dict = [];

            void Converter(IPAddress address, byte[] buffer)
            {
                Response resp = new(buffer);
                RecordPTR? firstPtr = resp.RecordsPTR.FirstOrDefault();
                string name = firstPtr?.PTRDNAME.Split('.')[0] ?? string.Empty;
                string addrString = address.ToString();

                Debug.WriteLine("IP: {0}, {1}Bytes: {2}, IsResponse: {3}", addrString, new Stringable(() => string.IsNullOrEmpty(name) ? string.Empty : $"Name: {name}, "), buffer.Length, resp.header.QR);

                if (resp.header.QR)
                {
                    string key = $"{addrString}{(string.IsNullOrEmpty(name) ? "" : $": {name}")}";
                    lock (dict)
                    {
                        dict[key] = resp;
                    }

                    callback?.Invoke(key, resp);
                }
            }

            Debug.WriteLine("Looking for {0} with scantime {1}", new Stringable(() => string.Join(", ", options.Protocols)), options.ScanTime);

            await NetworkInterface.NetworkRequestAsync(
                requestBytes,
                options.ScanTime,
                options.Retries,
                (int)options.RetryDelay.TotalMilliseconds,
                Converter,
                cancellationToken,
                netInterfacesToSendRequestOn).ConfigureAwait(false);

            return dict;
        }

        private static byte[] GetRequestBytes(ZeroconfOptions options)
        {
            Request req = new();
            QType queryType = options.ScanQueryType == ScanQueryType.Ptr ? QType.PTR : QType.ANY;
            req.AddQuestions(options.Protocols.Select(protocol => new Question(protocol, queryType, QClass.IN)));
            return req.Data;
        }

        private static ZeroconfHost ResponseToZeroconf(Response response, string remoteAddress, ResolveOptions? options)
        {
            IEnumerable<string> ipv4Adresses = response.Answers
                .Select(r => r.RECORD)
                .OfType<RecordA>()
                .Concat(
                    response.Additionals
                    .Select(r => r.RECORD)
                    .OfType<RecordA>())
                .Select(aRecord => aRecord.Address)
                .Distinct();

            IEnumerable<string> ipv6Adresses = response.Answers
                .Select(r => r.RECORD)
                .OfType<RecordAAAA>()
                .Concat(
                    response.Additionals
                    .Select(r => r.RECORD)
                    .OfType<RecordAAAA>())
                .Select(aRecord => aRecord.Address)
                .Distinct();

            IReadOnlyList<string> adresses = [.. ipv4Adresses, .. ipv6Adresses];
            ZeroconfHost z = new()
            {
                Id = adresses is [string value, ..] ? value : remoteAddress,
                IPAddresses = adresses
            };

            bool dispNameSet = false;

            foreach (RecordPTR ptrRec in response.RecordsPTR)
            {
                // set the display name if needed
                if (!dispNameSet
                    && (options == null
                        || (options != null
                            && options.Protocols.Contains(ptrRec.RR.NAME))))
                {
                    z.DisplayName = ptrRec.PTRDNAME.Replace($".{ptrRec.RR.NAME}", "");
                    dispNameSet = true;
                }

                // Get the matching service records
                Record[] responseRecords = [.. response.RecordsRR
                    .Where(r => r.NAME == ptrRec.PTRDNAME)
                    .Select(r => r.RECORD)];

                RecordSRV? srvRec = responseRecords.OfType<RecordSRV>().FirstOrDefault();
                if (srvRec == null)
                {
                    continue; // Missing the SRV record, not valid
                }

                Service svc = new()
                {
                    Name = ptrRec.RR.NAME,
                    ServiceName = srvRec.RR.NAME,
                    Port = srvRec.PORT,
                    Ttl = (int)srvRec.RR.TTL,
                };

                // There may be 0 or more text records - property sets
                foreach (RecordTXT txtRec in responseRecords.OfType<RecordTXT>())
                {
                    Dictionary<string, string?> set = [];
                    foreach (string txt in txtRec.TXT)
                    {
                        string[] split = txt.Split(['='], 2);
                        if (split.Length == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(split[0]))
                            {
                                set[split[0]] = null;
                            }
                        }
                        else
                        {
                            set[split[0]] = split[1];
                        }
                    }
                    svc.AddPropertySet(set);
                }
                z.AddService(svc);
            }
            return z;
        }
    }
}
