using Microsoft.Data.Analysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPSLib.Examples.TelecomX
{
    /// <summary>
    /// Преобразовывает грязные данные, где все пользователи в куче, в красивые датасеты, сгруппированные по пользователю
    /// </summary>
    public class TelecomXDirtyDataPreparer
    {
        private int HourInterval = 1;
        public HashSet<string> UserIds = new HashSet<string>();
        public void GroupByUser(string path, string saveToDirPath)
        {
            var dirtyData = Load(path);

            foreach(var userId in this.UserIds)
            {
                var learningData = dirtyData.Values.Where(s => s.UserId == userId).ToList();
                var learningDf = GetLearningDataFrame(learningData);

                DataFrame.SaveCsv(learningDf, TelecomX.GetSavePath(userId, saveToDirPath), separator: TelecomX.Separator);
            }
        }

        private static Regex CsvPxRegex = new Regex(@"^psx_(.*)_(?<date>\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2})\.(csv|txt)$");
        private static string DateFormat = "yyyy-MM-dd HH_mm_ss";
        private DataFrame GetLearningDataFrame(List<SessionStatistic> learningList)
        {
            var columns = new List<DataFrameColumn>();
            foreach (var learningProp in SessionStatistic.ViewProperties)
            {
                if (learningProp.PropertyType == typeof(Single))
                {
                    var columnData = learningList.Select(d => Convert.ToSingle(learningProp.GetValue(d)));
                    columns.Add(new SingleDataFrameColumn(learningProp.Name, columnData));
                }
                else if(learningProp.PropertyType == typeof(String))
                {
                    var columnData = learningList.Select(d => Convert.ToString(learningProp.GetValue(d)));
                    columns.Add(new StringDataFrameColumn(learningProp.Name, columnData));
                }
                else if (learningProp.PropertyType == typeof(DateTime))
                {
                    var columnData = learningList.Select(d => Convert.ToDateTime(learningProp.GetValue(d)));
                    columns.Add(new DateTimeDataFrameColumn(learningProp.Name, columnData));
                }
                else
                {
                    throw new Exception($"{learningProp.Name}: не найден конвертер для преобразования");
                }
            }
            return new DataFrame(columns);
        }

        /// <summary>
        /// Загружает все данные и группирует по пользователю и дате
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private Dictionary<string, SessionStatistic> Load(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception("НЕ удалочь найти обучающий датасет!!!");
            }

            //группируем по часам
            var stats = new Dictionary<string, SessionStatistic>();

            var files = Directory.GetFiles(path);
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
                var df = LoadDataFrame(file.FullName);

                //df.FillNulls(0, true);
                //уникальные пользователи
                foreach (var userObj in df["IdSubscriber"].ValueCounts().Columns[0])
                {
                    this.UserIds.Add(userObj.ToString());

                    //статистика может разбиваться на файлы, поэтому аккумулируем из всех
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

                    statistic.Merge(additionalStats);
                }
            }
            return stats;
        }

        /// <summary>
        /// csv OR txt
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private DataFrame LoadDataFrame(string filePath)
        {
            //Коммутаторы выгружают данные в csv или txt - нужно учитывать
            if (filePath.EndsWith(".csv"))
            {
                return DataFrame.LoadCsv(filePath);
            }
            else if (filePath.EndsWith(".txt"))
            {
                var csvString = String.Join("\n\r", File.ReadAllLines(filePath));
                return DataFrame.LoadCsvFromString(csvString, separator: '|');
            }
            throw new Exception($"File '{filePath}' не удалось прочитать ни одним из способов");
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
    }
}
