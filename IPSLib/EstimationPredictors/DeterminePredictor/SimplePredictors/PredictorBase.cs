using IPSLib.EstimationPredictors.DeterminePredictor.Models;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictors
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

        public string PropertyName;
        public object GetTargetValue(DataFrameRow row)
        {
            return row[PropertyName];
        }

        public History History;
        public PredictorOptions Options;

        /// <summary>
        /// Отбрасывает значения не пригодные для оценивания
        /// </summary>
        /// <param name="fieldToEstimate"></param>
        /// <returns></returns>
        public abstract bool Filter(DataFrameRow row);

        //TODO: может быть полезен
        //public abstract DataFrame Filter(DataFrame row);

        /// <summary>
        /// Выдаёт оценку
        /// </summary>
        /// <param name="fieldToEstimate"></param>
        /// <returns></returns>
        public abstract PredictInfo Estimate(DataFrameRow estimate);

        public virtual void Load(DataFrame df)
        {
            foreach(var row in df.Rows)
            {
                LoadRow(row);
            }
        }
        
        public abstract void LoadRow(DataFrameRow row);


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
