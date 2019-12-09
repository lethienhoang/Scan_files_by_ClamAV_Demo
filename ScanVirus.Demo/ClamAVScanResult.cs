using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ScanVirus.Demo
{
    public class ClamAVScanResult
    {
        public string RawResult { get; private set; }

        public ClamScanResultType Result { get; private set; }

        public ReadOnlyCollection<ClamScanInfectedFile> InfectedFiles { get; private set; }

        public ClamAVScanResult(string rawResult)
        {
            RawResult = rawResult;

            var resultLowered = rawResult.ToLowerInvariant();

            if (resultLowered.EndsWith("ok"))
            {
                Result = ClamScanResultType.Clean;
            }
            else if (resultLowered.EndsWith("error"))
            {
                Result = ClamScanResultType.Error;
            }
            else if (resultLowered.EndsWith("found"))
            {
                Result = ClamScanResultType.VirusDetected;

                var files = rawResult.Split(new[] { "FOUND" }, StringSplitOptions.RemoveEmptyEntries);
                var infectedFiles = new List<ClamScanInfectedFile>();
                foreach (var file in files)
                {
                    var trimFile = file.Trim();

                    infectedFiles.Add(new ClamScanInfectedFile() { FileName = before(trimFile), VirusName = after(trimFile) });
                }

                InfectedFiles = new ReadOnlyCollection<ClamScanInfectedFile>(infectedFiles);
            }
        }

        public static string before(string s)
        {
            int l = s.LastIndexOf(":");
            if (l > 0)
            {
                return s.Substring(0, l);
            }
            return "";
        }
        public static string after(string s)
        {
            int l = s.LastIndexOf(" ");
            if (l > 0)
            {
                return s.Substring(l);
            }
            return "";
        }

        public override string ToString()
        {
            return RawResult;
        }
    }
}
