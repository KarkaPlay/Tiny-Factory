# 0.4.0 — M04–M05: границы и приёмка

Production-запрос пользователя: 2026-10-05. Исходное состояние — 0.3.3. Owner реализации — Unity Engineer; read-only проверка правил — Game Designer; review сохранений и регрессии — QA. Producer интегрирует результат. Числа взяты из [GAMEPLAY_SPEC](GAMEPLAY_SPEC.md), local storage — из D11/D15 в [DECISIONS](DECISIONS.md).

## Согласованный результат

Три ветки по три покупаемых уровня, два линейных открытия этой же линии, authored uGUI с понятными ценами/дефицитом/MAX и видимым locked состоянием станций. Локальное сохранение валюты, уровней, lifetime sold, флагов onboarding и настроек. Unlock flags выводятся из lifetime sold. WIP/очереди не сохраняются между холодными запусками. Новые SDK, cloud sync, offline earnings, реклама, аналитика и M06 tutorial/audio в этот этап не входят.

| Данные | L0 | L1 | L2 | L3 |
|---|---:|---:|---:|---:|
| Цена покупки уровня | — | 36 | 54 | 72 |
| Сушилка, секунды | 3 | 2 | 2 | 1 |
| Скрутчик, секунды | 2 | 1 | 1 | 1 |
| Конечный узел, секунды | 2 | 2 | 1 | 1 |
| Листьев за ручной/автосбор | 1 | 2 | 3 | 4 |
| Интервал автосбора, секунды | 12 | 10 | 8 | 6 |

Скрутчик открывается после продажи №15; запайщик — после №40. Цена продажи №15 = 4, №16 = 5, №40 = 5, №41 = 6. Скрутчик меняет маршрут только будущих выходов сушилки; уже переданные в конечный узел предметы завершают обработку там.

## Проверяемые критерии

| Проверка | Ожидаемый результат |
|---|---|
| Все девять покупок | Ступени последовательны, цены из конфигурации; каждая ветка меняет только собственный параметр; L3 показывает MAX и не допускает списания. |
| Недостаток денег / быстрые повторные нажатия | Funds/level перепроверены при commit, не более одной покупки за тик, баланс неотрицателен. |
| Покупка во время обработки | Remaining time текущего WIP не меняется; новая работа использует новый уровень. Automation начинает полный новый интервал. |
| Пороговые продажи 15/16/40/41 | Правильные суммы и единственное открытие; запайщик не добавляет отдельный таймер продажи. |
| Полные очереди | Каждый буфер ≤8, batch целиком принят или отклонён; готовый WIP ждёт downstream, без потерь/дубликатов. Максимальная линия ≤27 предметов. |
| Числовая трасса | Воспроизвести принятые checkpoints t6/t42/t65/t127 и итог активной трассы t300: sold184, coins563, уровни L3. Расчётные числа до MCP проверки не считаются runtime evidence. |
| Насыщение прогресса | Coins/sold ≤1e12; производство продолжается на cap и после достижения MAX. |
| Background/resume | Прогресс сохранён; WIP и фазы остаются в RAM; в фоне производство остановлено, catch-up отсутствует. |
| Cold restart | Восстановлены coins/levels/sold/derived unlocks/onboarding/settings; WIP и фазы сброшены, offline income отсутствует. |
| Чистая установка / отсутствующие файлы | Обычное свежее состояние без сообщения о повреждении. |
| Повреждённый primary / валидный backup | Восстановлен проверенный backup, дальнейшие записи сохраняют читаемое состояние. |
| Обе копии повреждены | Без crash, безопасное fresh state и видимое уведомление. |
| Невалидные значения JSON | Отклонены отрицательные/выходящие за пределы coins/sold/levels и неподдерживаемый формат; нет некорректной прогрессии. |
| Прерванная запись | Temp перечитан/проверен перед заменой; доступна предыдущая валидная primary или backup. Платформенная crash safety подтверждается отдельно. |
| Более новая schema | Исходник сохранён в архивный slot, уведомление показано; fresh current slot не перезаписывает исходник, повторный запуск сохраняет архив. |
| Offline | Save/load и core loop не зависят от сети, SDK или cloud API. |
| Editor / build | MCP правильного проекта, импорт/компиляция/Console, сцена/prefabs сохранены и reopening проверен; APK 0.4.0/code10, идентификатор приложения сохранён. |

## Статус evidence

**Принято 2026-10-05 для Editor/build scope.** Game Designer подтвердил правила/баланс, UI — captured-state layout, QA — runtime/save evidence и финальные +4/+6 HUD fixes. Общий bounded harness дал PASS: active t300 sold184/coins563/all L3, passive sold24/coins105. Первоначальный неверно ограниченный цикл теста исправлен; после пользовательского перезапуска Unity проверки завершены. Сцена сохранена/reopened, persistent listeners count1, validation0; финальная сборка включает исправления batch/route hints.

Unity MCP job `build-5c45e8f29d` succeeded in 56.819734 s with 0 errors / 1 warning. Final artifact: `Builds/TinyFactory-0.4.0.apk`, **40,430,233 bytes**, SHA-256 `b9973088128f46578ff387f3b390c7d6e4d965842961508b7ca46bfe5c3f360d`. APK manifest inspection confirms version `0.4.0`, code `10`, package `com.tinyfactory.prototype`, minSDK 25 / targetSDK 36, and only `arm64-v8a` native libraries. Unity settings confirm IL2CPP. Reported build size 741.13 MB is not the APK file size.

Post-build MCP: Play off, compiling/updating false, Console errors 0; SampleScene validation 0 issues / 0 missing scripts / 0 broken prefabs. Pre-build saved scene was clean, with coins/sold/levels/WIP zero, no validation hosts, and exactly one roller and one sealer root.

The sole warning is `Pipeline: No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime). Pipeline will be disabled in Player builds.` It is the existing optional Pipeline configuration warning; no URP failure is inferred from it.

Физический Android и форматы19.5:9/20:9 не проверены. Save interruption fixture проверяет отказ до записи temp, а не process kill при replace; Android filesystem atomicity остаётся открытой. Подробности — [M040_QA](M040_QA.md) и [BUILD_NOTES](BUILD_NOTES.md).

По D12 установка/update, hardware touch, реальный background/resume и performance на Android остаются manual pending. Editor/build evidence не закрывает физический device DoD.
