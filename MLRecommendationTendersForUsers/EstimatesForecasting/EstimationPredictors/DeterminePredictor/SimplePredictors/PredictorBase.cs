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
    /// <summary>
    /// Составные элементы DeterminePredictor - анализируют простые поля,
    /// из которых в последствии формируется общие ответ для всей модели
    /// </summary>
    public abstract class PredictorBase
    {
        [JsonIgnore]
        public double MaxWeight = 0.5;
        
        /// <summary>
        /// Вес элемента
        /// </summary>
        public double Weight;

        public PredictorBase()
        {

        }
        public PredictorBase(string targetPropName)
        {
            History = new History();
            Options = new PredictorOptions();
            PropertyName = targetPropName;
        }

        private PropertyInfo _targetProperty;
        [JsonIgnore]
        public PropertyInfo TargetProperty
        {
            get
            {
                if(_targetProperty == null)
                {
                    _targetProperty = typeof(EntityEstimation).GetProperty(PropertyName);
                }
                return _targetProperty;
            }
        }
        public string PropertyName;
        public History History;
        public PredictorOptions Options;

        /// <summary>
        /// Отбрасывает значения не пригодные для оценивания
        /// </summary>
        /// <param name="fieldToEstimate"></param>
        /// <returns></returns>
        public abstract bool Filter(EntityEstimation fieldToEstimate);

        /// <summary>
        /// Выдаёт оценку
        /// </summary>
        /// <param name="fieldToEstimate"></param>
        /// <returns></returns>
        public abstract PredictInfo Estimate(EntityEstimation estimate);

        public abstract void Load(List<EntityEstimation> estimates);

        public virtual void DropStrangeItemsFromHistory()
        {
            History.DropStrangeItems();
        }

        public override string ToString()
        {
            return PropertyName;
        }
    }
    public class PredictorOptions
    {
        public bool NotUseSolo = false;
        public bool SkipIfNotPredict = false;
    }
    public class PredictInfo
    {
        public EstimateKf Kf = EstimateKf.Default();
        public string TargetKey;
        public PredictorBase Predictor;

        public override string ToString()
        {
            return $"{Predictor.Weight.ToString("0.##")}: {TargetKey}: {Kf}";
        }
    }
}
