using MLRecommendationTendersForUsers.EstimatesForecasting;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using RecommendedTendersForUsers.Models;
using RecommendedTendersForUsers.Models.Rabbit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;
using Tenderland.Database.PostgreSql.Bulk;
using Tenderland.MessageQueue;

namespace RecommendedTendersForUsers
{
    public class Controller
    {
        RabbitMQClient MqClient;
        PgClient PgClient;
        QueryBuffer Buffer;
        List<User> Users;

        /// <summary>
        /// Оптимизация: Ограничивает минимальный Id тендеров, которые анализируются, иначе пропускаются(тк старые)
        /// </summary>
        long MinTenderIdToCheck;

        //public UserEstimationController<MLEstimationPredictor> UserEstimationController;
        public UserEstimationController<DeterminePredictor> UserEstimationController;
        string CacheDir = Program.CacheDir;
        string ModelsDir = Program.ModelsDir;
        public Controller(RabbitMQClient mqClient,PgClient pgClient)
        {
            this.MqClient = mqClient;
            this.PgClient = pgClient;

            UserEstimationController = new UserEstimationController<DeterminePredictor>(pgClient);
            //буффер ограничен числом хранимых параметров - 65к
            //TODO:написать динамический подсчёт макс числа строк на основе параметров
            //Утверждается, что внутри стоит проверка и автосброс, если параметров становится больше
            Buffer = new QueryBuffer(PgClient,batchSize: 10000);

            Users = LoadUsers();
            InitTasks();
        }

        #region Tasks
        Task[] Tasks;
        void InitTasks()
        {
            Tasks = new Task[]
            {
                Task.Run(UpdateUsers),
                Task.Run(UpdateMinTenderIdToCheck)
            };

            //чтобы задачи успели хотя бы по разу выполниться
            Thread.Sleep(5000);
        }
        /// <summary>
        /// чтобы в случае, когда мы уже обновили данные о пользовательских моделях, мы использовали модели новых пользователей
        /// </summary>
        void UpdateUsers()
        {
            while (true)
            {
                //раз в час
                Thread.Sleep(3600000);
                lock (Users)
                {
                    Users = LoadUsers();
                }
            }
        }
        void UpdateMinTenderIdToCheck()
        {
            while (true)
            {
                try
                {
                    MinTenderIdToCheck = GetLastTenderIdFromDb() - GlobalSettings.MaxDistanceToLastTenderId;
                }
                catch(Exception ex)
                {
                    Log.Error(ex, "Ошибка при попытке обновить номер самого свежего тендера");
                }

                //раз в час
                Thread.Sleep(3600000);
            }
        }
        #endregion

        long GetLastTenderIdFromDb()
        {
            return PgClient.Select<long>("SELECT MAX(t.id) FROM tenders.tenders t").First();
        }

        static string queue = "user_recommendation";
        public void Start()
        {
            Console.WriteLine("Анализ пользовательских рекомендаций запущен");

            MqClient.UseChannel(nameof(RecommendedTendersForUsers), channel =>
            {
                //TODO:Для обеспечения возможности оценки 60к тендеров в день для 8к клиентов
                //необходимо 10-15 потоков при скорости 350-400 тендеров в секунду на одного клиента
                channel.DelegateBatchAction<AfterParseMessage>(batchSize: 1000, messages =>
                {
                    ProcessMessage(messages);
                });
                channel.ListenMessage(queue);
            });
        }
        
        private void ProcessMessage(IReadOnlyList<Message<AfterParseMessage>> messages)
        {
            try
            {
                //TODO: раскомментировать
                var tenders = messages.Where(m => m.Body.Type == EntityType.TENDER && m.Body.Id >= MinTenderIdToCheck);

                //формируем батч тендеров
                //группируем, тк вполне могут повторяться тендеры в сообщениях, тк это идёт после парсинга
                var batch = new MessagesBatch { TenderIds = tenders.GroupBy(t=>t.Body.Id).Select(g => g.Key).Distinct().ToList() };

                if(batch.TenderIds.Count() > 0)
                {
                    var timer = new Stopwatch();
                    Console.WriteLine("Начинаю обработку пакета");
                    timer.Start();

                    ProcessBatch(batch);
                    timer.Stop();
                    Console.WriteLine($"Завершил обработку пакета за {timer.Elapsed}");
                    //batch.TenderIds.ForEach(id => Log.Information("Тендер {entity_id} обработан", id));
                }
            }
            catch(Exception ex)
            {
                //В случае, если ошибки нет, только тогда мы отпускаем сообщение
                //, иначе приложение падает и нужно смотреть, что случилось 
                Log.Error(ex, "Критическая ошибка обработки пакета");
                throw;
            }

            foreach (var message in messages)
            {
                message.AckMessage();
            }
        }

