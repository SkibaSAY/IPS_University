using IPSLib.EstimatesForecasting.EstimateSearcher;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors
{
    public class NumberPredictor : PredictorBase
    {
        private Single roundingAccuracy;
        public NumberPredictor(string propName, Single roundingAccuracy = 100) : base(propName)
        {
            this.roundingAccuracy = roundingAccuracy;
        }

        public override PredictInfo Estimate(Entity estimate)
        {
            var expectedKey = CreateKey(estimate);
            var selectedKf = EstimateKf.Default();
            var selectedKey = "";
            if (History.TryGetByKey(expectedKey, out HistoryItem item))
            {
                selectedKf = item.EstimateKf;
                selectedKey = expectedKey;
            }

            return new PredictInfo { Kf = selectedKf, Predictor = this, TargetKey = selectedKey };
        }

        public override bool Filter(Entity entity)
        {
            var value = GetTargetValue(entity);
            return Filter(value);
        }

        protected virtual bool Filter(object value)
        {
            return (Single)value > 0;
        }

        public override bool Filter(DataFrameRow row)
        {
            var value = GetTargetValue(row);
            return Filter(value);
        }

        public override void LoadRow(DataFrameRow row)
        {
            var key = CreateKey(row);
            History.Add(key, (int)EntityScoreEnum.Application);
        }

        protected virtual string CreateKey(DataFrameRow row)
        {
            return ((int)((Single)GetTargetValue(row) / roundingAccuracy)).ToString();
        }

        protected virtual string CreateKey(Entity entity)
        {
            return ((int)((Single)GetTargetValue(entity) / roundingAccuracy)).ToString();
        }
    }
}
