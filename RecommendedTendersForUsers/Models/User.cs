using Microsoft.ML;
using MLRecommendationTendersForUsers.EstimatesForecasting;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;

namespace RecommendedTendersForUsers.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Inn { get; set; }

        public IEstimatesPredictor Model;

        public static IEnumerable<User> AllToLearning(PgClient pgClient)
        {
            var userQuery = $@"SELECT DISTINCT parent_id AS Id FROM system.users WHERE last_login > now() - interval '1 month'";
            var users = pgClient.Select<User>(userQuery);

            return users;
        }

        /// <summary>
        /// Из базы извлекаются все пользователи с неплохими моделями
        /// Не учитывает нахождение модели на диске
        /// </summary>
        /// <param name="pgClient"></param>
        /// <returns></returns>
        public static IEnumerable<User> AllToAnalyze(PgClient pgClient)
        {
            var userQuery = $@"SELECT user_id AS Id FROM machine_learning.estimates_forecasting ef 
                            WHERE ef.source_data_count >= {EstimationModelSettings.MinimumCountToLearn}
                                AND ef.rsquared > {EstimationModelSettings.MinRSquared}";
            var users = pgClient.Select<User>(userQuery);

            return users;
        }

        /// <summary>
        /// Извлекает всех пользователей, у которых на диске есть модели
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<User> AllOnDesk()
        {
            var savedModels = new DirectoryInfo(Program.ModelsDir).GetFiles().Select(f => f.Name.Replace(".json", "")).ToList();
            var users = savedModels.Select(sm => new User { Id = int.Parse(sm)});

            return users;
        }
        /// <summary>
        /// Метод для получения Альфа-тестеров
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<User> AllTest(PgClient pgClient)
        {
            var userQuery = $@"
            SELECT u.id AS Id,u.user_name AS Name FROM SYSTEM.users u 
            WHERE u.id IN(17349, 10687, 2390, 627, 3942,3449)";
            var users = pgClient.Select<User>(userQuery);

            return users;
        }
    }
}
