using IPSLib.EstimationPredictors.DeterminePredictor.Models;
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
    /// <summary>
    /// Предназначен для группировке нескольких предикторов по принципу ИЛИ
    /// Например: название и КТРУ хорошо могут работать в связке или
    /// </summary>
    public class GroupPredictor : PredictorBase
    {
        public string GroupName;
        public PredictorBase[] Predictors;
        public GroupPredictor() { }
        public GroupPredictor(params PredictorBase[] predictors)
        {
            GroupName = $"[{String.Join("~OR~", predictors.Select(p => p.PropertyName))}]";
            Predictors = predictors;

            History = new History();
        }
        public override PredictInfo Estimate(Entity entity)
        {
            var maxPredictInfo = new PredictInfo() { Predictor = this };
            foreach (var predictor in Predictors)
            {
                if (predictor.Filter(entity))
                {
                    var predInfo = predictor.Estimate(entity);
                    if(predInfo.Kf.Value > maxPredictInfo.Kf.Value)
                    {
                        maxPredictInfo = predInfo;
                    }
                }
            }

            //Пусть указывается именно тот, кто нашёл
            //maxPredictInfo.Predictor = this;
            //Иначе пересчёт весов будет работать неправильно
            if (!maxPredictInfo.Kf.IsDefault)
            {
                maxPredictInfo.Predictor.Weight = this.Weight;
            }

            return maxPredictInfo;
        }

        public override bool Filter(Entity fieldToEstimate)
        {
            return Predictors.Any(p => p.Filter(fieldToEstimate));
        }

        public override bool Filter(DataFrameRow row)
        {
            return Predictors.Any(p => p.Filter(row));
        }

        public override void LoadRow(DataFrameRow row)
        {
            //приходится делать проверку внутри, иначе не понятно, куда положить данные
            foreach (var predictor in Predictors)
            {
                if (predictor.Filter(row))
                {
                    predictor.LoadRow(row);
                }
            }
        }

        public override void DropStrangeItemsFromHistory()
        {
            foreach (var item in Predictors)
            {
                item.DropStrangeItemsFromHistory();
            }
        }

        public override string ToString()
        {
            return GroupName;
        }
    }
}
