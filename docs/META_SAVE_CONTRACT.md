# Мета — контракт сохранения и перехода с 0.4.0

**Дополнение 0.5.1 (D28, реализовано; проверки и ограничения — M051_ACCEPTANCE):** по запросу пользователя вводятся кнопки возле зданий: загрузка ровно одного ресурса и сбор всего помещающегося объёма. Нижний transfer picker убирается; скролл получает примагничивание и автоматический выбор видимого участка, а производство и ручные передачи — world-анимации. Рецепты, цены, caps, заказы и schema2 сохраняются; один свежий лист в сушильне ждёт второго и сохраняется как неполный вход. Предыдущие правила выбора количества описывают историческую 0.5.0; актуальный патч — [M051_UI_SPEC](M051_UI_SPEC.md). Offline production не включён.

Статус: **контракт 0.5.0 и политика перехода с 0.4.0 приняты пользователем 2026-10-05; реализация N02–N05 завершена для Editor/build scope — [M050_ACCEPTANCE](M050_ACCEPTANCE.md)**. Этот документ задаёт требования, а evidence реализации фиксируется отдельно. Owner реализации — Engineer, reviewer — QA; Producer фиксирует продуктовую семантику. Правила производства — [META_GAMEPLAY_SPEC](META_GAMEPLAY_SPEC.md). Эти требования нужны уже 0.5.0, offline elapsed включается 0.6.0.

## Постоянные данные

- Schema2 в отдельном meta slot: primary/verified backup/temp; старый schema1 slot и future-schema archives сохраняются. Unknown newer version не перезаписывается; recovery не импортирует v1 поверх существующего несовместимого meta save молча.
- Coins, built/unlocked plots, upgrade levels, chapter/milestone progress, `pilotGoalComplete`, completed-order count/IDs, warehouse quantities по productId.
- У каждого производства: local input/output quantities, active recipe и reserved WIP inputs, remaining processing time, состояние готового WIP, ожидающего места в output, если выпуск заблокирован. Начатый продукт не расходуется/не начисляется второй раз после restart.
- Offered orders: stable IDs, состав/награда, generation counter/RNG state; один active order, его ID/status. При reload предложения не генерируются заново. Onboarding, mute, камера/выбор в пределах существующих plot IDs.
- Последний checkpoint UTC, migration source/receipt и legacy summary. Gameplay counts/caps/enum allowlists/recipe IDs проверяются перед load; input/WIP и готовый output не представляют один продукт одновременно.
- Coins/покупка, collect/load, turn-in/reward, generation/accept/cancel и migration сохраняются целиком как транзакции. UI показывает подтверждённый результат. Ошибка записи не объявляет успешную выплату; повтор не дублирует продукты/монеты.

## Активный прототип 0.5.0

Производство идёт только в foreground, но input/output/WIP и оставшееся время **сохраняются** при cold restart. За закрытое приложение время не вычитается. Это сознательное отличие от 0.4.0 cold-WIP discard; в интерфейсе явно указано «Прототип: производство только в открытой игре». Offline production не заявляется до 0.6.0.

## Возвращение 0.6.0

**До запуска0.6 нужен отдельный review профиля длительностей и ёмкости.** Быстрый профиль0.5 заполняет сад за120с наL0, сушильня перерабатывает полный input за32с, фасовка — за48с; восьмичасовой cap не исправляет столь короткую работу. Профиль возвращений пока не готов к реализации.

Предлагаемый cap elapsed —8 часов, стартовая гипотеза для проверки. Фактические объёмы ограничены загруженным сырьём и локальным output capacity; cap не гарантирует 8 часов работы любого здания. Расчёт выполняется при каждом возврате из отсутствия (background и cold launch) из одного persisted checkpoint. Во foreground используем active timer; не применяем одновременно active delta и тот же elapsed.

