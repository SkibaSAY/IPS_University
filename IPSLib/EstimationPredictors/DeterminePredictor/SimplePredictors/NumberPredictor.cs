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

        //Исходная версия, может аномалить для аномалий, близких к нормальным значениям
        //public override PredictInfo Estimate(DataFrameRow estimate)
        //{
        //    var expectedKey = CreateKey(estimate);
        //    var selectedKf = EstimateKf.Default();
        //    var selectedKey = "";
        //    if (History.TryGetByKey(expectedKey, out HistoryItem item))
        //    {
        //        selectedKf = item.EstimateKf;
        //        selectedKey = expectedKey;
        //    }

        //    return new PredictInfo { Kf = selectedKf, Predictor = this, TargetKey = selectedKey };
        //}

        //private string GetNearKey(, int aditionalValue)
        //{
        //    var arr = baseKey.Split(['_'], StringSplitOptions.RemoveEmptyEntries);
        //    var value = arr[1];
        //    return $"{arr[0]}_{(Convert.ToInt32(value) + aditionalValue)}";
        //}
        public override PredictInfo Estimate(DataFrameRow estimate)
        {
            var expectedKey = CreateKey(estimate);
            var selectedKf = EstimateKf.Default();
            var selectedKey = "";
            if (History.TryGetByKey(expectedKey, out HistoryItem item))
            {
                selectedKf = item.EstimateKf;
                selectedKey = expectedKey;
            }
            //var radius = 
            //else if(History.TryGetByKey(GetNearKey(expectedKey, 1), out item))
            //{
            //    selectedKf = item.EstimateKf;
            //}
            //else if(History.TryGetByKey(GetNearKey(expectedKey, -1), out item))
            //{
            //    selectedKf = item.EstimateKf;
            //}
            return new PredictInfo { Kf = selectedKf, Predictor = this, TargetKey = selectedKey };
        }

        public override bool Filter(DataFrameRow entity)
        {
            var value = GetTargetValue(entity);
            return Filter(value);
        }

        protected virtual bool Filter(object value)
        {
            return true;
            //есть отрацительные числа
            //return (Single)value > 0;
        }
        public override void LoadRow(DataFrameRow row)
        {
            var key = CreateKey(row);
            History.Add(key, (int)EntityScoreEnum.Application);
        }

        protected virtual string CreateKey(DataFrameRow row)
        {
            return CreateKeyInner(this.GetTimeLineValue(row), GetKeyValue(row));
        }

        protected virtual string CreateKeyInner(string hour, int value)
        {
            return $"{hour}_{value}";
        }
        protected virtual int GetKeyValue(DataFrameRow row)
        {
            //округляем до ближайшего по roundingAccuracy
            return ((int)(((Single)GetTargetValue(row) + roundingAccuracy / 2) / roundingAccuracy));
        }
    }
}
