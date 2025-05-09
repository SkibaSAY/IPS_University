using IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using Nest;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPSLib.Examples.TelecomX
{
    /// <summary>
    /// Класс для решения задачи выявления аномалий в датасете TelecomX
    /// </summary>
    public class TelecomX
    {
        public static char Separator = ';';
        public static string GetSavePath(string userId, string savedDirPath)
        {
            return $"{savedDirPath}\\{userId}.csv";
        }

        private string Path;
        public DeterminePredictor Predictor;

        public DataFrame LearningDataFrame;

        public TelecomX(string dataUserPath)
        {
            this.Path = dataUserPath;
            this.LearningDataFrame = Load();
            //LearnPredictor();

            //TestLearning(this.LearningDataFrame);
            //PlotCheckResult(this.LearningDataFrame);
        }
        private DataFrame Load()
        {
            return DataFrame.LoadCsv(this.Path, separator: TelecomX.Separator);
        }
        public void LearnPredictor()
        {
            var predictors = GetPredictors(this.LearningDataFrame);
            this.Predictor = new DeterminePredictor(predictors);
            this.Predictor.Learn(this.LearningDataFrame);
        }
        private List<PredictorBase> GetPredictors(DataFrame learningData)
        {
            //var addressPredictor = new IdPredictor("Адрес") { MaxWeight = 0.3 };
            //var addressPredictor = new IdPredictor("Адрес") { MaxWeight = 0.3 };
            var result = new List<PredictorBase>();

            foreach (var column in learningData.Columns)
            {
                if (!SessionStatistic.LearningProperties.Any(d => d.Name.Equals(column.Name)))
                {
                    continue;
                }

                //для ip
                //if (column.DataType == typeof(String))
                //{
                //    result.Add(new IdPredictor(column.Name));
                //}
                if (column.DataType == typeof(Single))
                {
                    result.Add(new NumberPredictor(column.Name, roundingAccuracy: (Single)column.Mean() / 10));
                }

            }

            return result;
        }


        /// <summary>
        /// Прогоняет данные по обученной модели и строит график, пок оторому видно, доверяем или нет
        /// </summary>
        /// <param name="items"></param>
        public void PlotCheckResult(DataFrame df, string saveToDir, string fileName, int takeLast = 7*24)
        {
            var points = new SortedDictionary<DateTime, double>();

            //Выводим только результаты за неделю
            foreach (var entity in df.Rows.TakeLast(takeLast))
            {
                var temp = this.Predictor.Predict(entity);
                //переворачиваем график, чтобы пики были аномальной активностью
                //var value = -temp.Estimation + 100;
                var value = temp.Estimation;
                if (value > 100)
                {
                    value = 100;
                }
                else if(value < 0)
                {
                    value = 0;
                }
                points.Add(Convert.ToDateTime(entity["RoundedDate"]), value);
            }

            ScottPlot.Plot myPlot = new();
            myPlot.Add.Scatter(points.Keys.ToList(), points.Values.ToList());
            myPlot.Axes.DateTimeTicksBottom();
            myPlot.Axes.SetLimitsY(0, 100);

            myPlot.SavePng($"{saveToDir}\\{fileName}.png", 1000, 800);
        }

        public int TestLearning(DataFrame testingDf, double trustBorder = 0.4)
        {
            var badItemCount = 0;
            foreach (var entity in testingDf.Rows)
            {
                var temp = this.Predictor.Predict(entity);
                if (temp.PredictResult.TotalKf < trustBorder)
                {
                    badItemCount++;
                }
            }
            return badItemCount;
        }
    }
}
