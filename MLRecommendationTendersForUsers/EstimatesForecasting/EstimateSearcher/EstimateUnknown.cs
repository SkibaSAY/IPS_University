using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher
{
    public class EstimateUnknown : EstimateSearcherBase
    {
        public EstimateUnknown(PgClient postgreClient) : base(postgreClient)
        {
        }

        public override List<EntityEstimation> FindEstimations(long userId)
        {
            return FindEstimations(userId, knownEstimates: null, limit: 10000);
        }

        /// <summary>
        /// Принимает уже известные результаты с целью отобрать неизвестные
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="knownEstimates"></param>
        /// <returns></returns>
        public  List<EntityEstimation> FindEstimations(long userId,IEnumerable<EntityEstimation> knownEstimates, long limit)
        {
            if (knownEstimates.Count() == 0) return new List<EntityEstimation>();

            //var usedCategories = knownEstimates.Where(k=>k.Categories!= null).SelectMany(k => k.Categories.Split(',',StringSplitOptions.RemoveEmptyEntries)).Distinct().ToList();
            //var usedKtru = knownEstimates.Where(k => k.ProductsKtru != null).SelectMany(k => k.ProductsKtru.Split(',', StringSplitOptions.RemoveEmptyEntries)).Distinct().ToList();
            //var usedOkpd = knownEstimates.Where(k=>k.OkpdPath != null).Select(k => k.OkpdPath).Distinct().ToArray();
            var usedKtru = knownEstimates.Where(k=>k.ProductsKtru != null).SelectMany(k => k.ProductsKtru).Distinct().ToArray();

            //{(usedOkpd.Count() > 0 ?  $"AND oc.okpd NOT IN ({String.Join(',',usedOkpd.Select(o=>$"'{o}'"))})": "")}
            var query = $@"SELECT DISTINCT(tc.tender_id) AS TenderId
            FROM tenders.tender_categories tc JOIN dictionaries.okpd2_category oc ON tc.category_id = oc.category_id LEFT JOIN tenders.products p ON tc.tender_id = p.tender_id
            WHERE tc.tender_id NOT IN({System.String.Join(',',knownEstimates.Select(e=>e.TenderId))})
            {(usedKtru.Count() > 0 ? $"AND p.ktru_id NOT IN ({String.Join(',', usedKtru)})" : "")}
            ORDER BY TenderId DESC
            LIMIT {limit}";

            var estimates = PostgreClient.Select<EntityEstimation>(query).ToList();

            estimates.ForEach(e =>
            {
                e.UserId = userId;
                e.Estimation = (int)EstimateScoreEnum.Unknown;
            });
            
            //Удалить, тк Fill можно выполнить один раз для всех данных со всех Searcher
            //FillEstimationsFromBaseTables(estimates);
            return estimates;
        }
    }
}
