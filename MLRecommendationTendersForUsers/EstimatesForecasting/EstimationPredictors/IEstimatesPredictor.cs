using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors
{
    public interface IEstimatesPredictor
    {
        public EstimationPredict Predict(EntityEstimation entityEstimation);
        public string GenerateSavePath(long userId, string cacheDirPath);

        public void Learn(List<EntityEstimation> learnindData);
        public TestResult Test(List<EntityEstimation> testData);
        public void Load(string path);
        public void Save(string path);

        public class TestResult
        {
            public double RSquared;
            public override string ToString()
            {
                return RSquared.ToString();
            }
        }
    }
}
