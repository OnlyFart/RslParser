using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NLog;
using RslParser.Configs;
using RslParser.Helpers;
using RslParser.Types;

namespace RslParser.Logic {
    public class RslParser {
        private static readonly Uri _baseUrl = new Uri("https://search.rsl.ru/");
        private static readonly Uri _startPage = new Uri("https://search.rsl.ru/ru/catalog#ltr=%D0%90&st=author");
        private static readonly Uri _apiUrl = new Uri("https://search.rsl.ru/site/ajax-catalog?language=ru");
        
        private static readonly Logger _logger = LogManager.GetLogger(nameof(RslParser));
        
        private readonly RslParserConfig _config;

        public RslParser(RslParserConfig config) {
            _config = config;
        }

        public async Task Process() {
            using (var client = GetClient()) {
                var errorCount = 0;

                foreach (var letter in GetLetters(await ProcessMainPage(client))) {
                    var page = letter.StartPage;
                    SearchResponse response;

                    do {
                        response = await GetSearchResponse(client, letter.Letter, page);
                        if (response == null) {
                            page++;
                            errorCount++;
                            _logger.Error($"Обработка буквы '{letter.Letter}', страница {page} завершена с ошибкой");
                            continue;
                        }

                        _logger.Info($"Обработка буквы '{letter.Letter}', страница {page}/{response.MaxPage}");
                        var links = GetBookLinks(response.Content);
                        if (links == null || links.Count == 0) {
                            errorCount++;
                            _logger.Error($"Не удалось получить список ссылок на книги для '{letter.Letter}', страница {page}/{response.MaxPage}");
                            continue;
                        }

                        foreach (var bookLink in links.Select(link => new Uri(_baseUrl, link))) {
                            var bookResponse = await HttpClientHelper.GetStringAsync(client, bookLink);
                            if (string.IsNullOrWhiteSpace(bookResponse)) {
                                errorCount++;
                                _logger.Error($"Не удалось получить контент книги {bookLink}");
                                continue;
                            }

                            _logger.Info($"{bookLink} done");

                            var bookDoc = ParseBook(bookResponse);
                            bookDoc.Page = page;
                            bookDoc.Letter = letter.Letter;
                            bookDoc.Link = bookLink.ToString();

                            await ProcessBookInfo(bookDoc);
                        }
                    } while (errorCount < _config.MaxErrorCount && (response == null || ++page <= response.MaxPage) && page <= letter.EndPage);
                }
            }
        }

        /// <summary>
        /// Обработка завязанная на главную страницу
        /// </summary>
        /// <param name="client"></param>
        /// <returns>Контент главной страницы</returns>
        /// <exception cref="Exception"></exception>
        private async Task<HtmlDocument> ProcessMainPage(HttpClient client) {
            var mainPage = await HttpClientHelper.GetStringAsync(client, _startPage);
            if (string.IsNullOrWhiteSpace(mainPage)) {
                throw new Exception($"Не удалось загрузить страницу {_startPage}");
            }
                
            var doc = new HtmlDocument();
            doc.LoadHtml(mainPage);

            var token = GetToken(doc);
            if (string.IsNullOrWhiteSpace(token)) {
                throw new Exception($"Не удалось получить токен со страницы {_startPage}");
            }
                
            client.DefaultRequestHeaders.Add("X-CSRF-Token", token);
            return doc;
        }
        
        /// <summary>
        /// Создание HttpClient'a для обхода сайта
        /// </summary>
        /// <returns></returns>
        private HttpClient GetClient() {
            var httpMessageHandler = new HttpClientHandler();
            if (_config.Proxy != null) {
                httpMessageHandler.Proxy = _config.Proxy;
            }

            return new HttpClient(httpMessageHandler);
        }
        
        /// <summary>
        /// Получение токена со страницы для запросов в API
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static string GetToken(HtmlDocument doc) {
            return doc
                .DocumentNode
                .Descendants()
                .FirstOrDefault(t => t.Name == "meta" && t.Attributes["name"]?.Value == "csrf-token")
                ?.Attributes["content"]
                ?.Value;
        }
        
        /// <summary>
        /// Получение списка доступных уникальных букв для поиска книг
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private List<SearchLetter> GetLetters(HtmlDocument doc) {
            var langs = doc.DocumentNode.Descendants().Where(t => t.Name == "div" && t.Attributes["data-langcode"]?.Value != null).ToList();
            
            if (langs == null || langs.Count == 0) {
                throw new Exception($"Не получить список языков со страницы {_startPage}");
            }

            var letters = langs.SelectMany(GetLetters).Distinct().ToList();
            if (letters.Count == 0) {
                throw new Exception($"Не удалось получить список букв для обхода");
            }

            if (_config.HasStartParams()) {
                var index = letters.FindIndex(l => string.Equals(l.Letter, _config.StartParams.Letter, StringComparison.InvariantCultureIgnoreCase));
                if (index == -1) {
                    throw new Exception($"Не удалось найти букву '{_config.StartParams.Letter}'");
                }

                letters = letters.GetRange(index, letters.Count - index);
                letters[0].StartPage = _config.StartParams.Page;
            }

            if (_config.HasEndParams()) {
                var index = letters.FindIndex(l => string.Equals(l.Letter, _config.EndParams.Letter, StringComparison.InvariantCultureIgnoreCase));
                if (index == -1) {
                    throw new Exception($"Не удалось найти букву '{_config.EndParams.Letter}'");
                }
                
                letters = letters.GetRange(0, index + 1);
                letters.Last().EndPage = _config.EndParams.Page;
            }

            return letters;
        }
        
