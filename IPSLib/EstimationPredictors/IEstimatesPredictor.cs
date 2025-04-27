using IPSLib.Model;
using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors
{
    public interface IEntityPredictor
    {
        public EntityPredict Predict(Entity entity);
        public string GenerateSavePath(long userId, string cacheDirPath);

        public void Learn(DataFrame learnindData);
        public TestResult Test(DataFrame testData);
        public void Load(string path);
        public void Save(string path);

        public class TestResult
        {
            public double RSquared;
            public override string ToString()
            {
                return RSquared.ToString();
            }
        }
    }
}
