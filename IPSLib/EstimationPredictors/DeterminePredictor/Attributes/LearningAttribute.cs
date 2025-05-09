using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPSLib.EstimationPredictors.DeterminePredictors
{
    /// <summary>
    /// Используется для автоматического формирования обучающего PipeLine на основе аттрибутов полей модели
    /// </summary>
    public class LearningAttribute:Attribute
    {
        public LearningAttribute() { }
    }

    /// <summary>
    /// Аттрибут данных представления: обучающие и вспомогательные, не используемые в обучении
    /// </summary>
    public class ViewAttribute : Attribute
    {
        public ViewAttribute() { }
    }
}
