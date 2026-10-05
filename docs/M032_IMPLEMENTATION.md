# M03-R02 — явная world-material dependency для Android

Статус: implementation/build готов к Producer review; повторный запуск на устройстве не выполнен.

## Симптом и вывод

Пользователь сообщил: APK 0.3.1/code7 на Realme RMX3834 с Android 15 показывает все 3D-объекты розовыми, UI отображается нормально. Скриншот и Player log не предоставлены, поэтому конкретное поведение shader/pipeline в том Player не подтверждено.

Подтверждённая проблема исходного кода — у runtime-created primitives не было явной зависимости от проекта material/shader: `FactoryPresentation.MakePrimitive` полагался на `GameObject.CreatePrimitive` и назначал только `MaterialPropertyBlock`. Это оставляло Player зависимым от неявного default material и его shader. Причиной pink rendering могло быть отсутствие/подмена default shader или stripping; единственную причину на устройстве установить нельзя без Player log. Исправление добавляет явный serialized Resources material и назначает его всем создаваемым factory renderers.

## Изменения

- Создан `Assets/TinyFactory/Resources/FactoryWorldMaterial.mat`, GUID `585741ec9641c4e719ed509661b717c0`, shader `Universal Render Pipeline/Lit`, GUID `933532a4fcc9baf4fa0491de14d08ed7`.
- `FactoryPresentation.Start` загружает материал один раз через `Resources.Load<Material>("FactoryWorldMaterial")`. Если asset отсутствует, пишет `Debug.LogError` и factory world renderers отключаются вместо неявного default material.
- Общий `MakePrimitive` назначает `renderer.sharedMaterial = factoryWorldMaterial`, затем оставляет существующее per-renderer `MaterialPropertyBlock` для цветов. Поэтому материал разделяется всеми world objects/items, а цвета не теряются.
- Gameplay runtime, экономика, Quality/Graphics pipeline settings и packages не изменялись намеренно. `GraphicsSettings.defaultRenderPipeline` остаётся null; активный Editor quality был PC, а build target — Android. Вручную не менял `URPProjectSettings.asset`; Unity импорт создал/обновил serialization path при обработке material asset (смотри существующее состояние проекта и review Producer).

## Проверки и сборка

- Unity MCP: правильный проект `Tiny Factory@4b2dafe5`, Unity `6000.3.9f1`; материал создан через Editor AssetDatabase, runtime Resources load успешен.
- Compile/import: завершён, Console errors `0`. SampleScene validate: `0 issues`, `0 missing scripts`, `0 broken prefabs`.
- Editor Play smoke: все 7 проверенных world renderers используют `FactoryWorldMaterial` / `Universal Render Pipeline/Lit` и включены: ствол, земля, стол, чайный куст, лента, сушилка, касса/упаковка. Это подтверждает assignment в Editor, не Android render.
- Unity `BuildReport.GetLatestReport().packedAssets` содержит `Assets/TinyFactory/Resources/FactoryWorldMaterial.mat` (`1,672 B`) и `Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader` (`205,600 B`). Это подтверждает наличие material и shader assets в packed build report; device shader variant/rendering этим не доказан.
- Android build job `build-8eb5270fb4`: success, Unity `6000.3.9f1`, version `0.3.2`, bundle code `8`, bundle ID `com.tinyfactory.prototype`, duration `81.39 s`, `0 errors`, `2 warnings`; Unity summary reported `716.08 MB` uncompressed. Warnings: `No RuntimePipelineConfig asset found ... Pipeline will be disabled in Player builds` и существующее `Object.FindObjectOfType<T>()` obsolete warning в `FactoryPresentation.cs`. Первый warning не приписан URP без подтверждения его источника; конфигурацию pipeline не менял.
- APK [`TinyFactory-v0.3.2-code8.apk`](../Builds/Android/TinyFactory-v0.3.2-code8.apk): `38,998,713` bytes; SHA-256 `7e2b8b2a638255d039b09a657be8c80f3052a854d42af48b638ec23cfb55b2b2`.
- После сборки Editor находится вне Play Mode, compilation завершена; SampleScene снова прошла validate без ошибок. Реальное Android устройство после исправления не запускалось.

## Передача проверки

Попросить пользователя установить именно v0.3.2/code8 на Realme RMX3834 / Android 15 и подтвердить окрашивание кустов, станков и движущихся предметов, сохранившийся UI, а также прислать `Player.log`/снимок если розовый рендер повторится. Не считать Android rendering исправленным до этого smoke. Если останется розовым, следующим шагом сопоставить фактический active Render Pipeline на Android с сообщением RuntimePipelineConfig и Player log; не добавлять все shader variants или менять pipeline вслепую.
