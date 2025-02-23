using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.SimplePredictors;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.IEstimatesPredictor;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors
{
    public class DeterminePredictor : IEstimatesPredictor
    {

        private List<PredictorBase> Predictors;
        public DeterminePredictor()
        {
            InitPredictors();
            ResetWeight();
        }
        private void InitPredictors()
        {
            var tenderNamePredictor = new StringPredictor(nameof(EntityEstimation.TenderName), maxItemsInGroup: 7, minItemsInGroup: 3) { MaxWeight = 0.2 };
            var regionPredictor = new HierarchyPredictor(nameof(EntityEstimation.RegionKladr)){MaxWeight = 0.5};
            var organizationIdPredictor = new IdPredictor(nameof(EntityEstimation.OrganizationId));
            var organizationOkvedPredictor = new HierarchyPredictor(nameof(EntityEstimation.OrganizationOkved));
            
            var ktruCodesPredictor = new HierarchyArrayPredictor(nameof(EntityEstimation.ProductsKtruCodes)) { MaxWeight = 0.7 };
            var ktruNamesPredictor = new StringPredictor(nameof(EntityEstimation.ProductsKtruNames), maxItemsInGroup: 7, minItemsInGroup: 2);

            var ktruGroupPredictor = new GroupPredictor(ktruCodesPredictor, ktruNamesPredictor) { MaxWeight = 0.7 };
            var organizationGroupPredictor = new GroupPredictor(organizationIdPredictor, organizationOkvedPredictor) { MaxWeight = 0.3, PropertyName = nameof(EntityEstimation.OrganizationOkved) };
            Predictors = new List<PredictorBase>()
            {
                //new IdPredictor(nameof(EntityEstimation.OrganizationId)){MaxWeight = 0.3},
                //new HierarchyPredictor(nameof(EntityEstimation.OrganizationOkved))/*{MaxWeight = 0.2}*/,
                //new ArrayPredictor<Single>(nameof(EntityEstimation.ProductsKtru)),
                //,
                //organizationGroupPredictor,
                regionPredictor,
                ktruCodesPredictor,
                ktruNamesPredictor,
                //ktruGroupPredictor,
                //tenderNamePredictor,
                //TenderNamePredictor,
                //групповой анализатор плохо себя показывает, название выдаёт много мусора
                //new GroupPredictor(KtruCodesPredictor,TenderNamePredictor){MaxWeight = 0.7},
            };
        }
        private void ResetWeight()
        {
            //var predictorsCount = Predictors.Count();
            //Predictors.ForEach(predictor => { predictor.Weight = 1.0 / predictorsCount; });
            var min = 0.1;
            var max = 1 / Math.Pow(3, 0.5);
            var random = new Random();
            Predictors.ForEach(predictor =>
            {
                predictor.Weight = random.NextDouble() * (max - min) + min;
                if(predictor.Weight > predictor.MaxWeight) predictor.Weight = predictor.MaxWeight;
            });
        }
        private void SetWeight(Dictionary<PredictorBase, double> newWeights)
        {
            Predictors.ForEach(predictor => { predictor.Weight = newWeights[predictor]; });
        }

        private Dictionary<PredictorBase, double> GetCurWeights()
        {
            return Predictors.ToDictionary(p => p, p => p.Weight);
        }


        public string GenerateSavePath(long userId, string cacheDirPath)
        {
            return $"{cacheDirPath}/{userId}.json";
        }

        /// <summary>
        /// about learn https://habr.com/ru/articles/714988/
        /// </summary>
        /// <param name="learnindData"></param>
        public void Learn(List<EntityEstimation> learnindData)
        {
            //Подгонка весов происходит по данным, отличным от загруженной выборки
            //var prepared = PreparedData<EntityEstimation>.Prepare(learnindData, 0.5);
            //var testedData = prepared.TestData;
            LoadData(learnindData);
            DropStrangeItemsFromHistory();

            //запоминаем текущие параметры
            var bestWeights = GetCurWeights();

            double bestRsquared = Test(learnindData).RSquared;
            double bestLearnStep = 0;

            for(var curEpoch = 0; curEpoch < 10; curEpoch++)
            {
                var currWeights = GetCurWeights();

                //TODO: есть смысл отобрать самые частые шаги, чтобы не прогонять их все
                //0.5 самый оптимальный и частый шаг обучения в наших моделях
                //var learningSteps = new List<double>() { 10,5,2,1,0.5 };
                var learningSteps = new List<double>() { 5 };
                foreach (var learnStep in learningSteps)
                {
                    //TODO: есть ли смысл несколько раз пробегаться по данным?
                    for(var i = 0; i < 1; i++)
                    {
                        foreach (var learnItem in learnindData)
                        {
                            WeightCorrection(learnItem, learnStep);
                        }
                        //для отладки
                        var tempWeigth = GetCurWeights();
                    }

                    var testResult = Test(learnindData);
                    if (bestRsquared < testResult.RSquared)
                    {
                        bestRsquared = testResult.RSquared;
                        bestLearnStep = learnStep;
                        bestWeights = GetCurWeights();
                    }

                    //cбрасываем к начальным параметрам эпохи
                    SetWeight(currWeights);
                }
                //Генерируем параметры новой эпохи
                ResetWeight();
            }
            
            //заполняем лучшие веса
            Predictors.ForEach(p => { p.Weight = bestWeights[p]; });
            //дозагружаем тестовые данные 
            //LoadData(testedData);
            //повторно очищаем странные данные, тк догрузили новые
            //DropStrangeItemsFromHistory();
        }
        /// <summary>
        /// Добавляет данные в модель, веса не изменяются
        /// </summary>
        /// <param name="data"></param>
        public void LoadData(List<EntityEstimation> data)
        {
            foreach (var predictor in Predictors)
            {
                var filteredData = data.Where(l => predictor.Filter(l)).ToList();
                predictor.Load(filteredData);
            }
        }

        public bool Filter(EntityEstimation entity)
        {
            return Predictors.All(p => p.Filter(entity));
        }
        public void DropStrangeItemsFromHistory()
        {
            //TODO:подумать, можем не стоит удалять данные, которые не используем, а накапливать их, и помечать IsUse
            foreach (var predictor in Predictors)
            {
                predictor.DropStrangeItemsFromHistory();
            }
        }

        #region WeightRecorrection
        private void WeightCorrection(EntityEstimation learnItem, double learnStep)
        {
            var predictResult = PredictResults(learnItem);
            var totalO = predictResult.TotalKf;

            var totalError = learnItem.Estimation / 100 - totalO;
            var sigmoida = Sigmoida(totalO);

            var deltaPart = sigmoida * (1 - sigmoida) * totalO * learnStep;

            //неправильный подход - нужно учитывать все
            //var usedPredictors = predictResult.UsedPredictors;


            //var usedPredictors = predictResult.Items.Keys;
            var usedPredictors = predictResult.UsedPredictors;
            
            //в случае, когда ошибка отрицаительна, мы поднимаем веса
            //нет смысла поднимать веса тем, кто достиг лимита
            if(totalO < 0)
            {
                usedPredictors = usedPredictors.Where(p => p.Weight <= p.MaxWeight).ToList();
            }

            var weightSum = usedPredictors.Sum(up => up.Weight);

            foreach (var predictor in usedPredictors)
            {
                var currentError = totalError * predictor.Weight / weightSum;

                //var deltaW = -currentError * deltaPart;
                var deltaW = currentError * deltaPart;

                predictor.Weight += deltaW;
                if(predictor.Weight > predictor.MaxWeight) predictor.Weight = predictor.MaxWeight;
            }
        }

        private double Sigmoida(double value)
        {
            return 1 / (1 + Math.Exp(value));
        }
        #endregion

        public void Load(string path)
        {
            var json = File.ReadAllText(path);
            Predictors = JsonConvert.DeserializeObject<List<PredictorBase>>(json, JsonSettings);
        }

        public PredictResult PredictResults(EntityEstimation entityEstimation)
        {
            var predictResult = new PredictResult();
            foreach (var predictor in Predictors)
            {
                if (predictor.Filter(entityEstimation))
                {
                    var prInfo = predictor.Estimate(entityEstimation);
                    predictResult.Add(prInfo);
                }
            }
            return predictResult;
        }
        public EstimationPredict Predict(EntityEstimation entityEstimation)
        {
            var predictResult = PredictResults(entityEstimation);

            var result = new EstimationPredict();
            var newEstimation = 100 * predictResult.TotalKf;
            result.Estimation = (float)newEstimation;

            result.PredictResult = predictResult;
            return result;
        }

        private static JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
        };
        public void Save(string path)
        {
            var json = JsonConvert.SerializeObject(Predictors,Formatting.Indented,JsonSettings);
            File.WriteAllText(path, json);
        }

        public TestResult Test(List<EntityEstimation> testData)
        {
            var points = new List<RSquaredPoint>();
            foreach(var estimation in testData)
            {
                var point = new RSquaredPoint();
                point.TrueResult = estimation.Estimation;
                point.PredictedResult = Predict(estimation).Estimation;

                points.Add(point);
            }

            //результат может быть хуже, тк мы не смогли найти зависимости, из-за которых пользователь участвовал
            //-Насколько хорошо мы оценили те данные, которые смогли определить
            //-Но в основном, мы смотрим на общую картину, учитывая то, что не смогли сравнить
            //points = points.Where(p=>p.PredictedResult != 0).ToList();
            var result = new TestResult { RSquared = CalculateRSquared(points) };
            return result;
        }
        
        private double CalculateRSquared(List<RSquaredPoint> points)
        {
            //https://en.wikipedia.org/wiki/Coefficient_of_determination
            //В
            //var _y = points.Average(p => p.TrueResult);
            //костыль:
            //var _y = 0;
            var _y = 50; 
            var SS_res = points.Sum(p => Math.Pow(p.TrueResult - p.PredictedResult, 2));
            var SS_tot = points.Sum(p => Math.Pow(p.TrueResult - _y, 2));

            var rsquared = 1 - SS_res / SS_tot;
            return rsquared;
        }

        private class RSquaredPoint
        {
            public double TrueResult;
            public double PredictedResult;
            public override string ToString()
            {
                return $"true: {TrueResult.ToString("##.##")}, predict: {PredictedResult.ToString("##.##")}";
            }
        }

        public class PredictResult
        {

            public Dictionary<PredictorBase, PredictInfo> Items = new Dictionary<PredictorBase, PredictInfo>();
            public double TotalKf
            {
                //В случае, если одного их параметров нет, то 
                get
                {
                    var sum = Items.Sum(kvp =>
                    {
                        var predictor = kvp.Key;
                        var kf = kvp.Value.Kf;

                        var result = predictor.Weight * kf.Value;

                        return result;
                    });
                    return sum;
                }
            }
            public List<PredictorBase> UsedPredictors => Items.Where(kvp => !kvp.Value.Kf.IsDefault)
                .Select(kvp=>kvp.Key).ToList();
            public void Add(PredictInfo prInfo)
            {
                Items.Add(prInfo.Predictor, prInfo);
            }

            public override string ToString()
            {
                return TotalKf.ToString();
            }
        }
    }
}
