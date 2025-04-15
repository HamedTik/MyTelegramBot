using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Models
{
    public class PollResponse
    {
        public long StudentId { get; set; }
        public long GroupId { get; set; }
        public DateTime RespondedAt { get; set; }
        public PollType Type { get; set; } // ارجاع به PollType.cs
        public int? Score { get; set; } // For poll scores (1-10)
    }
}
