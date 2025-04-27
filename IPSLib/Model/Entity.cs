using Microsoft.Data.Analysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static IPSLib.EstimationPredictors.DeterminePredictors.DeterminePredictor;

namespace IPSLib.Model
{
    /// <summary>
    /// Сущность
    /// </summary>
    public class Entity
    {
        private Dictionary<string, object> _items = new Dictionary<string, object>();
        public Entity(Dictionary<string, object> values)
        {
            foreach(var item in values)
            {
                _items[item.Key] = item.Value;
            }
        }

        public Entity(DataFrameRow row)
        {
            foreach (var kvp in row.GetValues())
            {
                _items[kvp.Key] = kvp.Value;
            }
        }

        public object this[string key]
        {
            get { return _items[key]; }
            set { _items[key] = value; }
        }

        public void FillDataFraweRow(DataFrameRow row)
        {
            foreach (var kvp in _items)
            {
                row[kvp.Key] = kvp.Value;
            }
        }
    }

    public class EntityPredict
    {
        public Single Estimation;

        /// <summary>
        /// Содержит описание, из которого ясно, как получена оценка
        /// </summary>
        public PredictResult PredictResult;
    }
}
