using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikBot.Models
{
    public class Ticket
    {
        public long StudentId { get; set; }
        public long GroupId { get; set; }
        public DateTime IssuedAt { get; set; }
    }
}
