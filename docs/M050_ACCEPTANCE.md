# 0.5.0 — приёмка активного прототипа

2026-10-05. **N02–N05 приняты Producer для Editor/build scope.** Пользователь утвердил META baseline и переход0.4→0.5 (D25); результат проверен через Unity MCP. Физические Android проверки и playtest пользователь выполняет отдельно. Offline production и0.6+ не включены.

[APK0.5.0/code11](../Builds/Android/TinyFactory-0.5.0.apk): **40,491,069 bytes**; SHA-256 `1c1df75aa26accbc71901379b930c3576b0a351ae33578afd63177a29c5fe2b2`.

## Результат

Три3D участка в одной вертикальной колонке Garden→Dryer→Packer, ограниченный camera field, authored uGUI с закреплённым HUD/карточкой и вкладками. Общий warehouse40/perSKU20, local input/output8 и одинWIP на машину; config-driven рецепты/цены/два улучшения каждого участка. Ручной сбор/загрузка атомарны, имеют explicit modes, preview количества/остатка/ёмкости, MAX и отдельный commit CTA. Готовый output собирается при продолжающемсяWIP.

Два persistent offers/один active order, IDs/RNG и состав сохраняются. Hand-in полного набора начисляет награду один раз, cancel требует подтверждения. FreshLeaf fallback обеспечивает доход при нулевом wallet; Packer открыт после lifetime5DryTea; mixed2DryTea+2TeaPacket за22 завершает pilot goal.

Save2 сохраняет товары/WIP/remaining/заказы/UI selection. Время отсутствия не начисляется. Migration переносит coins/mute и компенсацию старых уровней36/54/72 до486 с cap1e12, начинает новую кампанию с4FreshLeaf без fresh80. V1 не перезаписывается, receipt/guard запрещают повторныйrefund; notice различает расчётную и фактически зачисленную компенсацию.

## Evidence

| Проверка | Результат | Источник |
|---|---|---|
| Import/compile/scene | PASS: C# errors0; SampleScene authored/saved/reopened, roots9, clean, validate0 missing scripts/broken prefabs. Все10 новых scripts/config/prefab имеют.meta/uniqueGUID. | [ENGINEER_CHECKS](M050_ENGINEER_CHECKS.md) |
| Core harness | PASS: fresh/UI reload, exactly-once order/cancel, immediate8s WIP и in-flight upgrade, schema2 sentinel normalization, migration receipt/no-repeat, malformed/future, pause/focus reset/resume-first-frame skip/0.25s hitch cap и persistence-block rollback. Timing tests выполнены Engineer, исходник reviewed QA. | [ENGINEER_CHECKS](M050_ENGINEER_CHECKS.md), [QA](M050_QA.md) |
| Independent QA | PASS bounded fixtures:80→88→48→73→3, mixed goal22, out-of-order tutorial; zero-wallet fallback; caps/partial collect/backpressure/conservation; RNG/IDs/cancel/reload; loaded-SKU shortage/atomic hand-in; upgrades/MAX/locked/timers; stale-UTC blocked-WIP cold restore; v1 SHA/no-repeat/retry, backup/corrupt2/future; rollback всех player actions/production и backup-copy failure. Disposable hosts/configs/temp roots очищены, scene не менялась. | [QA](M050_QA.md) |
| Authored HUD routes | PASS Engineer actual Button callbacks: Dryer collection сохраняетWIP8; Packer collectPacket1, loadDry1→WIP6; Dryer load MAX при leaves5 выбирает4→warehouse1/input2/reserved2/WIP8. Mode/step/MAX bug найден в review и исправлен; прежние API fixtures отдельно отUI-route evidence. | [ENGINEER_CHECKS](M050_ENGINEER_CHECKS.md), [QA](M050_QA.md) |
| UI | PASS representative states: Garden/Dryer/Packer views, selected tabs/modes, aligned preview/CTA, collect MAX/constrained/zero disabled, output+WIP, active missing/ready/cancel/post-cancel, migrationThreeL3/cap. ActualPNG824×1465; stale duplicate renders superseded. | [UI_REVIEW](M050_UI_REVIEW.md) |
| Migration bounds | PASS: legacy17 + refund288 =305; ThreeL3 old17 +486 =503; wallet1e12/расчёт486/зачислено0, receipt/no-repeat. Notice visible, fresh4/no80, originalv1 bytes preserved in fixtures. Не заявлена exhaustive legacy combination matrix. | [ENGINEER_CHECKS](M050_ENGINEER_CHECKS.md), [QA](M050_QA.md), [UI_REVIEW](M050_UI_REVIEW.md) |
| Android artifact | PASS: MCP jobbuild-d9abc9d8a7 success,0 errors/1 existingwarning; manifest0.5.0/code11/com.tinyfactory.prototype/min25/target36; onlyARM64,libil2cpp.so,ZIPCRC/signaturev2 PASS; certificate matches0.4. Producer independently verified size/hash/manifest/CRC/signature/ABI; old0.4 APK hash unchanged. | [BUILD_NOTES](BUILD_NOTES.md) |

