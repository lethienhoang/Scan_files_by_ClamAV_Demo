using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanVirus.Demo
{
    public class ClamAVClient : IClamAVClient
    {
        public int MaxChunkSize { get; set ; }
        public long MaxStreamSize { get; set ; }
        public string Server { get; set; }
        public int Port { get; set; }

        public ClamAVClient(string server, int port = 3310)
        {
            MaxChunkSize = 131072; //128k
            MaxStreamSize = 26214400; //25mb
            Server = server;
            Port = port;
        }

        public async Task<bool> PingAsync()
        {
            var result = await ExecuteClamAVCommandAsync("PING", CancellationToken.None);
            return result.ToLowerInvariant() == "pong";
        }

        public async Task<ClamAVScanResult> ScanFileOnServerAsync(string filePath)
        {
            return await ExecuteScanFileOnServerAsync("SCAN ", filePath, CancellationToken.None);            
        }

        public async Task<ClamAVScanResult> ScanFileOnServerAsync(string filePath, CancellationToken cancellationToken)
        {
            return await ExecuteScanFileOnServerAsync("SCAN ", filePath, cancellationToken);
        }

        public async Task<ClamAVScanResult> ScanFileOnServerMultithreadedAsync(string filePath)
        {
            return await ExecuteScanFileOnServerAsync("MULTISCAN ", filePath, CancellationToken.None);
        }

        public async Task<ClamAVScanResult> ScanFileOnServerMultithreadedAsync(string filePath, CancellationToken cancellationToken)
        {
            return await ExecuteScanFileOnServerAsync("MULTISCAN ", filePath, cancellationToken);
        }

        public async Task<ClamAVScanResult> SendAndScanFileAsync(Stream sourceStream)
        {
            return await ExecuteScanFileStreamOnServerAsync(sourceStream, CancellationToken.None);

        }

        public async Task<ClamAVScanResult> SendAndScanFileAsync(Stream sourceStream, CancellationToken cancellationToken)
        {
            return await ExecuteScanFileStreamOnServerAsync(sourceStream, cancellationToken);
        }

        #region Helper        
        private async Task<ClamAVScanResult> ExecuteScanFileOnServerAsync(string command, string filePath, CancellationToken cancellationToken)
        {
            return new ClamAVScanResult(await ExecuteClamAVCommandAsync(String.Format("{0} {1}", command, filePath), cancellationToken));
        }

        private async Task<ClamAVScanResult> ExecuteScanFileStreamOnServerAsync(Stream sourceStream, CancellationToken cancellationToken)
        {
            return new ClamAVScanResult(await ExecuteClamAVCommandAsync("INSTREAM", cancellationToken, (stream, token) =>  SendStreamFileChunksAsync(sourceStream, stream, token)));
        }

        /// <summary>
        /// Helper to connects to the ClamAV server, excuted the command and return the results
        /// </summary>
        /// <param name="command">The command to execute on the ClamAV Server</param>
        /// <param name="cancellationToken">Cancellation token used in requests</param>
        /// <param name="additionalCommand">Action to define additional server communications.  Executed after the command is sent and before the response is read.</param>
        /// <returns></returns>
        private async Task<string> ExecuteClamAVCommandAsync(string command, CancellationToken cancellationToken, Func<NetworkStream, CancellationToken, Task> additionalCommand = null)
        {
            var clamav = new TcpClient();
            string result = string.Empty;
            
            try
            {
                await clamav.ConnectAsync(Server, Port);

                using(var stream = clamav.GetStream())
                {
                    var commandText = String.Format("z{0}\0", command);
                    var commandBytes = Encoding.UTF8.GetBytes(commandText);
                    await stream.WriteAsync(commandBytes, 0, commandBytes.Length, cancellationToken);

                    if (additionalCommand != null)
                    {
                        await additionalCommand(stream, cancellationToken);
                    }

                    using(var reader = new StreamReader(stream))
                    {
                        result = await reader.ReadToEndAsync().ConfigureAwait(false);

                        if (!String.IsNullOrEmpty(result))
                        {
                            result = result.TrimEnd('\0');
                        }
                    }
                }
            }
            finally
            {
                if (clamav.Connected)
                {
                    clamav.Close();
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStream">The stream to send to the ClamAV server</param>
        /// <param name="clamStream">The communication channel to the ClamAV server.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task SendStreamFileChunksAsync(Stream sourceStream, Stream clamStream, CancellationToken cancellationToken)
        {
            var size = MaxChunkSize;
            var bytes = new byte[size];

            while ((size = await sourceStream.ReadAsync(bytes, 0, size, cancellationToken)) > 0)
            {
                if (sourceStream.Position > MaxStreamSize)
                {
                    throw new ArgumentException((String.Format("The maximum stream size of {0} bytes has been exceeded.", MaxStreamSize)));
                }

                var sizeBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(size));
                await clamStream.WriteAsync(sizeBytes, 0, sizeBytes.Length, cancellationToken);
                await clamStream.WriteAsync(bytes, 0, size, cancellationToken);
            }

            var newMessage = BitConverter.GetBytes(0);
            await clamStream.WriteAsync(newMessage, 0, newMessage.Length, cancellationToken);
        }
        #endregion
    }
}
