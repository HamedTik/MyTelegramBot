using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Models
{
    public class MentorPerformanceReport
    {
        public long MentorId { get; set; }
        public int TotalGroups { get; set; } // X
        public int ReviewedGroups { get; set; } // Y
        public double AverageReviewScore { get; set; } // میانگین نمره نظرسنجی
        public int NotSentGroups { get; set; } // Z
        public List<GroupInfo> NotSentGroupLinks { get; set; } = new List<GroupInfo>();
        public int InactiveGroups { get; set; } // Q
        public List<GroupInfo> InactiveGroupLinks { get; set; } = new List<GroupInfo>();
        public DateTime Date { get; set; }
    }
}
