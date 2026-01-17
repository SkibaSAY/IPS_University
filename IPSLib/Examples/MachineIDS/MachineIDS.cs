using IPSLib.EstimationPredictors.DeterminePredictor.SimplePredictors;
using IPSLib.EstimationPredictors.DeterminePredictors;
using IPSLib.Examples.TelecomX;
using Microsoft.Data.Analysis;
using ScottPlot.TickGenerators.TimeUnits;
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

            this.Counter = new AnomalyCounter(totalCount: 60, maxAnomalyCount: 10);
            this.View = viewService;
        }

        public Task Start()
        {
            if (this.ScheduleTask is null)
            {
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

        private async Task Working()
        {
            var interval = 10;
            var now = DateTime.Now;

            while (true)
            {
                var current_date = DateTime.Now;
                //анализ стартует на 5й минуте нового интервала, анализирует изменения за последние 10 минут
                var next_activated_date = new DateTime(
                                                year: now.Year,
                                                month: now.Month,
                                                day: now.Day,
                                                hour: now.Hour,
                                                minute: now.Minute / interval * interval + 5,
                                                second: 0
                                            );

                if (next_activated_date > current_date)
                {
                    await Task.Delay(next_activated_date.Subtract(current_date));
                }

                //Анализируем данные за предыдущий интервал
                //здесь не важны цифры view сам округлит и возмёт и разрезе 5:10 - 5:20, не важно, 5:18 или 5:15 мы передали
                var lastData = this.GetActual(next_activated_date.AddMinutes(-1 * interval));

                foreach(DataFrameRow item in lastData.Rows)
                {
                    bool isAnomaly = this.Analyze(item);
                    this.Counter.Add(isAnomaly: isAnomaly);
                    if (this.Counter.IsLongAnomaly())
                    {
                        //Пишем об ошибке в логи

                        //сброс
                        this.Counter.Reset();
                    }
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

        protected bool Analyze(DataFrameRow row)
        {
            bool isAnomaly = false;
            var result = this.Predictor.Predict(row);
            if (result.Estimation < this.EstimationAnomalyBorder)
            {
                isAnomaly = true;
            }
            return isAnomaly;
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
        public CpuAnalyzer() : base(new ViewService(ViewServiceEnum.CPU), "cpu")
        {

        }
    }

    public class DiskAnalyzer : MachineAnalyzer
    {
        public DiskAnalyzer() : base(new ViewService(ViewServiceEnum.DISK), "disk")
        {

        }
    }

    public class NetworkAnalyzer : MachineAnalyzer
    {
        public NetworkAnalyzer() : base(new ViewService(ViewServiceEnum.NETWORK), "network")
        {

        }
    }
}
