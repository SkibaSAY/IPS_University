using Dapper;
using Microsoft.ML;
using Microsoft.ML.Data;
using MLRecommendationTendersForUsers.EstimatesForecasting;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using RecommendedTendersForUsers.Ext;
using RecommendedTendersForUsers.Models;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Tenderland.Database.PostgreSql;
using Tenderland.Logging;
using Tenderland.MessageQueue;
using static MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors.DeterminePredictor;
using static MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.IEstimatesPredictor;
using String = System.String;

namespace RecommendedTendersForUsers
{
    internal class Program
    {
        static string AppName = "RecommendedTendersForUsers";
        public static string CacheDir = "Cache";
        public static string ModelsDir = $"{CacheDir}\\Models";

        static string connectionsPath = @"C:\Publish\connections.txt";
        static RabbitMQClient MqClient = new RabbitMQClient(connectionsPath, "rabbitmq", AppName, false);
        static PgClient PgClient;
        static Controller Controller;

        static void Main(string[] args)
        {
            InitCache();
            InitLog();
            InitDB();
            InitController();
            //LoadMissedModels();
            //для всех клиентов
            //LoadEstimatesForcastingUserModel();

            //ещё был 2390
            //var users = new long[] { 627, 17349, 10687, 3942, 3449 }.Select(id => new User { Id = id }).ToList();
            //var users = new long[] {17349, 3942 }.Select(id => new User { Id = id }).ToList();
            //var users = new long[] { 26769 }.Select(id => new User { Id = id }).ToList();
            //LoadEstimatesForcastingUserModel(users);

            //ExportTendersByQuery(users);
            //users.ForEach(user => CompareOldRecommendationByNewVersion(user));
            //users.ForEach(user => ExportLearningData(user)); 

            //Отчёт
            //users.ToList().ForEach(u=>UserRecommendationReport.CreateReport(PgClient,new User { Id = u}));
            //users.ToList().ForEach(u => UserRecommendationReport.CreateReport(PgClient, new User { Id = u }, ignoreAutosearch: true));

            Start();
        }

