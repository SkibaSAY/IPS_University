using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Transforms;
using Microsoft.ML;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
using static Microsoft.ML.DataOperationsCatalog;
using static MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.IEstimatesPredictor;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors
{
    public class MLEstimationPredictor : IEstimatesPredictor
    {
        /// <summary>
        ///Create ML Context with seed for repeteable/deterministic results
        ///seed заданное значение для генератора случайных чисел.
        ///заданный сид обеспечивает один и тот же результат при каждом запуске на одних данных
        /// </summary>
        static MLContext mlContext = new MLContext(seed: 0);
        private static IEstimator<ITransformer> TrainedPipeline;
        ITransformer Model;
        PredictionEngine<EntityEstimation, EstimationPredict> PredictEngine;
        //Костыльное создание схемы
        static DataViewSchema InputSchema = mlContext.Data.LoadFromEnumerable(new List<EntityEstimation>()).Schema;

        static MLEstimationPredictor()
        {
            TrainedPipeline = CreateTrainingPipeline();
        }

        private static IEstimator<ITransformer> CreateTrainingPipeline()
        {
            // STEP 2: Common data process configuration with pipeline data transformations
            //фильтрация недопустимых значений
            //IDataView trainingDataView = mlContext.Data.FilterRowsByColumn(baseTrainingDataView, nameof(EntityEstimation.Estimation), lowerBound: (int)EstimateScoreEnum.Hidden, upperBound: (int)EstimateScoreEnum.Winner);
            //var estimator = mlContext.Transforms.DropColumns(nameof(EntityEstimation.TenderId),nameof(EntityEstimation.UserId));
            //trainingDataView = estimator.Fit(trainingDataView).Transform(trainingDataView);

            IEstimator<ITransformer> pipeline = null;
            pipeline = mlContext.Transforms
                .CopyColumns(outputColumnName: "Label", inputColumnName: "Label");

            //позволяет перевести не числовые значения в числовые - модель не может работать не с числами
            //.Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: nameof(EntityEstimation.Code)))
            //нормировка в нашем случае не нужна, тк  FastTree это не требует             
            //.Append(mlContext.Transforms.NormalizeMeanVariance(outputColumnName: nameof(EntityEstimation.BeginPrice)))
            //.Append(mlContext.Transforms.Text.FeaturizeText("Vector_Categories", nameof(EntityEstimation.Categories)))
            //.Append(mlContext.Transforms.Concatenate("Features", nameof(EntityEstimation.PurchaseTypeId), nameof(EntityEstimation.SysModule), nameof(EntityEstimation.BeginPriceId), "Vector_Categories", nameof(EntityEstimation.RegionId), nameof(EntityEstimation.OrganizationId)))
            //.Append(mlContext.Regression.Trainers.FastForest(options));

            //Для указания, какие поля использовать при обучении
            var features = new List<string>();

            //1.Single добавляются сразу, без преобразования
            features.AddRange(EntityEstimation.SingleProperties.Select(p => p.Name));

            //2.string требует преобразования в вектор через FeaturizeText
            foreach (var prop in EntityEstimation.StringProperties)
            {
                //pipeline = pipeline.Append(mlContext.Transforms.Text.FeaturizeText(vectorName, prop.Name));

                //оптимизация алгоритма: https://devindeep.com/how-to-transform-data-in-ml-net/
                //нормализация строки, удаление лишних слов и тд.
                pipeline = pipeline
                    .Append(mlContext.Transforms.Text.NormalizeText($"NormalizedText_{prop.Name}", prop.Name, keepPunctuations: false))
                    .Append(mlContext.Transforms.Text.TokenizeIntoWords($"Tokens_{prop.Name}", $"NormalizedText_{prop.Name}"))
                    .Append(mlContext.Transforms.Text.RemoveDefaultStopWords($"NoStopWords_{prop.Name}", $"Tokens_{prop.Name}"))
                    .Append(mlContext.Transforms.Conversion.MapValueToKey($"Key_{prop.Name}", $"NoStopWords_{prop.Name}"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding($"Vector_{prop.Name}", $"Key_{prop.Name}", OneHotEncodingEstimator.OutputKind.Bag));
                //не забывать добавлять
                features.Add($"Vector_{prop.Name}");
            }

            //3.IEnumerable: векторизуются особым образом
            foreach (var enunerableProp in EntityEstimation.IEnumerableProperties)
            {
                var vectorName = $"IEnumerable_{enunerableProp.Name}";
                //Про Bag:https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.transforms.onehotencodingestimator?view=ml-dotnet
                pipeline = pipeline.Append(
                    mlContext.Transforms.Categorical.OneHotEncoding(vectorName, enunerableProp.Name, OneHotEncodingEstimator.OutputKind.Bag)
                );

                //не забывать добавлять
                features.Add(vectorName);
            }

            var featuresArr = features.ToArray();
            //Features - нужно явно добавить все преобразованные колонки в модель
            pipeline = pipeline.Append(mlContext.Transforms.Concatenate("Features", featuresArr));

            var options = new FastForestRegressionTrainer.Options
            {
                // Only use 80% of features to reduce over-fitting 
                //FeatureFraction = 0.8,
                // Simplify the model by penalizing usage of new features 
                //FeatureFirstUsePenalty = 0.1,
                // Limit the number of trees to 50 
                //NumberOfTrees = 50,

                //получено после тестирования предложенных алгоритмов
                NumberOfTrees = 4,
                NumberOfLeaves = 4,
                FeatureFraction = 1F
            };
            //алгоритм обучения
            //pipeline = pipeline.Append(mlContext.Regression.Trainers.FastForest(options));

            pipeline = pipeline.Append(mlContext.Regression.Trainers.FastTree());

            return pipeline;
        }

        /// <summary>
        /// Показывает какие параметры оказали влияние на результат - запускать только в отладке
        /// Не передавать много данных(больше 1к) - очень долго будет анализировать
        /// </summary>
        /// <param name="model"></param>
        /// <param name="trainingDataView"></param>
        private void ShowMetricPermutation(ITransformer model, IDataView trainingDataView)
        {
            trainingDataView = model.Transform(trainingDataView);
            // Calculate feature importance
            var pfiResults =
                mlContext
                    .Regression
                    .PermutationFeatureImportance(model, trainingDataView, permutationCount: 3);

            // Order features by importance
            var featureImportanceMetrics =
                pfiResults
                    .Select((metric, index) => new { index, metric.Value.RSquared })
                    .OrderByDescending(myFeatures => Math.Abs(myFeatures.RSquared.Mean));

            Console.WriteLine("Feature\tPFI");

            foreach (var feature in featureImportanceMetrics)
            {
                Console.WriteLine($"{pfiResults.ToArray()[feature.index].Key}|\t{feature.RSquared.Mean:F6}");
            }

        }

        /// <summary>
        /// Из модели формирует Engine для получения оценок
        /// Это тяжёлая операция, раз в 10 тяжелее, чем Predict, 
        /// поэтому нужно хранить её в памяти и не собирать каждый раз
        /// </summary>
        /// <param name="trainedModel"></param>
        /// <returns></returns>
        private PredictionEngine<EntityEstimation, EstimationPredict> CreatePredictEngineFromModel(ITransformer trainedModel)
        {
            // Create prediction engine related to the loaded trained model
            return mlContext.Model.CreatePredictionEngine<EntityEstimation, EstimationPredict>(trainedModel);
        }

        #region HelpMethods
        private IDataView CreateDataViewFromIEnumerable(IEnumerable<EntityEstimation> entityEstimations)
        {
            return mlContext.Data.LoadFromEnumerable<EntityEstimation>(entityEstimations);
        }
        private IEnumerable<EntityEstimation> LoadIEnumerableFromIDataView(IDataView dataView)
        {
            return mlContext.Data.CreateEnumerable<EntityEstimation>(dataView, false);
        }
        #endregion

        private ITransformer LoadModel(long userId, string cacheDirPath, out bool success)
        {
            var path = GenerateSavePath(userId, cacheDirPath);
            success = File.Exists(path);

            if (success)
            {
                return LoadModel(path);
            }
            return null;
        }
        private ITransformer LoadModel(string path)
        {
            var model = mlContext.Model.Load(path, out DataViewSchema schema);
            return model;
        }

        private ITransformer CreateTrainedModel(List<EntityEstimation> trainingData)
        {
            //базовая https://learn.microsoft.com/ru-ru/dotnet/machine-learning/tutorials/predict-prices
            //более подробная https://habr.com/ru/companies/jugru/articles/495208/
            // STEP 1: Common data loading configuration
            IDataView trainingDataView = CreateDataViewFromIEnumerable(trainingData);

            var transformedData = TrainedPipeline.Fit(trainingDataView).Transform(trainingDataView);

            var model = CreateTrainedModel(trainingDataView);

            //ShowMetricPermutation(model, trainingDataView);
            return model;
        }

        private ITransformer CreateTrainedModel(IDataView trainingDataView)
        {
            var trainedModel = TrainedPipeline.Fit(trainingDataView);

            return trainedModel;
        }

        private RegressionMetrics TestTrainedModel(IDataView testDataView)
        {
            IDataView predictions = Model.Transform(testDataView);

            //как аналог можно использовать более точную оценку  mlContext.Regression.CrossValidate
            //, но оценка точности по времени в 5 раз дольше
            var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

            //кф детерминации (0.0 - 1.0) чем ближе к 1, тем лучше модель, уже хорошо 0.8-0.9
            //Console.WriteLine($"*       RSquared Score:      {metrics.RSquared:0.##}");
            //среднеквадратичное отклонение, чем меньше, тем лучше
            //Console.WriteLine($"*       Root Mean Squared Error:      {metrics.RootMeanSquaredError:#.##}");
            return metrics;
        }
        private RegressionMetrics TestTrainedModel(IEnumerable<EntityEstimation> testData)
        {
            IDataView testDataView = CreateDataViewFromIEnumerable(testData);
            return TestTrainedModel(testDataView);
        }

        private TrainTestData PrepareLearningAndTestData(List<EntityEstimation> entityEstimations, double testFraction = 0.2)
        {
            IDataView dataView = CreateDataViewFromIEnumerable(entityEstimations);
            return PrepareLearningAndTestData(dataView, testFraction);
        }

        /// <summary>
        /// Разбивает данные на две случайные кучи
        /// </summary>
        /// <param name="data"></param>
        /// <param name="learningProbability">
        /// 0.0 - 1.0 Соответствует вероятности попадания элемента исходных данных в обучающую выборку
        /// </param>
        /// <param name="learnData"></param>
        /// <param name="testData"></param>
        private TrainTestData PrepareLearningAndTestData(IDataView data, double testFraction = 0.2)
        {
            var trainDataSet = mlContext.Data.TrainTestSplit(data, testFraction: testFraction);
            return trainDataSet;
        }

        private void UpdatePredictEngine()
        {
            PredictEngine = CreatePredictEngineFromModel(Model);
        }

        //PUBLIC

        public string GenerateSavePath(long userId, string cacheDirPath)
        {
            return $"{cacheDirPath}/{userId}.zip";
        }
        public EstimationPredict Predict(EntityEstimation entityEstimation)
        {
            return PredictEngine.Predict(entityEstimation);
        }
        public void Learn(List<EntityEstimation> learnindData)
        {
            Model =  CreateTrainedModel(learnindData);
            UpdatePredictEngine();
        }

        public TestResult Test(List<EntityEstimation> testData)
        {
            var metrics = TestTrainedModel(testData);
            var result = new TestResult();
            result.RSquared = metrics.RSquared;
            return result;
        }

        public void Load(string path)
        {
            Model = LoadModel(path);
            UpdatePredictEngine();
        }

        public void Save(string savePath)
        {
            mlContext.Model.Save(Model, InputSchema, savePath);
        }
    }
}
