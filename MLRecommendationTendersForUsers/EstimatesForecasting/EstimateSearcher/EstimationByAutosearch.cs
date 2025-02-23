using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher
{
    public class EstimationByAutosearch : EstimateSearcherBase
    {
        public EstimationByAutosearch(PgClient postgreClient) : base(postgreClient)
        {

        }

        public override List<EntityEstimation> FindEstimations(long userId)
        {
            //иначе очень много данных попадает и запрос заполнения слишком долгий
            var query = $"SELECT d.tender_id AS TenderId FROM search.distribution d WHERE d.user_id = @userId AND d.tender_id <> 0 ORDER BY d.sys_create_date DESC LIMIT 100000";
            var estimates = PostgreClient.Select<EntityEstimation>(query, new { userId = userId }).ToList();

            foreach(var estimation in estimates)
            {
                estimation.UserId = userId;
                estimation.Estimation = (int)EstimateScoreEnum.AutoSearch;
            }

            //FillEstimationsFromBaseTables(estimates);
            return estimates;
        }
    }
}