Документальные проверки:14 документов, missing local links0, trailing whitespace0. Общий `git diff --check` отмечает пробелы после пустых YAML-полей в Unity-authored SampleScene; сцену вручную не форматировали, Editor save/reopen/validation PASS.

Implementation: [M050_IMPLEMENTATION](M050_IMPLEMENTATION.md). Producer decisions: D25–D27. Post-build: Edit Mode, scene clean/roots9/validation0, Console errors0; existing warning и bridge diagnostics only.

## Ограничения

- APK не установлен/не запущен на устройстве: install/update/migration, hardware touch/scroll/safearea/48dp, actual19.5:9/20:9, background/process-kill/Androidfilesystem и10-minute FPS/memory остаются manual. Представленные кадры имеют≈9:16; MCP metadata не доказывает другой aspect.
- Newcomer и интерес выбора DryTea между заказом/фасовкой не измерены; device/build PASS не равен игровому playtest или retention.
- Не вся authored price/tap/legacy combination matrix проверена. Packer load и constrained/zero load preview не имеют valid visual captures; capacity/API/routes/shared availability проверены. Representative coverage принята Producer, эти пробелы не выданы заPASS.
- Minor duplicate migration notice/ungrouped cap digits в верхней строке остаются accepted polish. Modal copy accurate/readable.
- При storage-write failure state/reward откатываются, дальнейшие действия блокируются с явным сообщением о перезапуске. Retry после устранения причины проверен новым runtime. Kill в момент replace и реальная Android storage failure не доказаныEditor fixture.
- 0.25s foreground hitch cap отбрасывает избыток времени: тяжёлые задержки могут замедлитьтаймер. Offline production отсутствует. Realads/analytics/cloud не добавлены; существующийPluginYG2/SDK сохранён.
- Единственное buildwarning — existing optional RuntimePipelineConfig; actual hardware rendering этим не подтверждено. APK подписан прежним Android Debug certificate для внутреннего теста.

## Короткий проход на Android после получения APK

1. Сначала update поверх установленной0.4 без удаления данных: проверить notice, сумму старых coins+компенсации (до486), mute, новые4FreshLeaf и отсутствие дополнительных80. Перезапуск не начисляет компенсацию снова. Отдельный clean-install сценарий начинается с80 coins/4FreshLeaf.
2. Принять/сдать учебный заказ; построить Dryer; прокрутить Garden→Dryer→Packer, выбрать участки тапом. Сбор/загрузка партий показывают SKU, количество, остаток и предел до подтверждения.
3. Во время WIP собрать уже готовый DryTea; после lifetime5Dry построить Packer, изготовить/собрать TeaPacket. Сдать mixed2Dry+2Packet за22 один раз; отмена требует видимого подтверждения.
4. Купить улучшение во время WIP, проверить сохранение текущего таймера и ускорение следующего; проверить MAX и нехватку денег. Перезапустить с товарами/заказами/WIP — остатки восстанавливаются.
5. Отключить сеть: local игра/запись доступны. Свернуть/закрыть на несколько минут и вернуть: за отсутствовавшее время продукция не добавляется.
6. Проверить safe area/кнопки/прокрутку на фактическом portrait экране и10-минутный проход (FPS/память/зависания). Записать модель устройства, Android и версиюAPK. Kill в момент сохранения и реальная storage failure — отдельные проверки, Editor fixture их не доказывает.
