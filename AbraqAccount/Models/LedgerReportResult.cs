using System;
using System.Collections.Generic;

namespace AbraqAccount.Models
{
    public class LedgerReportResult
    {
        public decimal OpeningBalance { get; set; }
        public List<LedgerEntryViewModel> Entries { get; set; } = new();
        public decimal ClosingBalance { get; set; }
    }
}
