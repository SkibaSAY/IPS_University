using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shared
{
    public class RussWordNormalizer
    {
        private static Regex WordRegex = new Regex($@"\w+");

        //Окончания
        //https://www.slovorod.ru/russian-inflexions.html?ysclid=lyi7whx05a545895535
        private static string RussEndsSource = @"-а: сущ. ед. ч. им., род., вин. п.; мн.ч. им. и вин. п.; мест. ед. ч. им., род., вин. п.; прил. ед. ч. им., род. вин. п.; кр. прил.; числ. им. п.; род., дат., тв., предл. п.; вин. п.; глаг. прош. вр.
-ам: сущ. мн. ч. дат. п.; мест. мн. ч. дат. п.; числ. дат. п.
-ами: сущ. мн. ч. тв. п.; мест. мн. ч. тв. п.; числ. тв. п.
-ас: мест. мн. ч. род., вин., предл. п.
-am: глаг. наст.-буд. вр.
-ax: сущ. мн. ч. предл. п.; числ. предл. п.
-ая: прил. ед. ч. им. п.; сущ. ед. ч. им. п.
-е: сущ. ед. ч. им., вин. п.; ед. ч. дат. п.; ед. ч. предл. п.; мн. ч. им. п.; мест. ед. ч. дат., предл. п.; прил. мн. ч. им., вин. п.; числ. им., вин. п.,
-её: мест. ед. ч. род., вин. п.
-ей: сущ. мн. ч. род. п. ; мн. ч. вин. п.; ед. ч. род., дат., тв., предл. п.; мест. ед. ч. род., дат., тв., предл. п.; прил. ед. ч. род., дат., тв., предл. п.,
-ем: сущ. ед. ч. тв. п.; мест. ед. ч. тв. п.; прил. ед. ч. тв. п.; мн. ч. дат. п.
-еми: прил. мн. ч. тв. п.
-емя: числ. тв. п.
-ex: прил. мн. ч. род., вин., предл. п.
-ею: мест. ед. ч. тв. п.; прил. ед. ч. тв. п.; сущ. ед. ч. тв. п.
-ёт: глаг. наст.-буд. вр. ,
-ёте: глаг. наст.-буд. вр. ,
-ёх: числ. род., вин., предл. п.
-ёшь: глаг. наст.-буд. вр. ,
-и: сущ. ед. ч. род. п.; ед. ч. дат. п.; ед. ч. вин. п., ед. ч. предл. п.; мн.ч. им. п. ; мн. ч. вин. п.; мест. мн. ч. им. п.; прил. мн. ч. им., вин. п.; кр. прил.;: числ. им., вин. п.; род., дат., предл. п.; глаг. повел. накл.; прош. вр. ,
-ие: прил. мн. ч. им., вин. п.; сущ. мн. ч. им., вин. п.
-ий: прил. ед. ч. им. п.; ед. ч. вин. п.; сущ. ед. ч. им., вин. п.
-им: мест. ед. ч. тв. п.; мн. ч. дат. п.; прил. ед. ч. тв. п.; мн. ч. дат. п.; сущ. ед. ч. тв. п.; мн. ч. дат. п.; числ. дат. п.;: глаг. наст.-буд. вр.
-ими: мест. мн. ч. тв. п.; прил. мн. ч. тв. п.; сущ. мн. ч. тв. п. : числ. тв. п.
-ит: глаг. наст.-буд. вр.
-ите: глаг. наст.-буд. вр.
-их: мест. мн. ч. род., вин., предл. п.; прил. мн. ч. род., вин., предл. п.; сущ. мн. ч. род., вин., предл. п.; числ. род., вин., предл. п.
-ишь: глаг. наст.-буд. вр.
-ию: сущ. ед. ч. тв. п.
-|jу|: сущ. ед. ч. тв. п.; числ. тв. п.
-м: глаг. наст.-буд. вр.,
-ми: сущ. мн. ч. тв. п.
-мя: числ. тв. п.
-о: сущ. ед. ч., им. п.; ед. ч. вин. п.; мест. ед. ч. им., вин. п.; прил. ед. ч., им., вин. п.; кр. прил.; числ. им., вин. п.; глаг. прош. вр.
-ов: сущ. мн. ч. род. п. ; мн. ч. вин. п.,
-ого: мест. ед. ч. род., вин. п.; прил. ед. ч. род., вин. п.; сущ. ед. ч. род., вин. п.,
-ое: прил. ед. ч. им., вин. п.; сущ. ед. ч. им., вин. п.
-оё: прил. ед. ч. вин. п.
-ой: сущ. ед. ч. тв. п.; ед. ч. им., вин. п.; ед. ч. род., дат., предл. п.; мест. ед. ч. тв. п.; прил. ед. ч. им. п. ед. ч. род., дат., тв., предл. п.; ед. ч. вин. п.
-ом: сущ. ед. ч. тв. п.; ед. ч. предл. п.; мест. ед. ч. предл. п.; прил. ед. ч. предл. п.; числ. дат. п.; глаг. наст.-буд. вр. ,
-ому: мест. ед. ч. дат. п.; прил. ед. ч. дат. п.; сущ. ед. ч. дат. п.,
-ою: сущ. ед. ч. тв. п.; мест. ед. ч. тв. п.; прил. ед. ч. тв. п. ,
-cm: глаг. наст.-буд. вр.,
-у: сущ. ед. ч. род. п.; ед. ч. дат. п.; ед. ч. вин. п.; ед. ч. предл. п.; прил. ед. ч. дат. п.; ед. ч. вин. п.; глаг. наст.-буд. вр.
-ум: числ. дат. п.
-умя: числ. тв. п.
-ут: глаг. наст.-буд. вр. ,
-ух: числ. род., вин., предл. п.
-ую: прил. ед. ч. вин. п.; сущ. ед. ч. вин. п.
-шь: глаг. наст.-буд. вр.,
-ых:,
-ый:,
-ия:";
        public static string[] RussEnds = Regex.Matches(RussEndsSource, @"-(?<end>\w+):").Select(m => m.Groups["end"].ToString()).ToArray();

        //Суффиксы https://uchi.ru/otvety/questions/vse-suffiksi-v-russkom-yazike + суффиксы прилагательных
        public static string[] RussSuffics = Regex.Matches("-К-, -ИШК-, -УШК-, -ЮШК-, -ОНЬК-, -ЕНЬК-, -ОЧК-, -ЕЧК-, -ИСТ-, -Н-, -ЩИК-, -ЁР-, -НИК-, -ЧИК-, -ТЕЛЬ-, -ЯК-, -АРЬ-, -ИР-, -ИЧ-, -СК-, -ИНК-, -ОНОК, -ЁНОК, -АТ-, -ЯТ-, -ИЦ-, -ИХ-, -ЕЦ-, -ИК-, -ОК-, -ЫШК-, -НИЦ-, -ЁНК-, -ОНК-, -ЛИВ-, -ОВ-, -ОВАТ-, -ЕВАТ-, -ЕНН-, -ОНН-, -ИН-, -АНН-, -АН-, -ЯНН-, -ЯН-", @"-(?<suff>\w+)-", RegexOptions.IgnoreCase)
    .Select(m => m.Groups["suff"].ToString().ToLower()).ToArray();

        public class RussWord()
        {
            public string Root = "";
            public string Suffics = "";
            public string End = "";

            public bool IsFinal = false;
            public bool IsAbbreviation = false;

            public override string ToString()
            {
                return $"{Root}{Suffics}{End}";
            }
        }

        //ничего страшного в том, что кэш очистится полностью
        //главное, чтобы при обработке текст не обрабатывался много раз при обучении
        private const int MaxCacheLen = 1000000;
        private static Dictionary<string, RussWord> CacheWords = new Dictionary<string, RussWord>();

        public static List<RussWord> NormalizeWords(string text)
        {
            //операция затратная, поэтому проверяем закешированные рузультаты для слов

            //1.разбиваем на слова
            var preparedWords = WordRegex.Matches(text).Select(m => new RussWord { Root = m.Value.ToString()}).ToList();

            for (var i = 0; i < preparedWords.Count; i++)
            {
                //1.0 Проверка кэша
                var word = preparedWords[i];
                //на данном этапе в Root лежит всё слово
                if (CacheWords.TryGetValue(word.Root, out RussWord value))
                {
                    preparedWords[i] = value;
                    continue;
                }

                //2.Выделить аббревиатуры, например: МФУ
                if (word.Root.Length > 1 && word.Root.All(ch => Char.IsUpper(ch)))
                {
                    word.IsAbbreviation = true;
                    word.Root = word.Root.ToLower();
                }
                //2.1 Выделяем слова, длина которых меньше 5 символов, например: корм
                else if(word.Root.Length < 5)
                {
                    word.Root = word.Root.ToLower();
                }
                else
                {
                    word.Root = word.Root.ToLower();
                    //3.Отрезать окончания, если они есть
                    if (TryGetEnd(word.Root, out string end))
                    {
                        //кроме окончания
                        word.End = end;
                        word.Root = word.Root.Substring(0, word.Root.Length - end.Length);
                    }

                    //4.Отрезать суффиксы, если они есть
                    if (TryGetSuffics(word.Root, out string suff))
                    {
                        //кроме Суффиксов
                        word.Suffics = suff;
                        word.Root = word.Root.Substring(0, word.Root.Length - suff.Length);
                    }
                }

                //5.Кэшируем
                //ничего страшного в том, что кэш очистится полностью
                if (CacheWords.Count > MaxCacheLen)
                {
                    CacheWords.Clear();
                }

                //Обязательно проставляем IsFinal - иначе наша оптимизация не будет работать
                word.IsFinal = true;

                //на данном этапе в Root лежит только корень, поэтому берём слово целиком
                var key = word.ToString();
                if (!CacheWords.ContainsKey(key))
                {
                    CacheWords.Add(key, word);
                }
            }

            return preparedWords;
        }

        private static bool TryGetEnd(string currentRoot, out string result)
        {
            result = "";
            foreach(var end in RussEnds)
            {
                if(currentRoot.EndsWith(end) && end.Length > result.Length)
                {
                    result = end;
                }
            }

            return result.Length > 0;
        }
        private static bool TryGetSuffics(string currWordWithoutEnd, out string result)
        {
            result = "";
            foreach (var end in RussSuffics)
            {
                if (currWordWithoutEnd.EndsWith(end) && end.Length > result.Length)
                {
                    result = end;
                }
            }
            return result.Length > 0;
        }
    }
}
