using IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Examples.TelecomX;
using Microsoft.Data.Analysis;

namespace IpsExampleConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //ReadFrameCsv("example.txt");
            //TestLearning();
            TestXTelecom();
        }
        
        static void TestLearning()
        {
            var learningData = ReadFrameCsv("example_tcp.txt");
            var predictors = GetPredictors(learningData);
            var predictor = new DeterminePredictor(predictors);
            predictor.Learn(learningData);
            var res = 1;

            var badItemCount = 0;
            foreach(var item in learningData.Rows)
            {
                var temp = predictor.Predict(item);
                if(temp.PredictResult.TotalKf < 0.8)
                {
                    badItemCount++;
                }
            }
        }

        static void TestXTelecom()
        {
            var telecomDirty = new TelecomXDirtyDataPreparer();
            var path = "C:\\Users\\Admin\\Desktop\\telecom10k";
            var usersPath = $"{path}\\users";

            //Данные мы преобразовали, пока хватит
            PrepareDirtyData(telecomDirty, path, usersPath);

            var expectedUserId = 5548;
            //var users = new string[] { "5548", "3800", "8200" };
            var users = telecomDirty.UserIds;
            foreach (var userId in users.Take(100))
            {
                var telecom = new TelecomX(TelecomX.GetSavePath(userId, usersPath));
                telecom.PlotCheckResult(telecom.LearningDataFrame, usersPath, userId);
            }
        }
        static void PrepareDirtyData(TelecomXDirtyDataPreparer telecomDirty, string path, string saveTo)
        {
            var usersDir = new DirectoryInfo(saveTo);
            if (usersDir.Exists)
            {
                Directory.Delete(saveTo, true);
            }
            usersDir.Create();

            telecomDirty.GroupByUser(path, saveTo);
        }

        static List<PredictorBase> GetPredictors(DataFrame learningData)
        {
            //var addressPredictor = new IdPredictor("Адрес") { MaxWeight = 0.3 };
            //var addressPredictor = new IdPredictor("Адрес") { MaxWeight = 0.3 };
            var result = new List<PredictorBase>();

            foreach(var column in learningData.Columns)
            {
                if(column.DataType == typeof(String))
                {
                    result.Add(new IdPredictor(column.Name));
                }
                else if(column.DataType == typeof(Single))
                {
                    result.Add(new NumberPredictor(column.Name, roundingAccuracy: (Single)column.Mean()/10));
                }

            }

            return result;
        }

        //static List<Entity> ReadCsv(string filename)
        //{
        //    var result = new List<Dictionary<string, string>>();
        //    var i = 0;
        //    string[] columns = null;
        //    foreach(var line in File.ReadLines(filename))
        //    {
        //        var values = line.Split(',');
        //        if (i == 0)
        //        {
        //            columns = values;
        //            i++;
        //            continue;
        //        }

        //        var new_item = new Entity();
        //        for(var j = 0; j < values.Length; j++)
        //        {
        //            var prop_name = columns[j];
        //           // new_item.prop_name = 
        //            //new_item[] = values[j];
        //            //new_item.TrySetMember(columns[j], values[j]); 
        //        }
        //        //result.Add(new_item);
        //        i++;
        //    }
        //    return null;
        //}

        static DataFrame ReadFrameCsv(string filename)
        {
            var dataFrame = DataFrame.LoadCsv(filename);
            //foreach(var row in dataFrame.Rows)
            //{
            //    var res = row["Адрес"];
            //    //row["Адрес"] = "0.0.0.1";
            //}
            return dataFrame;
        }
    }
}
