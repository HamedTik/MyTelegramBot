using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Models
{
    public class CapacityRecord
    {
        public long MentorId { get; set; }
        public int MentorProposedCapacity { get; set; }
        public int SupervisorApprovedCapacity { get; set; }
        public int CurrentStudents { get; set; }
    }
}
