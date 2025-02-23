using MLRecommendationTendersForUsers.EstimatesForecasting;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimateSearcher;
using MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictor.Models;
using MLRecommendationTendersForUsers.EstimatesForecasting.Model;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RecommendedTendersForUsers.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tenderland.Database.PostgreSql;
using static MLRecommendationTendersForUsers.EstimatesForecasting.EstimationPredictors.DeterminePredictors.DeterminePredictor;

namespace RecommendedTendersForUsers
{
    public static class UserRecommendationReport
    {
        static string CacheDirName = "Report";
        static UserRecommendationReport()
        {
            if (Directory.Exists(CacheDirName))
            {
                Directory.Delete(CacheDirName, true);
            }
            Directory.CreateDirectory(CacheDirName);
        }
        public static FileInfo CreateReport(PgClient pgClient, User user, bool ignoreAutosearch = false)
        {
            var recommendations = GetUserRecommendations(pgClient, user);

            if (ignoreAutosearch)
            {
                //только те тендеры, которые не попали в автопоиск
                recommendations = IgnoreTendersInAutosearch(pgClient, recommendations, user);
            }

            //заполняем только после определения, какие id нам нужны, иначе это очень тяжёлый запрос
            EntityEstimation.FillEstimationsFromBaseTables(pgClient, recommendations);


            return ExportToExcel(recommendations, user);
        }

        public static List<EntityEstimation> GetEntitiesByLastDays(PgClient pgClient, long dayCount = 3)
        {
            var query = $"SELECT t.id AS TenderId, l.id AS LotId FROM tenders.tenders t JOIN tenders.lots l ON l.tender_id = t.id\r\nWHERE t.publish_date > now() - interval '{dayCount}' day";
            var tenders = pgClient.Select<EntityEstimation>(query).ToList();
            return tenders;
        }

        public static List<EntityEstimation> IgnoreTendersInAutosearch(PgClient pgClient, List<EntityEstimation> estimations, User user)
        {
            var autoSearch = new EstimationByAutosearch(pgClient);
            var autoSearchTenders = autoSearch.FindEstimations(user.Id);

            var unAutosearchTenders = estimations.ExceptBy(autoSearchTenders.Select(a=>a.TenderId), e => e.TenderId).ToList();
            return unAutosearchTenders;
        }

        public static FileInfo ExportToExcel(List<EntityEstimation> recommendations, User user, Dictionary<EntityEstimation, PredictResult> descriptions = null, string postfix = "Analyzed")
        {
            var saveFileName = $"{CacheDirName}/{user.Name}_{user.Id}_{postfix}.xlsx";

            FileInfo newExcelFile = new FileInfo(saveFileName);

            //Сортировка по Убыванию, обеспечивает порядок сортировки в листах
            recommendations.Sort(Comparer<EntityEstimation>.Create((e1, e2) => (-1) * e1.Estimation.CompareTo(e2.Estimation)));
            //временное поле
            //recommendations.Sort(Comparer<EntityEstimation>.Create((e1, e2) => (-1) * e1.NewEstimation.CompareTo(e2.NewEstimation)));

            //Epplus 4.5.3.3 версия с открытой лицензией - не обновляйте, более старшие версии не запустятся бесплатно
            using (var package = new ExcelPackage())
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets.Add("Отчёт");

                sheet.Cells[1, 1].Value = $"Пользователь: {user.Name}";
                sheet.Cells[2, 1].Value = $"Дата составления: {DateTime.Now.ToString()}";
                sheet.Cells[3, 1].Value = $"Тендеры: ({recommendations.Count} шт)";

                ExcelRange range = sheet.Cells[1, 1, 3, 1];

                range.Style.Font.Name = "Times New Roman";
                range.Style.Font.Size = 10;
                range.AutoFitColumns(MinimumWidth: 20, 50);

                var worksheetsPredication = new List<(string worksheetName, Predicate<EntityEstimation> filter)>
                {
                    //("<20", (EntityEstimation e) => e.Estimation < 20),
                    //("20-50", (EntityEstimation e) => e.Estimation >= 20 && e.Estimation <=50),
                   //("51-79", (EntityEstimation e) => e.Estimation >= 51 && e.Estimation <=79),
                    //("80-90", (EntityEstimation e) => e.Estimation >= 80 && e.Estimation <=90),
                    //(">90", (EntityEstimation e) => e.Estimation > 90)
                    ("Рекомендации", (EntityEstimation e) => e.Estimation >= 80)
                };

                var worksheetLimit = 1000;
                worksheetsPredication.ForEach(w =>
                {
                    var workSheetRows = recommendations.Where(r => w.filter.Invoke(r)).ToList();
                    var limit = worksheetLimit;
                    var count = workSheetRows.Count();
                    if (count < worksheetLimit)
                    {
                        limit = count;
                    }
                    workSheetRows = workSheetRows.Take(limit).ToList();

                    AddWorksheet(package, w.worksheetName, workSheetRows, descriptions);
                });

                package.SaveAs(newExcelFile);
            }
            return newExcelFile;
        }

