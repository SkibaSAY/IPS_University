using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLRecommendationTendersForUsers.EstimatesForecasting
{
    /// <summary>
    /// Используется для автоматического формирования обучающего PipeLine на основе аттрибутов полей модели
    /// </summary>
    internal class LearningAttribute:Attribute
    {
        public LearningAttribute() { }
    }
}
