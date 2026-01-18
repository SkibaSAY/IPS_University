using Elasticsearch.Net;
using IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Examples.TelecomX;
using Microsoft.Data.Analysis;
using Nest;
using ScottPlot.TickGenerators.TimeUnits;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.Examples.MachineIDS
{
    /// <summary>
    /// Логика:
    /// Для каждого такого логгера существует
    /// </summary>
    public class MachineAnalyzer
    {
        private FileInfo SavePath;
        private DeterminePredictor Predictor;
        protected ViewService View;

        protected int EstimationAnomalyBorder = 75;
        protected AnomalyCounter Counter;

        protected Task ScheduleTask;

        public static string GetSavePath(string hour, string savedDirPath)
        {
            return $"{savedDirPath}\\{hour}.csv";
        }

        public string Name;
        public static DirectoryInfo SaveDir = new DirectoryInfo(@"C:\\IDS");

        //TODO: доделать работы с безопасной остановкой - CancellationToken
        public MachineAnalyzer(ViewService viewService, string name, CancellationToken? token = null)
        {
            this.Name = name;

            this.Counter = new AnomalyCounter(totalCount: 60, maxAnomalyCount: 20);
            this.View = viewService;
        }

        public Task Start()
        {
            if (this.ScheduleTask is null)
            {
                this.WriteLog(LogEventLevel.Information, "Started");
                this.ScheduleTask = this.Working();
            }
            return this.ScheduleTask;
        }

        public void Stop()
        {
            if (this.ScheduleTask is not null)
            {
                this.ScheduleTask.Dispose();
            }
        }

        protected void WriteLog(LogEventLevel logLevel, string msg)
        {
            var newMsg = $"{this.Name}: {msg}";
            Log.Write(logLevel, newMsg);
        }

        private async Task Working()
        {
            var interval = 10;
            var now = DateTime.Now;

            //анализ стартует на 5й минуте нового интервала, анализирует изменения за последние 10 минут
            var next_activated_date = new DateTime(
                                            year: now.Year,
                                            month: now.Month,
                                            day: now.Day,
                                            hour: now.Hour,
                                            minute: now.Minute / interval * interval + 5,
                                            second: 0
                                        );

            //Костыль - хочу запустить за прошлое, начиная с прошлого часа
            //next_activated_date = next_activated_date.AddMinutes(-60 * 3);

            if (next_activated_date > DateTime.Now)
            {
                //При старте нам сразу бы стартовать - проверим прошлый интервал
                next_activated_date = next_activated_date.AddMinutes(-10);
            }

            while (true)
            {
                var current_date = DateTime.Now;

                this.WriteLog(LogEventLevel.Information, $"Время следующего запуска '{next_activated_date.ToString("F")}'");
                if (next_activated_date > current_date)
                {
                    await Task.Delay(next_activated_date.Subtract(current_date));
                }

                var analyzed_start_interval = next_activated_date.AddMinutes(-1 * 15);
                var analyzed_finish_interval = analyzed_start_interval.AddMinutes(10);
                var analyzed_interval = $"[{analyzed_start_interval.ToString("g")} - {analyzed_finish_interval.ToString("g")}]";
                this.WriteLog(LogEventLevel.Information, $"Запущен. Исследуемый интервал: {analyzed_interval}");
                try
                {
                    //Анализируем данные за предыдущий интервал
                    //здесь не важны цифры view сам округлит и возмёт и разрезе 5:10 - 5:20, не важно, 5:18 или 5:15 мы передали
                    var lastData = this.GetActual(next_activated_date.AddMinutes(-1 * interval));

                    var missed_logs = 10 - lastData.Rows.Count;
                    if (missed_logs > 0)
                    {
                        this.WriteLog(LogEventLevel.Warning, $"Обнаружено оставание логгера: для полного анализа не хватает {missed_logs}/{10} записей");
                    }

                    var anomalyCount = 0;
                    var estimates = new List<Single>();
                    foreach (DataFrameRow item in lastData.Rows)
                    {
                        var estimate = this.Analyze(item, out bool isAnomaly);
                        estimates.Add(estimate);
                        if (isAnomaly)
                        {
                            anomalyCount++;
                        }

                        this.Counter.Add(isAnomaly: isAnomaly);
                        if (this.Counter.IsLongAnomaly())
                        {
                            //Пишем об ошибке в логи
                            this.WriteLog(LogEventLevel.Error, "Обнаружено устойчивое отклонение.");
                            //сброс
                            this.Counter.Reset();
                        }
                    }

                    this.WriteLog(LogEventLevel.Information, $"Анализ завершён: Уровень доверия: {(int)(estimates.Average())}; Аномалий: {anomalyCount}/{lastData.Rows.Count}; Потеряно: {missed_logs}/10");
                }
                catch(Exception ex)
                {
                    this.WriteLog(LogEventLevel.Fatal, $"Ошибка выполняения: {ex.Message}");
                }
                finally
                {
                    next_activated_date = next_activated_date.AddMinutes(10);
                }
            }
        }

        private DataFrame GetLearningData(int hour)
        {
            return this.View.GetLearning(hour);
        }

        private DataFrame GetActual(DateTime dateTime)
        {
            return this.View.GetActual(dateTime);
        }

        public virtual void Learn()
        {
            DataFrame learningData = new DataFrame();
            bool firstAnalized = false;
            for (var i = 0; i < 24; i++)
            {
                var learning_batch = GetLearningData(hour:i);
                if (!firstAnalized && learning_batch.Rows.Count > 0)
                {
                    firstAnalized = true;
                    learningData = learning_batch;
                }
                //Потенциально может быть время, когда данных нет, например, ночью - тут мы всегда будем давать аномалии, тк активность в неактивное время
                else if(learning_batch.Rows.Count > 0)
                {
                    learningData.Append(learning_batch.Rows, inPlace: true);
                }
            }
            var predictors = GetPredictors(learningData);
            this.Predictor = new DeterminePredictor(predictors);
            this.Predictor.Learn(learningData);
        }

        private List<PredictorBase> GetPredictors(DataFrame learningData)
        {
            var result = new List<PredictorBase>();

            foreach (var column in learningData.Columns)
            {
                if (column.DataType == typeof(Single))
                {
                    result.Add(new NumberPredictor(column.Name, roundingAccuracy: (Single)column.Mean() / 20));
                }
            }

            return result;
        }

        protected float Analyze(DataFrameRow row, out bool isAnomaly)
        {
            isAnomaly = false;
            var result = this.Predictor.Predict(row);
            if (result.Estimation < this.EstimationAnomalyBorder)
            {
                isAnomaly = true;
            }
            return result.Estimation;
        }

        public void Save()
        {
            if (!SaveDir.Exists)
            {
                SaveDir.Create();
            }
            this.Predictor.Save(@$"{SaveDir.FullName}\{this.Name}.json");
        }
    }


    public class CpuAnalyzer : MachineAnalyzer
    {
        public CpuAnalyzer() : base(new ViewService(ViewServiceEnum.CPU), "cpu_analyzer")
        {

        }
    }

    public class DiskAnalyzer : MachineAnalyzer
    {
        public DiskAnalyzer() : base(new ViewService(ViewServiceEnum.DISK), "disk_analyzer")
        {

        }
    }

    public class NetworkAnalyzer : MachineAnalyzer
    {
        public NetworkAnalyzer() : base(new ViewService(ViewServiceEnum.NETWORK), "network_analyzer")
        {

        }
    }
}
