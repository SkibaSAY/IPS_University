using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher
{
    public class EstimationsByApplication : EstimateSearcherBase
    {
        public EstimationsByApplication(PgClient postgreClient) : base(postgreClient)
        {

        }

        /// <summary>
        /// Попытка привязать пользователя к организации
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Список организаций, к которым может быть привязан пользователь</returns>
        protected bool TryToBindOrganization(long userId, out List<long> organizationIds)
        {
            organizationIds = new List<long>();

            var queryFindUser = $@"SELECT inn FROM system.user_organization WHERE user_id = @userId AND is_main";
            var inns = PostgreClient.Select<string>(
                query: queryFindUser,
                param: new { userId = userId }).ToList();

            if (inns.Count == 0)
            {
                return false;
            }
            var queryFindOrganizations = $@"
                SELECT o.id
                FROM organizations.organizations o
                WHERE o.inn IN({String.Join(',',inns.Select(inn=>$"'{inn}'"))})";

            organizationIds = PostgreClient.Select<long>(queryFindOrganizations).ToList();
            if (organizationIds.Count == 0)
            {
                return false;
            }

            return true;
        }
        public override List<EntityEstimation> FindEstimations(long userId)
        {
            var estimationsByApplications = new List<EntityEstimation>();

            //Пользователь может быть филиалом компании или головным офисом,
            //участвовать может тоже по разному(сам или через головной офис), потому мы берём все организации, привязанные к пользователю
            var success = TryToBindOrganization(userId, out List<long> organizationIds);
            if (!success)
            {
                return estimationsByApplications;
            }

            //группировка нужна, чтобы для одного тендера получить одну оценку - мы смотрим несколько организаций же
            var query = $@"
            SELECT 
	            a.tender_id AS TenderId,
                l.id AS LotId
            FROM tenders.applications a 
            JOIN tenders.lots l ON a.tender_id = l.tender_id
            WHERE a.organization_id IN({String.Join(',',organizationIds)})
            GROUP BY a.tender_id, l.id";

            estimationsByApplications = PostgreClient.Select<EntityEstimation>(query).ToList();

            estimationsByApplications.ForEach(entityEstimation =>
            {
                entityEstimation.UserId = userId;
                entityEstimation.Estimation = (int)EstimateScoreEnum.Application;
            });

            //FillEstimationsFromBaseTables(estimationsByApplications);

            return estimationsByApplications;
        }
    }
}