        /// <summary>
        /// Получение списка букв языка
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static List<SearchLetter> GetLetters(HtmlNode node) {
            return node.Descendants()
                .FirstOrDefault(t => t.Name == "div")
                ?.Descendants()
                ?.Where(t => t.Name == "a" && t.Attributes["class"]?.Value?.Contains("alphacat-letter rsl-filter") == true)
                ?.Select(t => new SearchLetter(t.InnerText))
                ?.ToList();
        }

        /// <summary>
        /// Получение результатов поискового запроса
        /// </summary>
        /// <param name="client">HttpClient</param>
        /// <param name="letter">Буква</param>
        /// <param name="page">Страница</param>
        /// <returns></returns>
        private static async Task<SearchResponse> GetSearchResponse(HttpClient client, string letter, int page) {
            var list = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("SearchFilterForm[elfunds]", "0"),
                new KeyValuePair<string, string>("SearchFilterForm[nofile]", "0"),
                new KeyValuePair<string, string>("SearchFilterForm[accessFree]", "0"),
                new KeyValuePair<string, string>("SearchFilterForm[accessLimited]", "0"),
                new KeyValuePair<string, string>("SearchFilterForm[pubyearfrom]", ""),
                new KeyValuePair<string, string>("SearchFilterForm[pubyearto]", ""),
                new KeyValuePair<string, string>("SearchFilterForm[enterdatefrom]", ""),
                new KeyValuePair<string, string>("SearchFilterForm[enterdateto]", ""),
                new KeyValuePair<string, string>("SearchFilterForm[fdatefrom]", ""),
                new KeyValuePair<string, string>("SearchFilterForm[fdateto]", ""),
                new KeyValuePair<string, string>("SearchFilterForm[sortby]", "author"),
                new KeyValuePair<string, string>("SearchFilterForm[page]", page.ToString()),
                new KeyValuePair<string, string>("SearchFilterForm[letter]", letter),
                new KeyValuePair<string, string>("SearchFilterForm[searchType]", "author"),
                new KeyValuePair<string, string>("SearchFilterForm[updatedFields][]", "letter")
            };

            var dataContent = new FormUrlEncodedContent(list.ToArray());
            using (var response = await HttpClientHelper.PostAsync(client, _apiUrl, dataContent)) {
                return response == null ? null : JsonConvert.DeserializeObject<SearchResponse>(await response.Content.ReadAsStringAsync());
            }
        }
        
        /// <summary>
        /// Получение ссылок на книги
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static List<string> GetBookLinks(string html) {
            var pageDoc = new HtmlDocument();
            pageDoc.LoadHtml(html);

            return pageDoc
                .DocumentNode
                .Descendants()
                .Where(t => t.Name == "a" && t.Attributes["class"]?.Value == "rsl-modal")
                .Where(t => string.Equals(t.InnerText, "Описание", StringComparison.InvariantCultureIgnoreCase))
                .Select(t => t.Attributes["href"].Value)
                .ToList();
        }
        
        /// <summary>
        /// Парсинг страницы с книгой
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static BookInfo ParseBook(string html) {
            var bookDoc = new HtmlDocument();
            bookDoc.LoadHtml(html);

            var bookInfo = new BookInfo();
            var descriptionBlock = bookDoc.DocumentNode.Descendants().FirstOrDefault(t => t.Name == "div" && t.Attributes["class"]?.Value?.Contains("rsl-itemdescr-col") == true);

            if (descriptionBlock != null) {
                bookInfo.Authors = descriptionBlock.Descendants().FirstOrDefault(t => t.Name == "b")?.InnerText?.Trim() ?? string.Empty;
                bookInfo.Description = descriptionBlock.Descendants().FirstOrDefault(t => t.Name == "p")?.InnerText?.Split("ISBN")[0]?.Trim() ?? string.Empty;
            }

            var table = bookDoc.DocumentNode.Descendants().FirstOrDefault(t => t.Name == "table" && t.Attributes["class"]?.Value == "card-descr-table");
            BookInfoValue bookInfoValue = null;
            if (table != null) {
                foreach (var row in table.Descendants().Where(t => t.Name == "tr")) {
                    var name = row.Descendants().FirstOrDefault(t => t.Name == "th")?.InnerText?.Trim() ?? string.Empty;
                    var value = row.Descendants().FirstOrDefault(t => t.Name == "td")?.InnerText?.Trim() ?? string.Empty;

                    if (bookInfoValue == null || !string.IsNullOrWhiteSpace(name)) {
                        bookInfoValue = new BookInfoValue {
                            Name = name,
                            Value = value
                        };

                        bookInfo.Values.Add(bookInfoValue);
                    } else {
                        bookInfoValue.Value += Environment.NewLine + value;
                    }
                }
            }

            return bookInfo;
        }

        private async Task ProcessBookInfo(BookInfo bookInfo) {
            if (_config.ProcessUrl != null) {
                using (var client = new HttpClient()) {
                    await HttpClientHelper.PostAsync(client, _config.ProcessUrl, new StringContent(JsonConvert.SerializeObject(bookInfo)));
                }
            }
        }
    }
}
