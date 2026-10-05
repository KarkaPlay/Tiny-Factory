# M051 — патч 0.5.1: наглядность и прямое управление

2026-10-06. **Реализация и APK приняты Producer для ограниченного Editor/build scope; полного visual/device PASS не заявляем.** Запрос — D28, решение о приёмке с ограничениями — D29. Физическая Android проверка отдельно.

## Согласованный результат

- Возле зданий: загрузка ровно одного входного ресурса и сбор всего помещающегося выхода. У сада только сбор. Нижний выбор количества убран; сведения, улучшения, склад и заказы остаются доступны.
- Сушильня после первого листа ждёт второй (1/2); только затем начинает прежний рецепт 2 FreshLeaf → 1 DryTea. Неполный вход сохраняется без округления или schema bump. Остальные цены/таймеры/рецепты/caps прежние.
- Скролл автоматически выбирает ближайшее видимое здание с hysteresis; после отпускания примагничивается к его якорю. UI нажатия не панорамируют мир; свайп с кнопки отменяет click. Сохранение положения после остановки, прежний camera offset −12..0.
- Рабочие части двигаются, прогресс и готовый выпуск видимы. Товары перемещаются между источником, общим складом и получателем по ручным действиям. Эффекты только после успешного commit: они не задерживают/повторяют передачу, не работают как скрытая автоматизация.

## Приёмка

| Gate | Нужное evidence | Статус |
|---|---|---|
| Unity | Correct project, clean compile/Console, authored scene save/reopen/validation, сохранённые .meta/GUID | Engineer: Unity 6000.3.9f1 / SampleScene, compile complete, Console errors 0, save/reopen rootCount 10 и validation 0. Producer: matching .cs/.meta новых компонентов и уникальные GUID подтверждены. Post-build: Edit Mode, not compiling, clean scene/rootCount10/validation0, Console errors0 |
| Load/collect/save | 1+1 запускает обычный рецепт; odd input cold reload; cap/остаток/conservation; failure/double tap; совместимость schema2/WIP/orders/coins и прежней v1 migration | Независимый QA: +1/+1, odd cold reload, cap/conservation, injected write failure, schema2/WIP/orders/coins и v1 migration PASS. Полная authored double-tap/collect-FX матрица не проверена; QA commit/lifecycle/no-replay/failure PASS |
| Кнопки/скролл | Реальные authored callbacks, исчезновение picker, tap/drag изоляция, оба направления/bounds/auto selection/snap; restore −4.2; pause/focus прерывает drag/tween и оставляет settled state | Engineer: actual Dryer +1 и drag suppression, bidirectional preview/hysteresis. QA: free drag/clamp/release/one settle/pause PASS; hidden EventSystem hit-test NOT RUN. Engineer наблюдал startup restore −4.2 и nearest snap; перезапись offset после startup-snap не подтверждена. Full authored UI-hit matrix не проверена |
| Наглядность | Valid actual frames/clip: load/collect движение, работа, ready output, 1/2, near buttons/подписи, snap; нет FX при failure/reload, cleanup на interruption | UI: исправленный Dryer sidecar и состояние 1/2 подтверждены по PNG 824×1465; полный силуэт открыт. Garden/Dryer corrected controls representative PASS. Single token frame presence only; temporal motion/production progression visual NOT VERIFIED; final Packer-specific image NOT RUN, stale Dryer image excluded |
| Android | APK 0.5.1/code 12, прежний app ID/signature, ARM64/IL2CPP, build errors/warnings, manifest/hash/size/CRC и post-build scene/Console | PASS — build-d5e36bb17d success, 79.0s, 0 errors / 1 existing RuntimePipelineConfig warning; independent Producer metadata/CRC/v2 signature PASS; post-build scene/Console clean |

Документы: [IMPLEMENTATION](M051_IMPLEMENTATION.md), [ENGINEER_CHECKS](M051_ENGINEER_CHECKS.md), [UI_SPEC](M051_UI_SPEC.md), [QA](M051_QA.md). Source review сам по себе не считается runtime/visual evidence. Targeted state/FX execution и representative UI review приняты с явно указанной неполнотой visual/input coverage. Minor Garden edge inset — follow-up при проверке устройства. Старый APK и игровые данные не удаляются.

## Вне этого патча

Offline production/0.6+, новые recipes/upgrades/providers/packages, платные ассеты и публикация. Hardware touch, safe area/48dp, длинные portrait форматы, Android background/process-kill/filesystem и FPS/memory остаются device gate. Положительный отзыв о 0.5.0 не подменяет эти проверки.

## Артефакт и проверка Producer

[TinyFactory-0.5.1.apk](../Builds/Android/TinyFactory-0.5.1.apk), 40,506,481 bytes; SHA-256 `9b82885f8a9fb9b58715f4b7a0be61ff7f8329a514c7a7b7050665b96df02071`. APK: `com.tinyfactory.prototype`, 0.5.1/code12, minSDK25/targetSDK36, только ARM64, libil2cpp.so и ZIP CRC PASS. Подпись v2, certificate SHA-256 `7763eae5a11cfd879282e8c940eea3d9212cb72b8b46e13d200164d81994b63a` совпадает с 0.4.0/0.5.0; размеры и hash старых APK неизменны. Build report size 743.18 MB не является размером APK. После сборки временные fixtures сброшены; final source/serialized duration 0.62 s. Новые компоненты имеют matching .cs/.meta и уникальные GUID.

Ограничения покрытия приняты для выдачи APK на ручную проверку. Отдельного подтверждения всех визуальных маршрутов и полного device DoD нет. Unity-authored YAML whitespace не исправлялся вручную; сохранённый serializer output и чужие изменения сохранены.
