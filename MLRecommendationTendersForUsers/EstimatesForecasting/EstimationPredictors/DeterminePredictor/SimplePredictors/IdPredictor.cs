using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.Models;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors
{
    public class IdPredictor: PredictorBase
    {
        public IdPredictor():base()
        {

        }
        public IdPredictor(string propName):base(propName)
        {

        }
        public override PredictInfo Estimate(EntityEstimation estimate)
        {
            var expectedKey = CreateKey(estimate);
            var selectedKf = EstimateKf.Default();
            var selectedKey = "";
            if(History.TryGetByKey(expectedKey, out HistoryItem item))
            {
                selectedKf = item.EstimateKf;
                selectedKey = expectedKey;
            }

            return new PredictInfo { Kf =  selectedKf, Predictor = this, TargetKey = selectedKey };
        }

        public override bool Filter(EntityEstimation fieldToEstimate)
        {
            var value = TargetProperty.GetValue(fieldToEstimate);
            return value != null && !String.IsNullOrEmpty(value.ToString()) && value.ToString() != "0";
        }

        public override void Load(List<EntityEstimation> estimates)
        {
            //по одному
            for (var i = 0; i < estimates.Count; i++)
            {
                var curEstimates = estimates[i];
                var key = CreateKey(curEstimates);

                History.Add(key, curEstimates.Estimation);
            }
        }
        
        protected string CreateKey(EntityEstimation estimation)
        {
            return TargetProperty.GetValue(estimation).ToString();
        }
    }
}
