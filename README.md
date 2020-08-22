# RslParser
Инструмент для парсинга библиотеки сайта https://search.rsl.ru/

Для работы необходим net.core 3.1 https://dotnet.microsoft.com/download/dotnet-core/3.1

А так же утилита https://wkhtmltopdf.org/downloads.html#stable

Пример вызова сервиса
```
rslparser --sl Г --sp 10 --proxy 127.0.0.1:8888
```
Где --sl - буква, с которой начинается обход библиотки, --sp - страница, с которой начинается обход библиотеки

Без передачи параметров происходит полный обход

Для полного списка опций вызвать 

```
rslparser --help
```