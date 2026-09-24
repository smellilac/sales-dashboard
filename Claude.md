# Sales Performance Dashboard — Claude.md

Рабочий документ проекта. Здесь инварианты, зафиксированные решения, Draft и открытые вопросы.
Подробности и причины каждого решения — в `docs/decisions.md`.

**Статусы:**
- **settled** — решение принято, не пересматривать.
- **Draft** — идея есть, но перед реализацией её нужно подтвердить ещё раз. Не реализовывать.
- **open** — решения ещё нет.
- **TODO** — отложено на отдельный этап.
- **rejected** — рассмотрено и отклонено.

---

## О проекте

Тестовое задание DJI-Market.ru: dashboard аналитики продаж менеджеров (B2B).
Timebox — 8 часов. Запуск одной командой: `docker compose up --build`.

**Домен коротко:** менеджеры продают клиентам-компаниям. Сделка (Sale) — «шапка чека»:
менеджер, клиент, дата, статус (Paid / Cancelled / Refunded). Позиции (SaleItem) — строки чека:
товар, количество, цена продажи, себестоимость. Деньги есть только в SaleItem, статус и дата —
только в Sale.

## Стек

- Backend: .NET 10, C#, ASP.NET Core Minimal APIs, EF Core + Npgsql, Dapper
- Data: PostgreSQL (поднимается как сервис в docker-compose проекта)
- Frontend: React + TypeScript (детали — open)
- Infrastructure: Docker Compose

## Инварианты (settled)

1. Dashboard API только читает данные. Единственная запись — `SalesDbInitializer` при старте
   (миграции + seed).
2. Все вычисления и фильтрация — на сервере. Frontend не загружает продажи для пересчёта.
3. Одно бизнес-правило живёт в одном месте. Если правило приходится повторить в SQL (Dapper)
   и в C# (EF Core), это закрыто тестом согласованности (CONSISTENCY-TEST).
4. Ожидаемые ошибки (например, невалидный период) — `ErrorOr`. Исключения — только для
   инфраструктурных сбоев.
5. Валюта одна — рубли. Поля валюты нет.
6. Первичные ключи не несут бизнес-смысла.

## Модель данных (settled)

| Сущность | Поля |
|---|---|
| Manager | `id`, `first_name`, `last_name`, `team` (строка), `position`, `is_active` |
| Customer | `id`, `contact_name`, `company`, `segment` (Enterprise / SMB / Government) |
| Category | `id`, `name` |
| Product | `id`, `sku` (уникальный), `name`, `category_id`, `list_price`, `base_cost`, `version_at` (`timestamptz`) |
| Sale | `id`, `manager_id`, `customer_id`, `sold_at` (`timestamptz`, UTC), `status` |
| SaleItem | `id`, `sale_id`, `product_id`, `product_version_at`, `quantity` (`int`), `unit_price`, `unit_cost`, `line_revenue` (generated), `line_cost` (generated) |

- Все `id` — `uuid`, значения UUID v7 (`Guid.CreateVersion7()`).
- Деньги — `numeric(18,2)`.
- Именование в БД — snake_case (`EFCore.NamingConventions`).
- Инициалы и цвет аватара не хранятся: инициалы из имени, цвет считает frontend по `id`.
- `status` и `segment` — C# enum, в БД строка (`HasConversion<string>()`) + `CHECK`-ограничение, список значений строится из enum (`Enum.GetNames<T>()`), не дублируется вручную.

## Бизнес-правила (settled)

| ID | Правило |
|---|---|
| D1 | Refunded = как Cancelled: не даёт выручки, прибыли и продажи. Отдельная KPI-карточка «Возвраты»: сумма и доля от Paid + Refunded. |
| D2 | Продажа = только Paid. Количество продаж = число Paid. Средний чек = выручка / число Paid (по сделкам, не по позициям). Таблица последних продаж показывает все статусы; суммы Cancelled/Refunded серые и не входят в итоги. |
| D3 | API принимает только `from`/`to`. Предыдущий период = окно той же длины сразу перед выбранным, всегда (для «этого месяца» тоже). Считает сервер, возвращает даты обоих периодов. Пресеты в даты превращает frontend в бизнес-часовом поясе. |
| D4 | `SoldAt` — `timestamptz` в UTC. Дни считаются в `ReportingOptions.TimeZone` (по умолчанию Europe/Moscow). `from`/`to` — `DateOnly` включительно → `[from 00:00, to+1 00:00)` по бизнес-поясу → UTC. Валидация: `from ≤ to`, не больше 2 лет (`ErrorOr`). `TimeProvider` через DI, `FakeTimeProvider` в тестах. |
| D5 | Деньги — `numeric(18,2)` / `decimal`. `SaleItem` хранит копии `unit_price`, `unit_cost` и `product_version_at` на момент продажи. Generated columns (STORED) `line_revenue`, `line_cost`. Округление только на экране; маржа отдаётся долей. |
| D6 | Маржа и средний чек = `null`, если нет выручки/продаж; UI «—». Изменение к прошлому периоду = `null`, если в прошлом периоде 0; UI «нет данных для сравнения». |
| D7 | Спортивная нумерация (1, 1, 3). Порядок: метрика → выручка → имя. Менеджеры без продаж — внизу, без места, «Нет продаж». Неактивные без продаж скрыты. «Лучший менеджер» = первый по валовой прибыли; при равенстве «и ещё N с тем же результатом». |
| CAT-COUNT | В блоке категорий нет количества продаж: выручка, прибыль, маржа, единицы, доля в выручке. |

## API (settled)

