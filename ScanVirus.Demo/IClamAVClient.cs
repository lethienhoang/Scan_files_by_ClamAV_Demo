using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanVirus.Demo
{
    public interface IClamAVClient
    {
        int MaxChunkSize { get; set; }

        long MaxStreamSize { get; set; }

        string Server { get; set; }

        int Port { get; set; }

        Task<bool> PingAsync();

        Task<ClamAVScanResult> SendAndScanFileAsync(Stream sourceStream);

        Task<ClamAVScanResult> SendAndScanFileAsync(Stream sourceStream, CancellationToken cancellationToken);

        Task<ClamAVScanResult> ScanFileOnServerAsync(string filePath);

        Task<ClamAVScanResult> ScanFileOnServerAsync(string filePath, CancellationToken cancellationToken);

        Task<ClamAVScanResult> ScanFileOnServerMultithreadedAsync(string filePath);

        Task<ClamAVScanResult> ScanFileOnServerMultithreadedAsync(string filePath, CancellationToken cancellationToken);
    }
}
