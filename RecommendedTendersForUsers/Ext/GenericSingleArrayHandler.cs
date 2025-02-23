using Dapper;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecommendedTendersForUsers.Ext
{
    /// <summary>
    /// Решает проблему маппинга полей типа массив из БД
    /// https://stackoverflow.com/questions/62915756/how-to-deserialize-array-using-dapper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GenericSingleArrayHandler : SqlMapper.TypeHandler<Single[]>
    {
        public override void SetValue(IDbDataParameter parameter, Single[] value)
        {
            parameter.Value = value;
        }

        public override Single[] Parse(object value)
        {
            Single[] result = null;
            var arr = (value as int[]);
            result = arr.Select(n => Convert.ToSingle(n)).ToArray();
            return result;
        }
    }
    public class GenericHierarchyVectorHandler : SqlMapper.TypeHandler<HierarchyVector>
    {
        public override HierarchyVector Parse(object value)
        {
            var hVector = HierarchyVector.CreateFromString((String)value);
            return hVector;
        }

        public override void SetValue(IDbDataParameter parameter, HierarchyVector value)
        {
            parameter.Value = value.ToString();
        }
    }

    public class GenericHierarchyVectorArrayHandler : SqlMapper.TypeHandler<HierarchyVector[]>
    {
        public override HierarchyVector[] Parse(object value)
        {
            var hArray = new List<HierarchyVector>();
            var codes = (String[])value;
            foreach(var code in codes)
            {
                hArray.Add(HierarchyVector.CreateFromString(code));
            }

            return hArray.ToArray();
        }

        public override void SetValue(IDbDataParameter parameter, HierarchyVector[] value)
        {
            parameter.Value = value.Select(hVector => hVector.ToString());
        }
    }
}
