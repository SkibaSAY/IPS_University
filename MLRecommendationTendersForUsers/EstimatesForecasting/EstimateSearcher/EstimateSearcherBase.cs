using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher
{
    public abstract class EstimateSearcherBase
    {
        protected PgClient PostgreClient;
        public EstimateSearcherBase()
        {

        }
        public EstimateSearcherBase(PgClient postgreClient)
        {
            PostgreClient = postgreClient;
        }

        public abstract List<EntityEstimation> FindEstimations(long userId);

        /// <summary>
        /// Заполняет служебную информацию из таблицы tenders
        /// Метод требовался бы во многих наследниках
        /// </summary>
        /// <param name="entityEstimation"></param>
        /// <exception cref="Exception"></exception>
        protected virtual void FillEstimationsFromBaseTables(List<EntityEstimation> entityEstimations)
        {
            EntityEstimation.FillEstimationsFromBaseTables(PostgreClient, entityEstimations);
        }
    }
}
