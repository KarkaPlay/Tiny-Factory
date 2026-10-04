# M03 — базовая фабрика и скорость

Статус: реализация завершена, принятый Android APK собран через Unity MCP как v0.3.0/code6; физический Android device gate остаётся отложенным по D12.

## Изменения

- `FactoryBalanceConfig` хранит значения баланса в `Assets/TinyFactory/Resources/FactoryBalanceConfig.asset`: ручной сбор 1 листа, общая вместимость каждого буфера 8, автоисточник каждые 12 секунд, сушилка 3с (Speed L1 — 2с), упаковка 2с, продажа +4, цена Speed L1 — 36, верхний предел монет/продаж — 1e12.
- `FactoryRuntime` выполняет фиксированные односекундные тики. Источник и вход сушилки — один общий буфер; вход упаковки — второй буфер. Сушилка и упаковка имеют по одному WIP, поэтому максимум в линии — 18 предметов. Полные буферы удерживают WIP и блокируют новые ручные сборы без потери или повторной продажи. Один тик создаёт не больше одной продажи и обрабатывает максимум одну покупку с повторной проверкой средств.
- При длинном кадре runtime выполняет один тик и сбрасывает только пропущенные целые секунды, сохраняя долю остатка. Pause/focus блокируют симуляцию; обычный resume сохраняет remainder и не наверстывает background-время.
- `FactoryPresentation` собирает сцену из Unity primitives и минимальный HUD: кошелёк/продажи, статус линии, ручной сбор и кнопка Speed. Лист видимо перемещается между станциями; кнопки недоступны, если игрок не в foreground или источник переполнен.
- `SampleScene` сохраняет существующие Main Camera, Directional Light и Global Volume; к ней добавлен `Tiny Factory Runtime` с runtime/presentation. Сцена включена в Android build.

## Проверки

- Unity MCP deterministic validation: PASS. Точная трасса: t6 = 1 продажа/4 монеты, t30 = 6/24, t42 = 9/0 после Speed L1, t59 = 14/20; окончание 60-секундной трассы также 14 продаж/20 монет.
- Насыщенная линия за 120с: L0 = 39 продаж, Speed L1 = 58. С медленной упаковкой downstream backpressure удерживает WIP; 42 принятых листа после освобождения линии проданы ровно один раз, максимум WIP = 18.
- Валидация также проверяет полный source buffer, отсутствие очереди двойной покупки, отказ неактивного ввода, средства при commit, неизменность времени уже идущего процесса, pause/focus, остаток тика и cap 1e12.
- Validation fixtures создаются с `HideFlags.DontSave`, а тестовый runner уничтожает их и cloned config в `finally`, включая путь с ошибкой. После повторной валидации hierarchy содержит ровно четыре scene roots: Camera, Directional Light, Global Volume и production `Tiny Factory Runtime`.
- Actual Play Mode scene smoke: через `ExecuteEvents.pointerClickHandler` на live harvest Button получен `SourceBuffer=1`; затем в живом runtime проведены 42 тика, synthetic collect clicks по Button, 9 продаж и pointer click по Speed Button с commit в Speed L1/0 монет. Для изолированного Editor автоматического вызова foreground был явно выставлен и кнопка разблокирована; это UI event-path smoke, не физическое касание.
- `SampleScene` сохранена через MCP и прошла validation: 0 issues, 0 missing scripts, 0 broken prefabs. Post-build Editor: `ready_for_tools=true`, compile=false, Play Mode off; Console errors = 0.
- Первая code5 сборка была признана superseded: producer scene inspection нашёл оставшийся `M03 Validation Runtime` в сцене. Тестовый root удалён через MCP; fixture cleanup теперь гарантирован в `finally`, roots проверены после повторного PASS, сцена сохранена и повторно собрана как code6.
- Скриншот GameView 360×640: [`screenshot-20261005-031255.png`](../Assets/Screenshots/screenshot-20261005-031255.png). Его отображение и APK на физическом Android устройстве не проверялись.

## Границы

- M04 upgrades/unlocks, M05 saves, M06 polish не входят в этот этап. Нет экономического offline catch-up.
- Android Player build подтверждён; Android установка, запуск, физическая производительность и lifecycle на устройстве остаются pending по D12.
- Известные два build warnings указаны в [BUILD_NOTES](BUILD_NOTES.md): отсутствует RuntimePipelineConfig (унаследованная конфигурация проекта) и `Object.FindObjectOfType<T>()` obsolete в поиске EventSystem. Их не меняли в рамках M03.
