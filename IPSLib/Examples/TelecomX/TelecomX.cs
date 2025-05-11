using IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Model;
using Microsoft.Data.Analysis;
using Nest;
using ScottPlot;
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
        public void PlotCheckResult(DataFrame df, string saveToDir, string userId, int takeLast = 7*24, List<DateTime> anomalyDates = null)
        {
            var userDirToSave = new DirectoryInfo($"{saveToDir}/{userId}");
            if (userDirToSave.Exists)
            {
                userDirToSave.Delete(recursive: true);
            }
            userDirToSave.Create();

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

            //Отмечаем аномальные значения
            if (anomalyDates != null)
            {
                myPlot.Add.ScatterPoints(anomalyDates, points.Where(p=>anomalyDates.Contains(p.Key)).Select(p=>p.Value).ToList(), Colors.Red);
            }
            myPlot.Axes.DateTimeTicksBottom();
            myPlot.Axes.SetLimitsY(0, 100);

            myPlot.SavePng($"{userDirToSave.FullName}\\_predict.png", 1000, 800);

            //сохраним и исходные значения для проверки
            PlotLearning(df, userDirToSave.FullName, anomalyDates: anomalyDates);
        }

        private void PlotLearning(DataFrame df, string saveToDir, int takeLast = 7 * 24, List<DateTime> anomalyDates = null)
        {        
            foreach(var column in SessionStatistic.LearningProperties)
            {
                var points = new SortedDictionary<DateTime, double>();

                //Выводим только результаты за неделю
                foreach (var entity in df.Rows.TakeLast(takeLast))
                {
                    points.Add(Convert.ToDateTime(entity["RoundedDate"]), Convert.ToSingle(entity[column.Name]));
                }
                ScottPlot.Plot myPlot = new();
                myPlot.Add.Scatter(points.Keys.ToList(), points.Values.ToList());
                
                //Отмечаем аномальные значения
                if (anomalyDates != null)
                {
                    myPlot.Add.ScatterPoints(anomalyDates, points.Where(p => anomalyDates.Contains(p.Key)).Select(p => p.Value).ToList(), Colors.Red);
                }
                myPlot.Axes.DateTimeTicksBottom();
                //Иначе последняя запись не помещается и не подписывается
                //myPlot.Axes.SetLimitsY(0, points.Values.Max() + 10);

                myPlot.SavePng($"{saveToDir}\\{column.Name}.png", 1000, 800);
            }
        }

        /// <summary>
        /// Возвращает список дат, где были аномалии
        /// </summary>
        /// <param name="testingDf"></param>
        /// <param name="trustBorder"></param>
        /// <returns></returns>
        public List<DateTime> TestLearning(DataFrame testingDf, double trustBorder = 0.4)
        {
            var anomalyDateTimes = new List<DateTime>();
            foreach (var entity in testingDf.Rows)
            {
                var temp = this.Predictor.Predict(entity);
                if (temp.PredictResult.TotalKf < trustBorder)
                {
                    anomalyDateTimes.Add(Convert.ToDateTime(entity["RoundedDate"]));
                }
            }
            return anomalyDateTimes;
        }
    }
}
