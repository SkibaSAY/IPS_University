using Microsoft.ML.Data;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;
using static MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors.DeterminePredictor;

namespace MLRecommendationTendersForUsers.EstimatesForecasting.Model
{
    /// <summary>
    /// Оценка сущности, посчитанная для конкретного пользователя
    /// </summary>
    public class EntityEstimation
    {
        //LoadColumn указывает порядок подгрузки столбов из файла
        //[DisplayName("tender_id")]
        [LoadColumn(1)]//ignore
        public long TenderId { get; set; }
        
        [LoadColumn(2)]//ignore
        public long LotId { get; set; }

        [LoadColumn(1)]//ignore
        [DisplayName("Название тендера")]
        [LearningAttribute] //- без названия работает, примерно, в 1.5 раза быстрее
        public string TenderName { get; set; }

        //Label указывает на столбец, который нужно предсказать
        [DisplayName("Оценка(0-100)")]
        [LoadColumn(2), ColumnName("Label")]
        public Single Estimation { get; set; }

        [LoadColumn(3)]
        //[DisplayName("Новая oценка(0-100)")]
        public Single NewEstimation { get; set; }

        /// <summary>
        /// Ktru продуктов
        /// </summary>
        //[DisplayName("КТРУ_ids")]
        [LearningAttribute]
        [LoadColumn(10)]
        public Single[] ProductsKtru { get; set; }

        [LoadColumn(4)]
        //[DisplayName("КТРУ_codes")]
        [LearningAttribute]
        public HierarchyVector[] ProductsKtruCodes { get; set; }

        [DisplayName("КТРУ_names")]
        [LearningAttribute]
        public string ProductsKtruNames { get; set; }

        //[DisplayName("Стоимость в рублях")]
        [LoadColumn(4)]
        public Single BeginPrice { get; set; }

        [LoadColumn(7)]
        public Single RegionId { get; set; }

        //[DisplayName("Kladr региона(адрес)")]
        [LearningAttribute]
        [LoadColumn(7)]
        public HierarchyVector RegionKladr { get; set; }

        /// <summary>
        /// Id заказчика
        /// </summary>
        //[DisplayName("Id заказчика")]
        [LearningAttribute]
        [LoadColumn(5)]
        public long OrganizationId { get; set; }

        [LoadColumn(6)]
        //[DisplayName("Код деятельности заказчика")]
        [LearningAttribute]
        public HierarchyVector OrganizationOkved { get; set; }

        [DisplayName("Ссылка на тендер")]
        public string Tl2Link
        {
            get
            {
                return $"https://v2.tenderland.ru/Entity/Tender?id={TenderId}";
            }
        }
        [DisplayName("Дата начала")]
        public string BeginDate { get; set; }

        [DisplayName("Дата завершения")]
        public string EndDate { get; set; }

        [LoadColumn(2)]//ignore
        public long UserId { get; set; }

        [LoadColumn(3)]
        [LearningAttribute]
        public Single PurchaseTypeId { get; set; }

        [LearningAttribute]
        /// <summary>
        /// Квантованная по уровню цена
        /// </summary>      
        public Single BeginPriceId
        {
            get
            {
                var result = 0;
                if(BeginPrice < 150000)
                {
                    result = 0;
                }
                else if(BeginPrice < 500000)
                {
                    result = 1;
                }
                else if (BeginPrice < 1000000)
                {
                    result = 2;
                }
                else if (BeginPrice < 2000000)
                {
                    result = 3;
                }
                else if (BeginPrice < 5000000)
                {
                    result = 4;
                }
                else if (BeginPrice < 7000000)
                {
                    result = 5;
                }
                else if (BeginPrice < 10000000)
                {
                    result = 6;
                }
                else
                {
                    result = 7;
                }
                return result;
            }
        }

        [LearningAttribute]
        [LoadColumn(5)]
        public Single SysModule { get; set; }

        public EntityEstimation() { }

        #region Methods
        /// <summary>
        /// Формирует модели(включая лоты) по id тендеров
        /// </summary>
        /// <param name="pgClient"></param>
        /// <param name="tenderIds"></param>
        /// <returns></returns>
        public static List<EntityEstimation> GetByTenderIds(PgClient pgClient, IEnumerable<long> tenderIds)
        {
            var tenderEstimations = new List<EntityEstimation>();
            if (tenderIds.Any())
            {
                var query = $"SELECT l.tender_id, l.id AS lot_id FROM tenders.lots l WHERE l.tender_id IN({String.Join(",", tenderIds)})";
                tenderEstimations = pgClient.Select<dynamic>(query)
                    .Select(row => new EntityEstimation { TenderId = row.tender_id, LotId = row.lot_id }).ToList();
            }

            return tenderEstimations;
        }