Применить clamp(nowUTC − checkpointUTC,0,cap), вычислить только работу уже загруженных производств и свободного source, записать весь resulting state и новый checkpoint **одной** транзакцией, затем показать return summary. Checkpoint обновляется даже при полном output и при elapsed больше cap: отброшенные часы не догоняются следующим запуском. Нет auto collect/load, перераспределения warehouse, turn-in, reward или открытия plot из отсутствия игрока.

При смене часов назад elapsed=0, checkpoint устанавливается на текущий UTC в успешной транзакции; при больших скачках вперёд действует cap. Это локальная, не защищённая от манипуляций временем модель без backend; античит не обещается. Failed write/kill до commit оставляет прежнее валидное состояние; следующее применение из него не должно повторно применять ранее закоммиченный выпуск.

## Принятая миграция v1 → v2

| Старые данные | Новая модель |
|---|---|
| Coins | Перенести, применить прежний cap 1e12. |
| Speed/Productivity/Automation levels | Вернуть потраченные монеты по старой таблице 36/54/72 за каждый купленный уровень каждой ветки (максимум 486), вместе с переносом coins и cap. Не объявлять эти уровни уровнями новых зданий. |
| Sold/unlocks/onboarding | Сохранить в legacy summary и исходном v1. Новые order milestones начинаются с 0: старые auto-sales не выдаются за выполненные заказы. Показать объяснение новой кампании. |
| Mute | Перенести. |
| Старые WIP/фазы | v1 не сохранял их; новых запасов не создавать. |
| Новые здания/запасы | Стартовые здания по fresh-конфигурации; на складе стартовые4 FreshLeaf один раз, остальные запасы/input/output/WIP пусты. Стартовые80 монет fresh-профиля к перенесённым деньгам не добавляются. |

Миграция читает валидный v1 primary либо verified backup. Проверяет schema/диапазоны, создаёт и перечитывает temp v2, коммитит v2 вместе с migration receipt; **v1 не изменяет**. При валидном v2 повторно не импортирует и не начисляет refund. После failed migration повтор безопасен. При двух повреждённых v2 сначала обычный recovery/notice; не использовать v1 как бесконечный источник нового refund.

Перед применением пользователь видит в review контракт: деньги перенесены, старые улучшения компенсированы, новые контракты начинаются сначала. Никакого silent reset. Пользователь принял эту политику отдельным production-запросом 2026-10-05. Инженер не придумывает альтернативу при build.

Профиль с legacy coins может купить здания/уровни раньше fresh-профиля; prerequisites новой кампании сохраняются (в пилоте — выпуск5 DryTea). Migrated pacing не смешивается с fresh balance/playtest. Обратный downgrade с v2 в 0.4.0 не синхронизирует кампании; исходный v1 остаётся снимком до миграции, не обещанием автоматического rollback.

## Обязательные негативные проверки

- Повтор collect/load/turn-in/accept при двойном tap, нехватка продукта после UI preview, full local storage: нет partial mutation и отрицательного inventory.
- Save → cold restart посреди обработки, на границе завершения и после order payout: количества и reward ровно один раз.
- Corrupt/missing primary +валидная backup; обе копии повреждены с notice; неизвестный product/recipe/plot/range/неполный DTO; newer schema preservation.
- Interrupted temp write/replace и migration retry: нет потери v1, duplicate refund, reward или вход рецепта; platform crash safety проверяется отдельно, не выводится из Editor fixture.
- Offline точная граница remaining time/до неё/после неё, полный output/пустой input, elapsed0/negative/cap+1, background+cold последовательность, два быстрых launch, некорректные часы; отработанное время не начисляется дважды.
- Update реального 0.4.0/code10 → meta build на Android; backup recovery и migration notice. D12 device gate остаётся обязательным.

Наличие этого документа не означает, что schema2/миграция/атомарность реализованы. Engineer выбирает детали serialisation/replace через существующий Unity MCP workflow, без нового storage SDK/cloud.