        static void Start()
        {
            Controller.Start();
            Console.ReadLine();
        }
        static void LoadEstimatesForcastingUserModel(List<User> users = null)
        {
            if (users == null)
            {
                users = User.AllToLearning(PgClient).ToList();
            }

            //var controller = new UserEstimationController<MLEstimationPredictor>(PgClient);
            var controller = new UserEstimationController<DeterminePredictor>(PgClient);
            var timer = new Stopwatch();
            var a = 0;
            foreach (var userId in users.Select(u=>u.Id))
            {
                try
                {
                    var learnSuccess = false;
                    a++;
                    Console.WriteLine($"{a}.Начинаю обработку {userId}");
                    timer.Restart();
                    //получить обучающие данные
                    var sourceData = controller.LoadLearningDataForUser(userId);

                    //выгрузка тестовых данных в эксель
                    //UserRecommendationReport.ExportToExcel(sourceData, new User { Id = userId }, "Learning");
                    //continue;

                    timer.Stop();

                    var loadTime = timer.Elapsed;
                    TimeSpan learningTime = new TimeSpan();
                    TestResult trainedResult = null;

                    //если данные есть, пробуем учить
                    if (sourceData.Count() >= EstimationModelSettings.MinimumCountToLearn)
                    {
                        learnSuccess = true;

                        //разбить на обучающие и тестовые
                        //var preparedData = controller.PrepareLearningAndTestData(sourceData);
                        var learn = sourceData;
                        var test = sourceData;

                        timer.Restart();

                        //обучить модель
                        var predictor = controller.CreatePredictor();
                        predictor.Learn(learn);

                        timer.Stop();
                        learningTime = timer.Elapsed;

                        //протестировать модель
                        //trainedResult = predictor.Test(sourceData);
                        trainedResult = predictor.Test(test);
                        
                        ////догрузили тестовые данные в модель
                        //(predictor as DeterminePredictor).LoadData(preparedData.TestData);
                        
                        ////Из общей истории удаляем данные, которые редко встречаются в выборке
                        //(predictor as DeterminePredictor).DropStrangeItemsFromHistory();

                        //кф детерминации (0.0 - 1.0) чем ближе к 1, тем лучше модель, уже хорошо 0.8-0.9
                        Console.WriteLine($"*       RSquared Score:      {trainedResult.RSquared:0.##}");

                        //сохранить модель на диск
                        var savePath = predictor.GenerateSavePath(userId, ModelsDir);
                        predictor.Save(savePath);
                    }
                 
                    //независимо, успешно обучение или нет, пишем в базу
                    var queryParameters = new List<QueryParameter>()
                    {
                        new QueryParameter("userId", userId),
                        new QueryParameter("sourceDataCount", sourceData?.Count??0),
                        new QueryParameter("rsquared", trainedResult?.RSquared??0),
                        new QueryParameter("learningTime", learningTime),
                        new QueryParameter("loadDataTime", loadTime)
                    };
                    //записать в базу:
                    var insertQuery = $@"INSERT INTO machine_learning.estimates_forecasting(user_id, source_data_count, rsquared, learning_time, load_data_time,sys_update_date) 
                        VALUES(
                        @userId, @sourceDataCount, @rsquared, @learningTime, @learningTime, now())                 
                        ON CONFLICT(user_id) 
                        DO UPDATE 
                        SET source_data_count = EXCLUDED.source_data_count
                        ,rsquared = EXCLUDED.rsquared
                        ,learning_time = EXCLUDED.learning_time
                        ,load_data_time = EXCLUDED.load_data_time
                        ,sys_update_date = EXCLUDED.sys_update_date";

                    PgClient.Execute(insertQuery, queryParameters);

                    if (learnSuccess)
                    {
                        Log.Information("Модель рекомендаций для пользователя {user_id} обучена", userId);
                    }
                    else
                    {
                        Log.Information("Для обучения модели рекомендаций пользователя {user_id} недостаточно данных", userId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Ошибка при обучении модели рекомендаций для пользователя {user_id}", userId);
                }
            }
        }

        static void LoadMissedModels()
        {
            var savedModels = new DirectoryInfo(ModelsDir).GetFiles().Select(f => long.Parse(f.Name.Replace(".json",""))).ToList();
            var dbModels = User.AllToLearning(PgClient).Select(u=>u.Id);

            var missed = dbModels.Except(savedModels).ToList();

            LoadEstimatesForcastingUserModel(missed.Select(id => new User { Id = id }).ToList());
        }

        static void CompareOldRecommendationByNewVersion(User user)
        {
            var userEstimationController = new UserEstimationController<MLEstimationPredictor>(PgClient);
            var recommendations = UserRecommendationReport.GetUserRecommendations(PgClient, user);
            //recommendations = UserRecommendationReport.IgnoreTendersInAutosearch(PgClient, recommendations, user);

            EntityEstimation.FillEstimationsFromBaseTables(PgClient,recommendations);

            var predictor = new DeterminePredictor();
            var path = predictor.GenerateSavePath(user.Id, ModelsDir);
            predictor.Load(path);

            //заходи туда под отладкой и смотри сравнение
            //predictor.Test(recommendations);

            var descriptions = new Dictionary<EntityEstimation, PredictResult>();
            //Для сравнения старой и новой версии по одним и тем же тендерам
            foreach(var rec in recommendations)
            {
                var result = predictor.Predict(rec);
                rec.NewEstimation = result.Estimation;

                descriptions.Add(rec, result.PredictResult);
            }
            //OldNewCompare
            UserRecommendationReport.ExportToExcel(recommendations, user, descriptions,  postfix: "Рекоммендации");
        }

        static void ExportTendersByQuery(List<User> users)
        {
            var tenders = UserRecommendationReport.GetEntitiesByLastDays(PgClient, 3);
            EntityEstimation.FillEstimationsFromBaseTables(PgClient, tenders);
            foreach (var user in users)
            {
                var notAutosearchTenders = UserRecommendationReport.IgnoreTendersInAutosearch(PgClient, tenders, user);
                //var notAutosearchTenders = tenders;

                Export(notAutosearchTenders, user, "рекомендованные тендеры");
            }         
        }
        static void ExportLearningData(User user)
        {
            var userEstimationController = new UserEstimationController<MLEstimationPredictor>(PgClient);
            var learning = userEstimationController.LoadLearningDataForUser(user.Id);
            EntityEstimation.FillEstimationsFromBaseTables(PgClient, learning);

            Export(learning, user, "LearningData");
        }

        static void Export(List<EntityEstimation> targetTenders, User user, string exportName = "Export")
        {
            var predictor = new DeterminePredictor();
            var path = predictor.GenerateSavePath(user.Id, ModelsDir);
            predictor.Load(path);

            var exportedTenders = new List<EntityEstimation>();
            var descriptions = new Dictionary<EntityEstimation, PredictResult>();
            //Для сравнения обучающих данных и полученной оценки
            foreach (var rec in targetTenders)
            {
                //(rec.EndDate == null || DateTime.Parse(rec.EndDate, CultureInfo.InvariantCulture) > DateTime.Now) && 
                if (predictor.Filter(rec))
                {
                    var result = predictor.Predict(rec);
                    //rec.NewEstimation = result.Estimation;
                    rec.Estimation = result.Estimation;
                    if (result.Estimation >= 80)
                    {
                        exportedTenders.Add(rec);
                        descriptions.Add(rec, result.PredictResult);
                    }
                }
            }

            //UserRecommendationReport.ExportToExcel(exportedTenders, user, descriptions, postfix: exportName);
            UserRecommendationReport.ExportToExcel(exportedTenders, user, postfix: exportName);
        }

        #region Init
        static void InitCache()
        {
            if (!Directory.Exists(CacheDir))
            {
                Directory.CreateDirectory(CacheDir);
            }
            if (!Directory.Exists(ModelsDir))
            {
                Directory.CreateDirectory(ModelsDir);
            }
        }
        static void InitDB()
        {
            PgClient = new PgClient(AppName, "postgres", connectionsPath);
            PgClient.Error += PgClient_Error;
            PgClient.ErrorProcessed += PgClient_ErrorProcessed;

            SqlMapper.AddTypeHandler(new GenericSingleArrayHandler());
            SqlMapper.AddTypeHandler(new GenericHierarchyVectorHandler());
            SqlMapper.AddTypeHandler(new GenericHierarchyVectorArrayHandler()); 
        }

        static void InitLog()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("app", AppName)
            //не стоит в консоль писать сообщения от Information, тк их много
            //- вывод в консоль большого числа инфы замедлит работу и уменьшит информативность, в этом app это критично
            .WriteTo.Console(Serilog.Events.LogEventLevel.Warning)
            .WriteTo.RabbitMQ(MqClient, new RabbitMQSinkConfiguration() { RestrictedToMinimumLevel = Serilog.Events.LogEventLevel.Information })
            .CreateLogger();
        }
        static void InitController()
        {
            Controller = new Controller(MqClient, PgClient);
        }

        private static void PgClient_ErrorProcessed(Exception ex, QueryContext queryContext)
        {
            Log.Error(ex, $"PgClient_ErrorProcessed: {queryContext.Query}");
        }

        private static void PgClient_Error(Exception ex, QueryContext queryContext)
        {
            Log.Error(ex, $"PgClient_Error: {queryContext.Query}");
        }
        #endregion
    }
}