        /// <summary>
        /// Добавляет в Excel лист с оценками тендеров
        /// </summary>
        /// <param name="excel"></param>
        /// <param name="worksheetName"></param>
        /// <param name="recommendations">порядок будет соответствовать переданному порядку</param>
        private static void AddWorksheet(ExcelPackage excel, string worksheetName, List<EntityEstimation> recommendations, Dictionary<EntityEstimation, PredictResult> descriptions = null)
        {
            ExcelWorksheet sheet = excel.Workbook.Worksheets.Add(worksheetName);

            var columnNum = 1;
            var displayedProps = EntityEstimation.DisplayedProperties;
            foreach (var prop in displayedProps)
            {
                var isArray = prop.propInfo.PropertyType.IsArray;

                //заголовок колонки, как на форме - то, что написано в атрибуте DisplayName
                sheet.Cells[1, columnNum].Value = prop.displayName;

                //заполняем колонку, пробегая по статистикам
                for (int i = 0; i < recommendations.Count; i++)
                {
                    var current = recommendations[i];

                    string value = "";
                    if (isArray)
                    {
                        if(prop.propInfo.PropertyType == typeof(HierarchyVector[]))
                        {
                            var tempValue = prop.propInfo.GetValue(current);
                            value = String.Join(", ", String.Join(" | ", ((HierarchyVector[])tempValue).Select(v=>v.ToString())));
                        }
                        else
                        {
                            value = String.Join(", ", (Single[])prop.propInfo.GetValue(current));
                        }                      
                    }
                    else
                    {
                        value = prop.propInfo.GetValue(current)?.ToString();
                    }
                    var curRange = sheet.Cells[i + 2, columnNum];
                    curRange.Value = value;

                    //если в описании указано, что было использовано поле, то подсвечиваем
                    var isGreenLight =
                        descriptions != null && descriptions.TryGetValue(current, out PredictResult predictDesc)
                        && predictDesc.UsedPredictors.Any(predictor => predictor.TargetProperty.Name.Equals(prop.propInfo.Name));
                    
                    if (isGreenLight)
                    {
                        curRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        curRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.GreenYellow);
                    }
                }
                columnNum++;
            }

            ExcelRange range = sheet.Cells[1, 1, recommendations.Count + 2, displayedProps.Count()];

            range.Style.Font.Name = "Times New Roman";
            range.Style.Font.Size = 10;
            range.AutoFitColumns(MinimumWidth:20,50);
        }

        public static List<EntityEstimation> GetUserRecommendations(PgClient pgClient, User user)
        {
            //Выделяем только текущие действительные тендеры
            var query = 
                @$"SELECT ml.user_id AS UserId, ml.tender_id AS TenderId, ml.lot_id AS LotId, ml.estimation AS Estimation
                FROM machine_learning.user_tender_recommendation ml 
                    JOIN tenders.tenders t ON ml.tender_id = t.id AND ml.user_id = {user.Id}
                ORDER BY Estimation DESC
                LIMIT 1000000";

            var recommendations = pgClient.Select<EntityEstimation>(query).ToList();

            //Отбрасываем то, что пользователь уже получал в автопоисках или видел
            //var userController = new UserEstimationController(pgClient);
            //var userSeeIt = userController.EstimateSearchers.SelectMany(c=>c.FindEstimations(user.Id)).ToList();
            //var userSeeItIds = userSeeIt.Select(e => e.TenderId).ToList();
            //var userNotSeeIt = recommendations.Where(r=>!userSeeItIds.Contains(r.TenderId)).Except(userSeeIt).ToList();
            //return userNotSeeIt;

            return recommendations;
        }
    }
}
