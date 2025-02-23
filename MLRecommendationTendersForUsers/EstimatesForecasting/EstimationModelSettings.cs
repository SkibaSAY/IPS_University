using MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting
{
    public static class EstimationModelSettings
    {
        public const int MinimumCountToLearn = 10;
        /// <summary>
        /// Не стоит делать значение больше 20к - слишком много данных, это будет работать дольше и нагрузка вызрастёт
        /// Пока считаю, что стоит брать последние 500 записей из группы, иначе мы можем взять слишком много и терять точность
        /// </summary>
        public const int MaximuxCountInLearningGroup = 500;

        public const int MaximumAnalizeThreads = 5;
        
        /// <summary>
        /// Минимальная оценка для доступа к анализу
        /// </summary>
        public const string MinRSquared = "0.1";

        /// <summary>
        /// Минимальное значение для сохранения
        /// </summary>
        public const int MinEstimationToSave = (int)EstimateScoreEnum.Famous;
    }
}
