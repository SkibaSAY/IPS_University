using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.Model
{
    public class PreparedData<T> where T : class
    {
        public PreparedData() { }
        public List<T> LearningData = new List<T>();
        public List<T> TestData = new List<T>();

        private static Random Random = new Random();
        public static PreparedData<T> Prepare(List<T> allItems, double learningFraction = 0.8)
        {
            var result = new PreparedData<T>();
            foreach(var item in allItems)
            {
                if(Random.NextDouble() < learningFraction)
                {
                    result.LearningData.Add(item);
                }
                else
                {
                    result.TestData.Add(item);
                }
            }
            return result;
        }
    }
}
