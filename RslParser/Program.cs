using System;
using System.Net;
using CommandLine;
using Microsoft.Extensions.Configuration;
using RslParser.Configs;

namespace RslParser {
    class Program {
        static void Main(string[] args) {
            ServicePointManager.DefaultConnectionLimit = 100;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => {
                    IConfiguration appConfig = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", true, true)
                        .Build();

                    var processUrl = appConfig.GetValue<string>("ProcessUrl");
                    
                    var config = new RslParserConfig {
                        StartParams = new Params {
                            Letter = options.StartLetter, 
                            Page = options.StartPage
                        }, 
                        EndParams = new Params {
                            Letter = options.EndLetter, 
                            Page = options.EndPage
                        },
                        MaxErrorCount = options.MaxErrorCount,
                        ProcessUrl =  string.IsNullOrWhiteSpace(processUrl) ? null : new Uri(processUrl)
                    };

                    if (!string.IsNullOrWhiteSpace(options.Proxy)) {
                        var split = options.Proxy.Split(":");
                        if (split.Length != 2) {
                            Console.Error.WriteLine("Параметр proxy передан в неверном формате");
                            return;
                        }

                        if (!int.TryParse(split[1], out var port)) {
                            Console.Error.WriteLine("Порт proxy должен быть числом");
                            return;
                        }
                        
                        Console.WriteLine($"Используем прокси Host={split[0]} Port={port}");
                        config.Proxy = new WebProxy(split[0], port);
                    }

                    if (config.HasStartParams()) {
                        Console.WriteLine($"Стартуем с буквы '{config.StartParams.Letter}' и страницы '{config.StartParams.Page}'");
                    }
                    
                    if (config.HasEndParams()) {
                        Console.WriteLine($"Заканчиваем на букве '{config.EndParams.Letter}' и странице '{config.EndParams.Page}'");
                    }

                    if (!config.HasStartParams() && !config.HasEndParams()) {
                        Console.WriteLine("Параметров не передано, будет произведен полный обход");
                    }

                    new Logic.RslParser(config).Process().Wait();
                    
                    Console.WriteLine("Обработка завершена");
                })
                .WithNotParsed(options => {});
        }
    }
}