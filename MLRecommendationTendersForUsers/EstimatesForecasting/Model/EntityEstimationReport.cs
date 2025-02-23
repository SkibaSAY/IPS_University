using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.Model
{
    public class EntityEstimationReport: EntityEstimation
    {
        public EntityEstimationReport() { }
        [DisplayName("Регион")]
        public string RegionName { get; set; }

        public static new void FillEstimationsFromBaseTables(PgClient pgClient, List<EntityEstimationReport> entityEstimations)
        {
            //базовое заполнение
            //EntityEstimation.FillEstimationsFromBaseTables(pgClient,(List<EntityEstimation>)entityEstimations);
            

        }
    }
}
