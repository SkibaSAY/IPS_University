using IPSLib.EstimatesForecasting.EstimateSearcher;
using IPSLib.EstimationPredictors.DeterminePredictor.Models;
using IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictors
{
    public class IdPredictor: NumberPredictor
    {
        public IdPredictor(string propName):base(propName)
        {

        }

        protected override bool Filter(object value)
        {
            return value != null && !String.IsNullOrEmpty(value.ToString()) && value.ToString() != "0";
        }

        protected override string CreateKey(DataFrameRow row)
        {
            return GetTargetValue(row).ToString();
        }
        protected override string CreateKey(Entity entity)
        {
            return GetTargetValue(entity).ToString();
        }
    }
}
