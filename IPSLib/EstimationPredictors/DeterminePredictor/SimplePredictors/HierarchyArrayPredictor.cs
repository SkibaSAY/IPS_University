using IPSLib.EstimationPredictors.DeterminePredictor.Models;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors
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

        public override bool Filter(DataFrameRow row)
        {
            var success = base.Filter(row);

            var targetList = PrepareTargetArray(row);
            return success && targetList.All(k => !k.ToString().Equals("0/"));
        }

        //TODO: работает не совсем верно - 0/ уже ничего не значит
        public override bool Filter(Entity entity)
        {
            var success = base.Filter(entity);

            var targetList = PrepareTargetArray(entity);
            return success && targetList.All(k => !k.ToString().Equals("0/"));
        }
    }
}
