using System;
using System.Collections.Generic;
using System.Text;

namespace ScanVirus.Demo
{
    public enum ClamScanResultType
    {
        Unknown = 0,
        Clean,
        VirusDetected,
        Error
    }
}