| ID | Решение |
|---|---|
| D9 | Один endpoint на каждый блок dashboard. |
| API-KPI | Метрика = `current`, `previous`, `change`. Для денег и количества `change` — относительное изменение (0.12 = +12%). Для маржи — разница в процентных пунктах, отдельное поле `changePoints`. В ответе `period` и `previousPeriod`. |
| TIMESERIES | Шаг выбирает сервер: ≤ 31 дня — день, ≤ 180 — неделя (с понедельника), иначе месяц. Неполные корзины на краях — как есть, с реальными датами. |
| TOP-PRODUCTS | 10 товаров по валовой прибыли; выручка, прибыль, маржа, единицы. |
| RECENT-SALES | Keyset-пагинация по (`sold_at` desc, `id` desc), курсор непрозрачный. Размер страницы 20, максимум 100. UI — «Показать ещё». |
| RANKING-CHANGE | В рейтинге изменение к прошлому периоду — по метрике (прибыль), не по месту. |
| OPENAPI | Встроенный `Microsoft.AspNetCore.OpenApi`, документ генерируется при сборке и коммитится; UI — Scalar. Frontend-типы — `openapi-typescript`. |

## Технические решения (settled)

| ID | Решение |
|---|---|
| C1 | Миграции — EF Core migrations (исключение из правила «Liquibase для всего DDL») |
| REPO | Monorepo `sales-dashboard`: `backend/`, `frontend/`, `docker-compose.yml` (`name: sales-dashboard`), `Claude.md`, `docs/` в корне. |
| C3 | .NET 10 (LTS) |
| DB-INIT | `SalesDbInitializer` (`IHostedService`, async `StartAsync`) применяет миграции и seed при старте API. Включается флагом `Database:ApplyMigrationsOnStartup`, флаг включён только в docker-compose. Seed на C#. |
| DB-COMPOSE | PostgreSQL — сервис в `docker-compose.yml` проекта, с healthcheck. API стартует после `service_healthy`. |
| D8 | Vertical Slice Architecture: один API-проект, папка на каждый блок dashboard + тонкий `Shared`. Без CA-слоёв, MediatR, репозиториев. |
| D10 | EF Core LINQ по умолчанию. Dapper — для hot path, сложных запросов и когда нужен полный контроль над SQL. Один `NpgsqlDataSource` для EF Core и Dapper. |
| D12 | Исключения → `IExceptionHandler` → `IProblemDetailsService` + лог Serilog. Ожидаемые ошибки → `ErrorOr` → общий маппинг в `ProblemDetails`. |
| IDS | UUID v7 для всех ключей. |
| ENUMS | Enum в модели + строка в БД + `CHECK`, построенный из enum. |
| NAMING | snake_case в БД через `EFCore.NamingConventions` (без данных о практиках команды). |
| PKG | Central Package Management: `Directory.Packages.props`. Анализаторы в `Directory.Build.props` с `PrivateAssets=all`. `xunit.analyzers` — только в тестовом проекте. |
| PERF | Оптимизация чтения — отдельная ветка `perf`. |
| CONSISTENCY-TEST | Интеграционный тест: сумма динамики = сумма категорий = сумма рейтинга = выручка KPI, для нескольких периодов. |

## Seed (settled, D13)

- Bogus, локальный `Randomizer(20260924)` (не глобальный `Randomizer.Seed`); даты — смещения от дня запуска.
- Id детерминированные: `SeedGuid` строит UUID v7 из seeded-генератора (timestamp продажи = `SoldAt`).
- 20 менеджеров, 80 клиентов, 6 категорий, ~40 товаров, ~4 000 сделок за 12 месяцев, 1–4 позиции в сделке.
- Профили: 3 сильных, 3 слабых, 2 «крупные сделки», 2 «много мелких», 1 в отпуске 6 недель, 1 неактивный (уволен 3 месяца назад), остальные средние.
- Сезонность: пик март–май и декабрь, спад в январе.
- 5–8% отмен, 3–5% возвратов, 3–5 очень крупных сделок.
- Обновление прайса в середине года: часть товаров — новая цена и новый `version_at`.
- Запускается, только если таблицы пусты.
- Сложные edge cases (равенство менеджеров, продажа в полночь) проверяются тестами на ручных данных.

## Draft

- **C2** — отдельный one-shot контейнер для миграций (вариант для production).
- **EXTRA-BLOCKS** — доля категорий, распределение маржи, sparklines в рейтинге (в этом порядке).
- **PRODUCT-HISTORY** — таблица истории карточек товара.
- **SALE-TOTALS** — итоги сделки в `Sale` (денормализация, ветка `perf`).
- **RANK-POSITION-CHANGE** — изменение места в рейтинге к прошлому периоду.

## Rejected

- **VIEW** — представление PostgreSQL с правилами; R3 решён generated columns.
- **DAPPER-AOT** — выгода не видна на фоне стоимости запроса к БД.

## Open

- **D14** — frontend-стек и дизайн.
- **D15** — состав тестов.
- **AI_PROMPTS.md** — пользователь займётся отдельно.

## TODO

- **D11** — индексы. В ветке `perf`, с `EXPLAIN ANALYZE`.

## Соглашения по коду

- Options-классы: `{Feature}Options`, свойства `{ get; init; }`.
- Логи: Serilog, только структурированные свойства, без интерполяции строк.
- JSON: только source-generated сериализация.
- Hosted-инициализаторы: `{Store}Initializer`, async `StartAsync`, без `.GetAwaiter().GetResult()` и `CancellationToken.None`.
- `TreatWarningsAsErrors` с первого коммита; папка `Migrations` помечена `generated_code = true` в `.editorconfig`.
- Тесты: xUnit v3 + Testcontainers.
