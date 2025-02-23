using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.Model
{
    public class UserTenderRecommendation
    {
        public UserTenderRecommendation() { }
        public long UserId { get; set; }
        public long TenderId { get; set; }
        public long LotId { get; set; }
        public float Estimation { get; set; }
        public RecommendationStatus Status {  get; set; }
    }
    public enum RecommendationStatus
    {
        Success = 0,
        Error = 1,
        BatchError = 2
    }
}
