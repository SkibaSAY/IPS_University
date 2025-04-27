using IPSLib.EstimatesForecasting.EstimateSearcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictors
{
    public class HistoryItem
    {
        public string Key;
        public long Count;
        public EstimateKf EstimateKf;
        public double SumEstimates;

        //TODO:пока не используется
        /// <summary>
        /// Принималось ли участие в таком тендере
        /// </summary>
        public bool IsNotNeedDelete = false;

        public HistoryItem(string key)
        {
            Key = key;
            EstimateKf = new EstimateKf();
        }

        public void AddEstimate(double estimate)
        {
            SumEstimates += estimate;
            Count++;
            EstimateKf.Value = SumEstimates / Count / (int)EntityScoreEnum.Application;
        }

        public override string ToString()
        {
            return $"{Key}({Count}) : {EstimateKf}";
        }
    }
}
