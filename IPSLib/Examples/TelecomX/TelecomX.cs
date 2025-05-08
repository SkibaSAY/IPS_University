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
    class SessionStatistic
    {
        public static List<string> IgnoreFields = new List<string>(){ nameof(UserId), nameof(RoundedDate), nameof(IgnoreFields), nameof(LearningFields) };
        public static List<FieldInfo> LearningFields = typeof(SessionStatistic).GetFields()
            .Where(f => !IgnoreFields.Contains(f.Name)).ToList();
        public SessionStatistic() { }
        public SessionStatistic (string userId, DateTime roundedDate)
        {
            this.UserId = userId;
            this.RoundedDate = roundedDate;
        }

        //метаданные
        public string UserId;
        public DateTime RoundedDate;
        //

        public Single SessionCount;

        public Single TotalDuration;
        public Single MedianDuration;
        public Single MaxDuration;

        // TODO: рассмотреть возможность брать медиану

        // Количество Переданных(Up) бит
        public Single TotalTransmittedBit;
        public Single MedianTransmittedBit;
        public Single MaxTransmittedBit;

        // Количество Полученных(Down) бит
        public Single TotalReceivedBit;
        public Single MedianReceivedBit;
        public Single MaxReceivedBit;

        public void Add(SessionStatistic additional)
        {
            //TODO: избавиться от копипаста
            this.MaxDuration = Math.Max(this.MaxDuration, additional.MaxDuration);
            this.MedianDuration = GetMergedMedian(this.MedianDuration, this.SessionCount, additional.MedianDuration, additional.SessionCount);
            this.TotalDuration += additional.TotalDuration;

            this.MaxTransmittedBit = Math.Max(this.MaxTransmittedBit, additional.MaxTransmittedBit);
            this.MedianTransmittedBit = GetMergedMedian(this.MedianTransmittedBit, this.SessionCount, additional.MedianTransmittedBit, additional.SessionCount);
            this.TotalTransmittedBit += additional.TotalTransmittedBit;

            this.MaxReceivedBit = Math.Max(this.MaxReceivedBit, additional.MaxReceivedBit);
            this.MedianReceivedBit = GetMergedMedian(this.MedianReceivedBit, this.SessionCount, additional.MedianReceivedBit, additional.SessionCount);
            this.TotalReceivedBit += additional.TotalReceivedBit;

            this.SessionCount += additional.SessionCount;
        }

        /// <summary>
        /// Настоящую медиану пока себе не позволяем - неудобно хранить всё
        /// </summary>
        /// <param name="medianA"></param>
        /// <param name="countA"></param>
        /// <param name="medianB"></param>
        /// <param name="countB"></param>
        /// <returns>Средняя медиана</returns>
        private Single GetMergedMedian(Single medianA, Single countA, Single medianB, Single countB)
        {
            return (medianA * countA + medianB * countB) / (countA + countB);
        }
    }

    /// <summary>
    /// Класс для решения задачи выявления аномалий в датасете TelecomX
    /// </summary>
    public class TelecomX
    {
        /// <summary>
        /// В папке множество csv файлов для обучения
        /// </summary>
        private string Path;
        private int HourInterval;

        public TelecomX(string datasetPath, int hourInterval = 1)
        {
            this.Path = datasetPath;
            this.HourInterval = hourInterval;
            var allLearningData = Load();

            var expectedUserId = 5548;

            var learningData = allLearningData.Values.Where(s => s.UserId == expectedUserId.ToString()).ToList();

            TestLearning(learningData);
        }

        private void TestLearning(List<SessionStatistic> learningList)
        {ю telecom
            var learningData = GetLearningDataFrame(learningList);
            var predictors = GetPredictors(learningData);
            var predictor = new DeterminePredictor(predictors);

            predictor.Learn(learningData);
            var res = 1;

            var badItemCount = 0;
            foreach (var item in learningData.Rows)
            {
                var entity = new IPSLib.Model.Entity(item);
                var temp = predictor.Predict(entity);
                if (temp.PredictResult.TotalKf < 0.4)
                {
                    badItemCount++;
                }
            }
        }

        private DataFrame GetLearningDataFrame(List<SessionStatistic> learningList)
        {
            var columns = new List<DataFrameColumn>();
            foreach (var learningField in SessionStatistic.LearningFields)
            {
                var columnData = learningList.Select(d => Convert.ToSingle(learningField.GetValue(d)))
                    .ToArray();
                columns.Add(new SingleDataFrameColumn(learningField.Name, columnData));
            }
            return new DataFrame(columns);
        }
        static List<PredictorBase> GetPredictors(DataFrame learningData)
        {
            //var addressPredictor = new IdPredictor("Адрес") { MaxWeight = 0.3 };
            //var addressPredictor = new IdPredictor("Адрес") { MaxWeight = 0.3 };
            var result = new List<PredictorBase>();

            foreach (var column in learningData.Columns)
            {
                if (column.DataType == typeof(String))
                {
                    result.Add(new IdPredictor(column.Name));
                }
                else if (column.DataType == typeof(Single))
                {
                    result.Add(new NumberPredictor(column.Name, roundingAccuracy: (Single)column.Mean() / 10));
                }

            }

            return result;
        }

        /// <summary>
        /// Округление даты в зависимости от интервала часов
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private DateTime RoundDate(DateTime date)
        {
            var hours = date.Hour;
            var roundedDate = date.Date;
            //округление в меньшую сторону
            roundedDate = roundedDate.AddHours(hours / this.HourInterval * this.HourInterval);
            return roundedDate;
        }


        private static Regex CsvPxRegex = new Regex(@"^psx_(.*)_(?<date>\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2})\.csv$");
        private static string DateFormat = "yyyy-MM-dd HH_mm_ss";
        
        private Dictionary<string, SessionStatistic> Load()
        {
            if (!Directory.Exists(Path)) 
            {
                throw new Exception("НЕ удалочь найти обучающий датасет!!!");
            }

            //группируем по часам
            var stats = new Dictionary<string, SessionStatistic>();

            var files = Directory.GetFiles(Path);
            foreach (var filePath in files)
            {
                var file = new FileInfo(filePath);
                var match = CsvPxRegex.Match(file.Name);
                if (!match.Success)
                {
                    continue;
                }

                var date = DateTime.ParseExact(match.Groups["date"].ToString(), DateFormat, CultureInfo.InvariantCulture);
                var roundedDate = RoundDate(date);
                var df = DataFrame.LoadCsv(file.FullName);

                //df.FillNulls(0, true);
                //уникальные пользователи
                foreach(var userObj in df["IdSubscriber"].ValueCounts().Columns[0])
                {
                    var key = $"{userObj}__{roundedDate.ToString()}";
                    var contains = stats.TryGetValue(key, out SessionStatistic statistic);
                    if (!contains)
                    {
                        statistic = new SessionStatistic(userId: userObj.ToString(), roundedDate);
                        stats.Add(key, statistic);
                    }

                    //в 1 файле для 1 пользователя вполне может быть несколько сессий
                    var sessions = df.Filter(df["IdSubscriber"].ElementwiseEquals(Convert.ToSingle(userObj)));
                    var sessionsCount = sessions.Rows.Count;

                    var additionalStats = new SessionStatistic
                    {
                        MaxDuration = (Single)sessions["Duartion"].Max(),
                        MedianDuration = (Single)sessions["Duartion"].Median(),
                        TotalDuration = (Single)sessions["Duartion"].Sum(),

                        MaxTransmittedBit = (Single)sessions["UpTx"].Max(),
                        MedianTransmittedBit = (Single)sessions["UpTx"].Median(),
                        TotalTransmittedBit = (Single)sessions["UpTx"].Sum(),

                        MaxReceivedBit = (Single)sessions["DownTx"].Max(),
                        MedianReceivedBit = (Single)sessions["DownTx"].Median(),
                        TotalReceivedBit = (Single)sessions["DownTx"].Sum(),

                        SessionCount = sessions.Rows.Count
                    };

                    statistic.Add(additionalStats);
                }
            }
            return stats;
        }
    }
}
