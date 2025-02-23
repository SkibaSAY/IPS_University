using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Transforms;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using Nest;
using Tenderland.Database.PostgreSql;
using static Microsoft.ML.DataOperationsCatalog;

namespace MLRecommendationTendersForUsers.EstimatesForecasting
{
    public class UserEstimationController<T> where T: IEstimatesPredictor, new()
    {
        private PgClient pgClient;

        public UserEstimationController(PgClient pgClient)
        {
            this.pgClient = pgClient;

            InitSearchers();
        }

        public void SaveLearningDataToFile(string path, List<EntityEstimation> entityEstimations)
        {
            var separator = "\t";
            var props = EntityEstimation.LearningProperties.Select(p=>p).ToList();
            props.Add(typeof(EntityEstimation).GetProperty(nameof(EntityEstimation.Estimation)));

            using (var sw = new StreamWriter(path))
            {
                var headers = System.String.Join(separator, props.Select(p => p.Name));
                sw.WriteLine(headers);

                foreach(var e in entityEstimations)
                {
                    var row = System.String.Join(separator, props.Select(p => p.GetValue(e)));
                    sw.WriteLine(row);
                }
            }
        }

        public List<EstimateSearcherBase> EstimateSearchers;
        private EstimateUnknown EstimateUnknown;
        private void InitSearchers()
        {
            EstimateSearchers = new List<EstimateSearcherBase>
            {
                new EstimationsFamous(pgClient),
                //new EstimationByAutosearch(pgClient),
                new EstimationsByApplication(pgClient),
                new EstimationsByElastic(pgClient)
            };

            //используется отдельно, тк ему нужно передавать число строк, тк должен быть баланс хороших/плохих оценок
            //EstimateUnknown = new EstimateUnknown(pgClient);
        }

        public List<EntityEstimation> LoadLearningDataForUser(long userId)
        {
            var result = new List<EntityEstimation>();

            EstimateSearchers.ForEach(s =>
            {
                result.AddRange(s.FindEstimations(userId));
            });

            //возможна ситуации, что будут дубли с общими данными, но разной оценкой - нужна группировка
            result = result
                .GroupBy(e => new { TenderId = e.TenderId, LotId = e.LotId })
                .Select(g => new EntityEstimation
                {
                    TenderId = g.Key.TenderId,
                    LotId = g.Key.LotId,
                    Estimation = g.Max(t => t.Estimation)
                }).ToList();

            //если данных нет, то ничего не делаем
            if (result.Count == 0) return result;

            //Попытаемся понять, сколько данных нужно в каждой группе

            //эти данные наиболее ценны, их явно меньше, чем автопоисков, поэтому по ним мы выбираем число элементов в группе
            var maxInteresingGroup = result.Where(r => r.Estimation >= (int)EstimateScoreEnum.Famous);
            
            //столько данных мы возьмём из каждой группы
            var groupItemsCount = maxInteresingGroup.Count();

            //в группе не должно быть слишком много, иначе запрос заполнения Fill, будет очень долгий
            if(groupItemsCount > EstimationModelSettings.MaximuxCountInLearningGroup)
            {
                groupItemsCount = EstimationModelSettings.MaximuxCountInLearningGroup;
            }

            //В каждой группе отбираем groupItemsCount последних элементов
            result = result.GroupBy(r => r.Estimation)
                .SelectMany(g => g.OrderByDescending(t => t.TenderId).Take(groupItemsCount))
                .ToList();


            //заполняем данными для корректной работы EstimateUnknown
            EntityEstimation.FillEstimationsFromBaseTables(pgClient, result);

            //не исключено, что в систему попадает мусор, например, тендеры, которые уже удалены из базы, но в таблице есть
            result = FilterData(result);

            //используется отдельно, тк ему нужно передавать число строк, тк должен быть баланс хороших/плохих оценок
            //группировка не нужна, тк это другие тендеры
            //var unknownData = EstimateUnknown.FindEstimations(userId, result, limit: groupItemsCount);
            
            //var filterUnknownData = FilterData(unknownData);
            
            //EntityEstimation.FillEstimationsFromBaseTables(pgClient, unknownData);

            //result.AddRange(unknownData);

            return result;
        }

        public List<EntityEstimation> FilterData(List<EntityEstimation> entityEstimations)
        {   
            var result = new List<EntityEstimation>();
            foreach(var e in entityEstimations)
            {
                if(e.TenderId != 0 && (e.ProductsKtru != null && e.ProductsKtru.First() != 0 
                    || !String.IsNullOrEmpty(e.TenderName)))
                {
                    result.Add(e);
                }
            }
            return result;
        }

        public PreparedData<EntityEstimation> PrepareLearningAndTestData(List<EntityEstimation> estimations)
        {
            return PreparedData<EntityEstimation>.Prepare(estimations);
        }

        public IEstimatesPredictor CreatePredictor()
        {
            return new T();
        }
    }
}
