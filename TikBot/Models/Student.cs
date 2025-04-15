using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Models
{
    public class Student
    {
        public long UserId { get; set; }
        public string FirstName { get; set; }
        public long GroupId { get; set; }
        public long MentorId { get; set; } // New
        public DateTime LastMessageAt { get; set; }
    }
}
