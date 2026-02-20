# Search Orchestrator Service

Сервис-оркестратор поиска. Управляет индексацией файлов через внешний Search Engine и предоставляет API для поиска.

## Структура

Clean Architecture, 4 проекта:

- **Domain** — сущности, enum ы, value objects
- **Application** — интерфейсы, сервисы, DTO
- **Infrastructure** — in-memory реализации, фейковый SearchEngine, фоновый процессор
- **Api** — контроллеры, middleware, Program.cs

## API

| Метод | Маршрут | Описание |
|-------|---------|----------|
| POST | `/api/indexing/tasks` | Запуск индексации (202) |
| GET | `/api/indexing/tasks/{taskId}` | Статус задачи |
| GET | `/api/indexing/tasks?sourceId=...` | Список задач |
| POST | `/api/indexing/tasks/{taskId}/cancel` | Отмена |
| POST | `/api/search` | Поиск |

## Запуск

```bash
dotnet run --project src/SearchOrchestrator.Api
```

## Тесты

```bash
dotnet test
```

31 тест (unit + интеграционные через WebApplicationFactory).
