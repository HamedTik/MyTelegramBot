using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Utilities
{
    public class StatusManager
    {
        private readonly ToolStripStatusLabel _statusLabel;

        public StatusManager(ToolStripStatusLabel statusLabel)
        {
            _statusLabel = statusLabel;
            UpdateStatus("Offline");
        }

        public void UpdateStatus(string status)
        {
            _statusLabel.Text = $"Status: {status}";
        }
    }
}