        /// <summary>
        /// Заполняет служебную информацию из таблицы tenders
        /// Метод требовался бы во многих наследниках
        /// </summary>
        /// <param name="entityEstimation"></param>
        /// <exception cref="Exception"></exception>
        public static void FillEstimationsFromBaseTables(PgClient pgClient, List<EntityEstimation> estimations)
        {
            var tendersInPartCount = 5000;
            var partCount = estimations.Count / tendersInPartCount + 1;
            for (int i = 0; i < partCount; i++)
            {
                var start = i * tendersInPartCount;
                var count = estimations.Count - start;
                if(count > tendersInPartCount) count = tendersInPartCount;

                var partTenders = estimations.Slice(start, count);
                FillEstimationsFromBaseTablesByPart(pgClient, partTenders);
            }
        }
        /// <summary>
        /// не стоит передавать сюда больше нескольких тысяч в один запрос, будет очень тяжёлый
        /// </summary>
        /// <param name="pgClient"></param>
        /// <param name="estimations"></param>
        private static void FillEstimationsFromBaseTablesByPart(PgClient pgClient, List<EntityEstimation> estimations)
        {
            if (estimations.Count() == 0) return;

            var usedTenders = String.Join(',', estimations.Select(e => e.TenderId));
            var usedLots = String.Join(',', estimations.Select(e => e.LotId));
            var query =
                $@"SELECT 
                    t.id AS TenderId,
                    l.id AS LotId,
                    t.name AS TenderName,
                    t.begin_date AS BeginDate,
                    t.end_date AS EndDate,
                    t.purchase_type_id AS PurchaseTypeId,
                    t.begin_price AS BeginPrice,
                    t.sys_module AS SysModule,
                    t.region_id AS RegionId,
                    MAX(o.organization_id) AS OrganizationId,
                    array_agg(DISTINCT(COALESCE(p.ktru_id,0))) AS ProductsKtru,
                    string_agg(DISTINCT(COALESCE(kt.name,'')),'; ') AS ProductsKtruNames,
                    --array_agg(DISTINCT(COALESCE(REPLACE(SPLIT_PART(kt.code,'-', 1),'.','/'),'0'))) AS ProductsKtruCodes,
                    array_agg(DISTINCT(COALESCE(kt.path,'0'))) AS ProductsKtruCodes,
                    MAX(okv.path)::text AS OrganizationOkved,
                    k.path AS RegionKladr
                FROM tenders.tenders t 
                LEFT JOIN tenders.lots l ON t.id = l.tender_id
                LEFT JOIN tenders.organizations o ON t.id = o.tender_id AND o.lot_id = l.id AND purchase_role_id = 1
                LEFT JOIN organizations.organizations_okved o_okv ON o.organization_id = o_okv.organization_id AND is_main = true
                LEFT JOIN dictionaries.okved okv ON o_okv.okved_id = okv.id
                LEFT JOIN tenders.products p ON p.tender_id = t.id AND p.lot_id = l.id
                LEFT JOIN dictionaries.ktru kt ON p.ktru_id = kt.id
                LEFT JOIN dictionaries.kladr k ON t.region_id = k.id
                GROUP BY t.id, l.id, t.name, t.begin_date,t.end_date,t.purchase_type_id,t.begin_price,t.sys_module,t.region_id,k.path
                HAVING l.id IN ({usedLots})
                LIMIT {estimations.Count()}";
            //не учитывается: у организатора может быть несколько видов деятельности

            var addedInfo = pgClient.Select<EntityEstimation>(query).ToList();

            #region Заполнение переданных классов общими данными из tenders.tenders

            //сортировка обеих коллекций обязательна, иначе не будет работать быстрое сопоставление
            addedInfo = addedInfo.OrderBy(a => a.TenderId).ThenBy(a => a.LotId).ToList();
            estimations = estimations.OrderBy(a => a.TenderId).ThenBy(a => a.LotId).ToList();

            var estimationCurrentIndex = 0;
            for (int i = 0; i < addedInfo.Count; i++)
            {
                for (; estimationCurrentIndex < estimations.Count; estimationCurrentIndex++)
                {
                    var current = estimations[estimationCurrentIndex];
                    var currAddInfo = addedInfo[i];
                    if (currAddInfo.TenderId == current.TenderId && currAddInfo.LotId == current.LotId)
                    {
                        foreach (var prop in Properties)
                        {
                            if (prop.CanRead && prop.CanWrite && !prop.Name.Equals(nameof(Estimation)))
                            {
                                var value = prop.GetValue(currAddInfo);
                                prop.SetValue(current, value);
                            }
                        }

                        estimationCurrentIndex++;
                        break;
                    }
                    else if (currAddInfo.TenderId < current.TenderId)
                    {
                        break;
                    }
                }
            }

            #endregion
        }

        public static PropertyInfo[] Properties =
            (from x in typeof(EntityEstimation).GetProperties() select x).ToArray();

        /// <summary>
        /// Отображаемые свойства обьекта
        /// </summary>
        public static (PropertyInfo propInfo, string displayName)[] DisplayedProperties = 
            (from x in Properties
             where x.GetCustomAttributes(typeof(DisplayNameAttribute), false).Length > 0
            select (x, 
                (x.GetCustomAttributes(typeof(DisplayNameAttribute), false).First() as DisplayNameAttribute).DisplayName
             )
            ).ToArray();

        /// <summary>
        /// Свойства, используемые при обучении
        /// </summary>
        public static PropertyInfo[] LearningProperties = Properties.Where(p => p.GetCustomAttribute(typeof(LearningAttribute)) != null).ToArray();
        
        /// <summary>
        /// Свойства типа Single
        /// </summary>       
        public static PropertyInfo[] SingleProperties = LearningProperties.Where(p => p.PropertyType.Equals(typeof(Single))).ToArray();
        
        /// <summary>
        /// Свойства типа string - требуют векторизации
        /// </summary>
        public static PropertyInfo[] StringProperties = LearningProperties.Where(p => p.PropertyType.Equals(typeof(string))).ToArray();
        
        /// <summary>
        /// Массивы требуют особого подхода к векторизации - с использованием параметра Bag: https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.transforms.onehotencodingestimator?view=ml-dotnet
        /// </summary>
        public static PropertyInfo[] IEnumerableProperties = LearningProperties.Where(p => p.PropertyType.GetInterface(nameof(IEnumerable)) != null).ToArray();
        #endregion
    }

    public class EstimationPredict
    {
        [ColumnName("Score")]
        public Single Estimation;

        /// <summary>
        /// Содержит описание, из которого ясно, как получена оценка
        /// </summary>
        public PredictResult PredictResult;
    }
}
