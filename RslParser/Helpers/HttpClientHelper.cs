using System;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;

namespace RslParser.Helpers {
    public class HttpClientHelper {
        private static readonly Logger _logger = LogManager.GetLogger(nameof(HttpClientHelper));
        
        public static async Task<HttpResponseMessage> GetAsync(HttpClient client, Uri url) {
            for (var i = 0; i < 3; i++) {
                try {
                    return await client.GetAsync(url);
                } catch (Exception e) {
                    _logger.Error(e);
                }
            }

            return null;
        }
        
        public static async Task<string> GetStringAsync(HttpClient client, Uri url) {
            for (var i = 0; i < 3; i++) {
                try {
                    return await client.GetStringAsync(url);
                } catch (Exception e) {
                    _logger.Error(e);
                }
            }

            return null;
        }

        public static async Task<HttpResponseMessage> PostAsync(HttpClient client, Uri url, ByteArrayContent data) {
            for (var i = 0; i < 3; i++) {
                try {
                    return await client.PostAsync(url, data);
                } catch (Exception e) {
                    _logger.Error(e);
                }
            }

            return null;
        }
    }
}