        private void ProcessBatch(MessagesBatch batch)
        {
            lock (Users) 
            {
                var tenderEstimations = EntityEstimation.GetByTenderIds(PgClient, batch.TenderIds);

                EntityEstimation.FillEstimationsFromBaseTables(PgClient, tenderEstimations);

                //Иначе оценка может быть выдана ошибочно, тк нужные поля не указаны
                tenderEstimations = UserEstimationController.FilterData(tenderEstimations);

                if (tenderEstimations.Count == 0) return;

                Parallel.ForEach(Users, new ParallelOptions { MaxDegreeOfParallelism = EstimationModelSettings.MaximumAnalizeThreads },
                user =>
                {
                    //var stopwatch = new Stopwatch();
                    //stopwatch.Start();
                    //обработка пакета моделью пользователя
                    try
                    {                     
                        var userModel = user.Model;

                        foreach (var tenderEstimation in tenderEstimations)
                        {
                            var recommendation = new UserTenderRecommendation()
                            {
                                UserId = user.Id,
                                TenderId = tenderEstimation.TenderId,
                                LotId = tenderEstimation.LotId,
                                Status = RecommendationStatus.Success
                            };
                            try
                            {
                                var predict = userModel.Predict(tenderEstimation);
                                recommendation.Estimation = predict.Estimation;
                            }
                            catch (Exception ex)
                            {
                                //пока не используется
                                recommendation.Status = RecommendationStatus.Error;
                            }

                            //добавляем в буффер только хорошие оценки - не нужно забивать базу мусором
                            if(recommendation.Estimation >= EstimationModelSettings.MinEstimationToSave)
                            {
                                AddToBuffer(recommendation);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Log.Error(ex,"Произошла ошибка при обработке пакета тендеров для пользователя: {user_id}", user.Id);

                        ////Ошибка обработки батча - все необработанные результаты помечаются как ошибочные
                        //var unproccessedTenderIds = tenderEstimations.Select(e => e.TenderId)
                        //    .Except(tenderRecommendations.Select(t => t.TenderId));
                        //var unprocessedRecommendations = unproccessedTenderIds.Select(t => new UserTenderRecommendation
                        //{
                        //    UserId = user.Id,
                        //    TenderId = t,
                        //    Status = RecommendationStatus.BatchError
                        //});
                        ////добавляем необработанные из-за ошибка результаты
                        //tenderRecommendations.AddRange(unprocessedRecommendations);

                        //в случае падения обработки батча - разработчик должен смотреть
                        throw;
                    }
                    //логирование отключено, тк слишком сильно заваливает лог
                });

                //сохранение
                Buffer.Execute();
            }           
        }

        private void AddToBuffer(UserTenderRecommendation recommendation)
        {
            //обязательно нужно дропать для каждого пользователя, иначе будут конфликты
            var postfix = $"{recommendation.UserId}_{recommendation.TenderId}_{recommendation.LotId}";

            var userIdPar = new QueryParameter($"user_id_{postfix}", recommendation.UserId);
            var tenderIdPar = new QueryParameter($"tender_id_{postfix}", recommendation.TenderId);
            var lotIdPar = new QueryParameter($"lot_id_{postfix}", recommendation.LotId);
            var estimationPar = new QueryParameter($"estimation_{postfix}", (int)recommendation.Estimation);

            var query = @$"INSERT INTO machine_learning.user_tender_recommendation(user_id,tender_id,lot_id,estimation)
                 VALUES(@{userIdPar.Name},@{tenderIdPar.Name},@{lotIdPar.Name},@{estimationPar.Name})
                ON CONFLICT(user_id,tender_id, lot_id) 
                DO UPDATE 
                SET estimation = EXCLUDED.estimation;";
            Buffer.Add(query, new List<QueryParameter> { userIdPar, tenderIdPar, lotIdPar, estimationPar});
        }

        /// <summary>
        /// Занимает около 10 минут для 2к моделей
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private List<User> LoadUsers()
        {
            var users = new List<User>();

            //users = User.All(PgClient).ToList();
            //users = User.AllTest(PgClient).ToList();
            //users = User.AllToAnalyze(PgClient).ToList();
            users = User.AllOnDesk().ToList();

            //TODO: проверяем, что модели есть на диске - потом это переписать, проверка будет не нужна
            var simplePredictor = UserEstimationController.CreatePredictor();
            users = users.Where(u=>File.Exists(simplePredictor.GenerateSavePath(u.Id, Program.ModelsDir))).ToList();

            //Модель должна загружаться 1 раз, а затем использоваться
            //при загрузке модели при обработке каждого батча мы теряем 90% времени
            users.ForEach(u =>
            {
                u.Model = UserEstimationController.CreatePredictor();
                var savedPath = u.Model.GenerateSavePath(u.Id, ModelsDir);
                u.Model.Load(savedPath);
            });

            return users;
        }
    }
}
