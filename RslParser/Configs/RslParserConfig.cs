using System;
using System.Net;

namespace RslParser.Configs {
    public class RslParserConfig {
        /// <summary>
        /// Стартовые параметры для обхода
        /// </summary>
        public Params StartParams;
        
        /// <summary>
        /// Конечные параметры для обхода
        /// </summary>
        public Params EndParams;
        
        /// <summary>
        /// Максимальное кол-во ошибок после которых парсинг остановится
        /// </summary>
        public int MaxErrorCount;
        
        /// <summary>
        /// Прокси для запросов
        /// </summary>
        public WebProxy Proxy;
        
        /// <summary>
        /// Url для отправки данных по книгам
        /// </summary>
        public Uri ProcessUrl;
        
        /// <summary>
        /// Указаны ли стартовые параметра
        /// </summary>
        /// <returns></returns>
        public bool HasStartParams() {
            return HasParams(StartParams);
        }
        
        /// <summary>
        /// Указаны ли конечные параметры
        /// </summary>
        public bool HasEndParams() {
            return HasParams(EndParams);
        }

        private bool HasParams(Params param) {
            return param != null && !string.IsNullOrWhiteSpace(param.Letter) && param.Page >= 1;
        }
    }

    public class Params {
        /// <summary>
        /// Буква
        /// </summary>
        public string Letter;
        
        /// <summary>
        /// Страница
        /// </summary>
        public int Page;
    }
}
