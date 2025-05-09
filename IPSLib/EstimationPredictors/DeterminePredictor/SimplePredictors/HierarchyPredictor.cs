using Microsoft.VisualBasic;
using IPSLib.EstimationPredictors.DeterminePredictor.Models;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Analysis;

namespace IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors
{
    public class HierarchyPredictor : IdPredictor
    {
        public double MaxDistancePercent = 0.01;
        public HierarchyPredictor(string targetPropName, double maxDistancePercent = 0.01) : base(targetPropName)
        {
            MaxDistancePercent = maxDistancePercent;
        }
        public override PredictInfo Estimate(DataFrameRow entity)
        {
            var targetKey = CreateKey(entity);
            var targetVector = GetVector(targetKey);

            //var vectors = History.Items.Select(item => new { Vector = GetVector(item.Key), Kf = item.EstimateKf });
            var vectors = History.Items
                .Select(item => new { Vector = GetVector(item.Key), Kf = item.EstimateKf })
                .ToDictionary(kvp => kvp.Vector, kvp => kvp.Kf);

            var minDistanceKf = EstimateKf.Default();
            var mostNearlyVector = targetVector.GetMostNearly(out HierarchyVector mostNearlyDifference, vectors.Keys.ToArray());

            var selectedKey = "";

            //сверяем, отличие должно быть не более, чем на MaxDistancePercent
            if (targetVector.IsDifferenceNotBig(mostNearlyDifference))
            {
                minDistanceKf = vectors[mostNearlyVector];
                selectedKey = mostNearlyVector.ToString();
            }
            else
            {

            }

            return new PredictInfo {Kf = minDistanceKf, Predictor = this, TargetKey = selectedKey };
        }

        //TODO: стоит переписать под более универсальный вариант, но пока этого хватит
        private HierarchyVector GetVector(string key)
        {
            var result = HierarchyVector.CreateFromString(key);
            return result;
        }
    }
}