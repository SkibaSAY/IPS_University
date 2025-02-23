using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher
{
    public class EstimationsFamous : EstimateSearcherBase
    {
        public EstimationsFamous(PgClient postgreClient) : base(postgreClient)
        {
        }

        public override List<EntityEstimation> FindEstimations(long userId)
        {
            var query = $@"SELECT em.entity_id AS TenderId, l.id AS LotId             
                        FROM system.entity_marks em
                        JOIN tenders.lots l ON em.entity_id = l.tender_id
                        WHERE em.user_parent_id = {userId}";

            var rows = PostgreClient.Select<EntityEstimation>(query)
                .ToList();
            foreach (var item in rows)
            {
                item.Estimation = (int)EstimateScoreEnum.Famous;
                item.UserId = userId;
            }

            //FillEstimationsFromBaseTables(rows);
            return rows;
        }
    }
}
