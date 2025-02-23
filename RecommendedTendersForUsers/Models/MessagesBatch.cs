using RecommendedTendersForUsers.Models.Rabbit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendedTendersForUsers.Models
{
    public class MessagesBatch
    {
        public MessagesBatch()
        {
            
        }
        public List<long> TenderIds { get; set; }
    }
}
