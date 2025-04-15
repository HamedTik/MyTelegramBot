using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Models
{
    public class InviteRecord
    {
        public long MentorId { get; set; }
        public long GroupId { get; set; }
        public DateTime InvitedAt { get; set; }
    }
}
