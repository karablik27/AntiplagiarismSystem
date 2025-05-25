# AntiplagiarismSystem

## 🔍 Описание

**AntiplagiarismSystem** — это микросервисное .NET 7 приложение для анализа .txt-отчётов студентов на наличие повторов (плагиата) и вычисления статистики с генерацией облака слов.

## 🌐 Архитектура системы

* **API Gateway**

  * Точка входа для клиентов
  * Проксирует запросы к сервисам
  * Swagger UI со всеми API

* **FileStoringService**

  * Принимает файлы .txt
  * Хранит на диске (+ метаданные в PostgreSQL)
  * Отдаёт файлы по id

* **FileAnalysisService**

  * Считает:

    * кол-во абзацев
    * слов
    * символов
  * Обнаруживает 100% копии (через SHA256)
  * Генерирует PNG облака слов (QuickChart.io)

## 📂 Стек технологий

* C#, .NET 7
* ASP.NET Core Web API
* Entity Framework Core + PostgreSQL
* Swagger / OpenAPI
* Docker + Docker Compose
* xUnit для тестов

## 📅 Сценарии пользователя

1. Загрузка текстового отчёта
2. Автопроверка на 100% копию
3. Анализ статистики: слова, абзацы, символы
4. Генерация облака слов
5. Повторный анализ идёт из кэша

## 📁 Спецификация API

### FileStoringService

| Endpoint           | Method | Description         |
| ------------------ | ------ | ------------------- |
| `/files/store`     | POST   | Загрузка .txt файла |
| `/files/file/{id}` | GET    | Скачать файл по ID  |

### FileAnalysisService

| Endpoint                         | Method | Description                |
| -------------------------------- | ------ | -------------------------- |
| `/files/analysis/{id}/start`     | POST   | Запуск анализа файла       |
| `/files/analysis/{id}`           | GET    | Получить результат анализа |
| `/files/analysis/{id}/wordcloud` | GET    | Получить PNG-облако слов   |

### API Gateway

| Endpoint                         | Method | Proxy to                 |
| -------------------------------- | ------ | ------------------------ |
| `/files/store`                   | POST   | `FileStoringService`     |
| `/files/file/{id}`               | GET    | `FileStoringService`     |
| `/files/analysis/{id}/start`     | POST   | `FileAnalysisService`    |
| `/files/analysis/{id}`           | GET    | `FileAnalysisService`    |
| `/files/analysis/{id}/wordcloud` | GET    | `FileAnalysisService`    |
| `/health`                        | GET    | API Gateway health check |

## ✅ Тесты

* Покрытие > 80%
* Юнит-тесты на:

  * `AnalysisService`
  * `AnalysisController`
  * `FilesController`

🛠️ Инструкция по запуску

Требуется установленный Docker + Docker Compose

# Клонируем репозиторий
git clone https://github.com/karablik27/AntiplagiarismSystem.git
cd AntiplagiarismSystem

# Запуск всех микросервисов
docker compose up --build

После запуска доступны:

Gateway Swagger: http://localhost:5005/swagger/index.html

File Storing Swagger: http://localhost:5017/swagger/index.html

File Analysis Swagger: http://localhost:5002/swagger/index.html


