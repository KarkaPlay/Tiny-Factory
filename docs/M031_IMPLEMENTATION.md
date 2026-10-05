# M03-R01 — patch читаемости 0.3.1

Статус: Editor implementation, evidence и Android build v0.3.1/code7 готовы к Producer handoff. Изменяется только presentation существующей M03-петли. Новые upgrade уровни, unlocks, saves, sound и ads не входят в patch.

## Реализация

- `FactoryRuntime` публикует read-only presentation-события после принятого сбора и после зафиксированной продажи. Tick, очереди, цена продажи и транзакция покупки не менялись.
- `FactoryPresentation` показывает первый явный CTA, подписи трёх станций, автоисточник с countdown, очередь, живые полосы обработки, дефицит/доступность Speed L1 и состояние завершённого улучшения. Тексты покупки берут цену и времена из `FactoryBalanceConfig`.
- Ограниченный FIFO-список визуальных листьев согласуется с runtime snapshot: упаковка WIP, вход упаковки, сушилка WIP, затем source buffer. Переход в «упакованный чай» происходит только при фактическом runtime переходе; продажа удаляет элемент только после `SaleCommitted`. DOTween анимирует перемещение/полосы/feedback, но не изменяет очередь, деньги или продажи.
- Label projection преобразует `Camera.WorldToViewportPoint` в прямоугольник safe area, чтобы экранная геометрия совпадала при разных разрешениях GameView. Камера подбирает вертикальный размер по фактическому aspect.

## Проверки и evidence

- Unity instance `Tiny Factory@4b2dafe5`, Unity `6000.3.9f1`; актуальный `FactoryPresentation.cs` compiled успешно. После финального Play smoke Editor остановлен, `is_playing=false`, `is_changing=false`, `is_compiling=false`; Console error/warning query вернул 0 записей (остались только bridge/log сообщения). `SampleScene` повторно прошла MCP validate: 0 issues, 0 missing scripts, 0 broken prefabs; hierarchy имеет четыре ожидаемых roots: Main Camera, Directional Light, Global Volume и Tiny Factory Runtime.
- M03 deterministic harness: ранее PASS на точной трассе t6/t30/t42/t59–60, saturated 120s L0=39 и Speed L1=58, backpressure drain=42 items exactly once. Runtime не изменялся после этого прохода.
- Synthetic live-button smoke: 8 последовательных сборов заполнили source buffer до8; вместе с одним packager WIP получилось `TotalInFlight=9`, visuals=9, rejection=0. Следующий сбор получил отказ, `RejectedHarvests=1`; projected stages source8 / dryer0 / packagerInput0 / packagerWIP1. После покупки `TotalInFlight=8`, visuals=8, source6 / dryerWIP1 / packagerInput0 / packagerWIP1. Все 8 movement tweens были paused при foreground=false; позиции не изменились за1.5с. После `DestroyImmediate(FactoryPresentation)` все пять сохранённых owned tween handles (`feedback`, `wallet`, `harvest`, `dryerBar`, `packagerBar`) имели `IsActive=false`.
- Живой purchase button был доступен при 36 монетах; click с последующим runtime tick привёл к Speed L1 и 0 монет. Уже начатая до покупки сушка сохранила 3с, что соответствует tick boundary; новая сушка отображает 3→2с. Runtime остаётся источником истины.
- Actual GameView capture targets и размеры: 9:16 — 540×960, aspect0.5625; 19.5:9 — 432×936, aspect0.461538; 20:9 — 405×900, aspect0.45. После viewport projection подписи станций не пересекаются в этих трёх снимках.
- Android build job `build-7c99c9d2e6`: success, duration152.40с, 0 errors/2 warnings; version0.3.1, bundle code7. Unity MCP post-build readback подтвердил `0.3.1/code7`, Android, PlayMode off. Console warnings: отсутствует `RuntimePipelineConfig` (Pipeline выключен в Player) и obsolete `Object.FindObjectOfType<T>()` в `FactoryPresentation.cs`; scene validation повторена после сборки: 0 issues/missing scripts/broken prefabs, roots остаются четырьмя.
- APK: [TinyFactory-v0.3.1-code7.apk](../Builds/Android/TinyFactory-v0.3.1-code7.apk), 38,844,621 bytes; SHA-256 `bb864cb3837e43183e06872948ef012b4a1000fc2f1d638ee74a0a9a6ca97eef`. Unity report указывает uncompressed size715.73MB.

Ключевые новые кадры: [startup/processing 9:16](../Assets/Screenshots/m031-startup-9x16-projection-fix.png), [sale +4](../Assets/Screenshots/m031-sale-feedback-projection-fix-9x16.png), [affordable](../Assets/Screenshots/m031-upgrade-affordable-projection-fix-9x16.png), [purchase + acknowledgement](../Assets/Screenshots/m031-upgrade-acknowledged-projection-fix-9x16-1.png), [19.5:9](../Assets/Screenshots/m031-projection-fix-19_5x9.png), [20:9](../Assets/Screenshots/m031-startup-20x9-projection-fix.png).

Последний purchase acknowledgement был зафиксирован после ручного завершения уже совершившейся sale feedback tween для детерминированного Editor capture; покупка и карточка завершённого улучшения до этого были проверены по фактическому runtime состоянию.

UI review предыдущего candidate отмечал, что длинная purchase copy обрезалась внизу. Copy сокращена до двух коротких строк; указанный выше свежий кадр показывает acknowledgement и обе строки завершённого состояния целиком. Повторный независимый review после этой правки не выполнялся.

## Ограничения и следующий gate

- Build включает два предупреждения, описанные выше; они не помешали успешной сборке. RuntimePipelineConfig warning существовал раньше; obsolete FindObjectOfType относится к presentation коду.
- Физический Android install/run остаётся deferred по D12. Скриншоты и synthetic UI input не заявляют device UX pass или подтверждённую понятность для нового игрока.
