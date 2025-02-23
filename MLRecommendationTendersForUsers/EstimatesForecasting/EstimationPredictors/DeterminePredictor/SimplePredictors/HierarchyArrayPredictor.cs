using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.Models;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.SimplePredictors
{
    internal class HierarchyArrayPredictor: ArrayPredictor<HierarchyVector>
    {
        //важно, чтобы мин группа была из одного элемента иначе в PrepareKeyGroups сломается
        //получение отдельных ключей
        const int _minItemsInGroup = 1;
        public HierarchyArrayPredictor(string targetPropName, int maxItemsInGroup = 3) : base(targetPropName, maxItemsInGroup, _minItemsInGroup)
        {

        }
        protected override List<KeysGroup> PrepareKeyGroupsByEstimate(List<HierarchyVector> targetList)
        {
            var comparer = EqualityComparer<HierarchyVector>.Create((a, b) => a.ToString().Equals(b.ToString()), a=>a.GetHashCode());
            targetList = targetList.Distinct(comparer).ToList();
            //принципиальное отличие в том, что перед формирование групп нужно в targetList положить
            //самый близкий вектор для значения
            var mostNearlyDict = new Dictionary<HierarchyVector, HierarchyVector>();

            //В истории лежат группы векторов, нам нужны группы из одного вектора
            //тк из этих векторов группы и формируются
            var historyVectors = History.Items.Where(x => !x.Key.Contains(KeySeparator))
                .Select(x => HierarchyVector.CreateFromString(x.Key)).ToList();

            foreach (var target in targetList)
            {
                var mostNearlyVector = target.GetMostNearly(out HierarchyVector mostNearlyDifference, historyVectors);

                if (target.IsDifferenceNotBig(mostNearlyDifference))
                {
                    mostNearlyDict.Add(target, mostNearlyVector);
                }
            }

            var newTargetList = mostNearlyDict.Values.ToList();
            return base.PrepareKeyGroupsByLearn(newTargetList);
        }

        public override bool Filter(EntityEstimation fieldToEstimate)
        {
            var success = base.Filter(fieldToEstimate);

            var targetList = PrepareTargetArray(fieldToEstimate);
            return success && targetList.All(k => !k.ToString().Equals("0/"));
        }
    }
}
