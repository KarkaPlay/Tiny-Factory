# Backlog

Статусы: queued (ждёт зависимости) → ready → active → review → done; blocked с причиной. M01 принят как документный этап 0.1.0; M02 принят как техническая основа 0.2.0 с APK и QA/provider PASS по D12. M03 принят как первый playable 0.3.0 для Editor/build scope; физический Android gate остаётся pending manual по D12. Owner — исполнитель; Producer назначает reviewer и обновляет статус. Оценки приблизительные и включают совместную работу ролей.

## Основа 0.1–0.4 и перенесённые задачи
| ID | Задача / acceptance criteria | Owner | Зависит от | Статус | Оценка |
|---|---|---|---|---|---|
| M01 | Зафиксировать 2–4 звена, 3 upgrades и конечные unlocks; таблица цен/эффектов, схема HUD, первый сценарий 60 секунд и QA criteria без новых систем | Game Designer, UI | — | done: GAMEPLAY_SPEC, UI/Monetization review, QA PASS документа | 0.5 дня |
| M02 | Проверить Unity MCP/проект, Android toolchain и smoke build; описать UI/save решение и PluginYG2 capability matrix Editor/Android/другие выбранные targets с источниками, версией и unknowns | Engineer | SDK/EDM для выбранной Yandex Mobile Ads | done: Editor smoke, YMA config/matrix, APK code 4 после импорта ad modules, QA PASS; device manual pending D12 | 0.5–1 день |
| M03 | Рабочая цепочка → продажа → валюта → upgrade; изменение выпуска измеримо, число объектов ограничено; Editor + Android smoke | Engineer | M01, техническая часть M02 | done: базовая цепочка/Speed L1, Editor tests + synthetic HUD path, APK code 6; device pending D12 | 2 дня |
| M04 | Конечные upgrades/unlocks из данных; понятные состояния locked/affordable/max; нет отрицательных денег/повторной покупки | Engineer, Game Designer | M03 | done (Editor/build): nine levels/unlocks, harness/UI review PASS, final APKcode10; device D12 pending | 0.5–1 день |
| M05 | Save/load денег, уровней и onboarding; restart/background/clean install/update/invalid-save/offline проверены; выбран один source of truth, cloud sync не добавлен автоматически | Engineer | M02, M04 | done (Editor/build): JSON/recovery/negative harness and notice review PASS; device/crash-replace D12 pending | 1 день |
| M06 | Новый HUD поля/склада/заказов, safe area/portrait, tutorial, sound/mute, newcomer на устройстве | UI, Engineer | N05, N06, N07 | queued: перенесён в 0.8.0; baseline UX входит уже 0.5 | Переоценить |
| M07 | Пересмотреть reward/event baseline под заказы; прежний GAMEPLAY_SPEC: rewarded x2 доход/60 active seconds, caps, default-off interstitial и 8 events; уточнить provider mapping и QA failure cases после capability matrix, не закрывать integration дизайном | Monetization | M01, capability matrix M02 | queued: переоценить под мету | Переоценить |
| M08 | Интеграция ads через boundary и fake Editor; completion ровно один раз, cancel/no-fill/error/offline/timeout возвращают игру в рабочее состояние; реальные тесты provider на выбранной платформе | Engineer | M02 plugin gate, M06, M07 | queued:0.9.0 после проверки меты | 0.5–1 день |
| M09 | Analytics boundary/fake, параметры и точность отправки; проверка реального endpoint/debug view, gameplay не зависит от доступности сервиса | Engineer | M02 plugin gate, M07 | queued: переоценить под мету | Переоценить |
| M10 | Полный регресс, clean install/update, background/restore, 10 минут performance на названном Android-устройстве; нет release-blocking дефектов | QA, Engineer | N05, N06, N07, M06, M08, M09, P01 | queued | 1 день |
| M11 | Reproducible Android build и build notes: Unity/plugin version, настройки, путь артефакта, устройство, результаты и ограничения; README с управлением и demo | Engineer, QA, Producer | M10 | queued: переоценить под мету | Переоценить |

M02 / v0.2.0 принят 2026-10-05: [TECHNICAL_FOUNDATION](TECHNICAL_FOUNDATION.md), [BUILD_NOTES](BUILD_NOTES.md), [M02_QA](M02_QA.md). Yandex Mobile Ads назначена, SDK/EDM установлены, APK code 4 с RewardedAdv/InterstitialAdv собран; Console errors 0. По D12 установка/запуск/performance на физическом Android остаются будущей ручной проверкой пользователя. Ad wrappers импортированы и compile/build проверены; real callbacks не проверены; PluginYG2 Metrica WebGL-only и не закрывает Android M09. Исторический результат M02; базовый gameplay теперь реализован в M03.

M03 / v0.3.0 принят 2026-10-05 для Editor/build scope: [M03_IMPLEMENTATION](M03_IMPLEMENTATION.md), [M03_QA](M03_QA.md), [BUILD_NOTES](BUILD_NOTES.md). Трасса t6/t42/t60 подтверждена Unity validation; saturated выпуск 39→58 за 120 с, backpressure без потерь; synthetic путь через кнопки завершает первую покупку. Исторический APK code 6; актуальный патч — code 7; code 5 отклонён из-за тестового объекта, исправление и повторная проверка выполнены. Физический install/run/performance и hardware input остаются pending D12. Следующий этап на момент приёмки M03 был M04–M05; его текущий статус указан выше.

## Polish

**M03-R03 / 0.3.3 — выполнен, authoring/Editor/build scope принят.** uGUI/factory/HUD сохранены в сцене; семь world/item/UI prefab assets, явные материалы/font/config/view references. FactoryPresentation builder и auto installer удалены, отдельный Bootstrap не нужен. Persistent button events save/reopen/count1 PASS, InputSystem actions resolve; synthetic clicks/LineView re-enable и deterministic economy PASS, startup frames9:16/19.5:9/20:9 reviewed Producer+UI. APKcode9 build success0 errors/1 warning. После build MCP исчез: PackedAssets/warning detail/postConsole pending; device0.3.3 NOT RUN. Owner Engineer, reviewer UI, QA Producer последовательно (thread limit). Без новых progression/saves/ads. [M033_IMPLEMENTATION](M033_IMPLEMENTATION.md), [M033_QA](M033_QA.md).


**M03-R02 / 0.3.2 — done: Editor/build PASS, rendering PASS по повторному запуску пользователя.** Все 3D объекты в APK0.3.1 розовые, UI корректен по реальному запуску на Realme RMX3834/Android15. Добавлены явный Resources material URP/Lit и sharedMaterial assignment; Editor7/7 world renderers PASS, build0 errors/2 warnings. [M032_QA](M032_QA.md). Owner Engineer; Producer выполняет последовательный QA review (QA agent недоступен). Критерии: выявлена и устранена причина pipeline/material/shader mismatch, Editor Console/scene clean, зависимости рендеринга включены в Android Player, новый APK собран. Пользователь после 0.3.2 сообщил «Теперь все работает» на ранее названном устройстве; user-reported rendering PASS. Полный device DoD не проверен. Gameplay и progression scope не меняются.


**M03-R01 / 0.3.1 — done для Editor/build scope.** Пользователь разрешил патч, Engineer реализовал подписи станций, видимый поток с DOTween, отклик сбора/продажи, объяснение автосбора и состояния Speed L1. Producer проверил финальные снимки и evidence: регресс экономики, visual queues/lifecycle, три портретные пропорции, APK code7. [M031_IMPLEMENTATION](M031_IMPLEMENTATION.md), [M031_QA](M031_QA.md). Дефекты перекрытия подписей и обрезания завершённой карточки исправлены. UI/Game Designer review предыдущих кандидатов выполнен; последнюю правку независимо повторно не проверяли. Критерий понимания игроком без устного объяснения остаётся pending реальная пользовательская игра, device gate D12 открыт. Полные onboarding/sound/progression/saves не входят в патч.

| ID | Задача / acceptance criteria | Owner | Приоритет |
|---|---|---|---|
| P01 | Минимальный обязательный feel: читаемые сбор/загрузка/заказ/покупка/обработка, лёгкие animation/VFX/sound, mute; не мешает видеть поток | UI, Engineer | До release, 0.5 дня |
| P02 | Короткое gameplay видео, 3–5 screenshots и описание вклада/архитектуры/ограничений; без ложных метрик | Producer, UI | До portfolio-ready, 0.5 дня |
| P03 | Дополнительные переходы/иконки/баланс после наблюдения игрока | UI, Game Designer | Только резерв |

## Мета — production0.5 и последующие этапы

Пользователь принял N01 и запустил N02–N05 отдельным production-запросом 2026-10-05. N06–N07 остаются будущими этапами. Старый бюджет1–3 недели не подтверждён для расширения; Engineer оценивает первый срез перед production.

| ID | Задача / acceptance criteria | Owner | Зависит от | Статус / версия |
|---|---|---|---|---|
| N01 | Документировать правила меты, интерфейс, сохранения и план; сверить сценарий и отсутствие тупиков | GD, UI, Producer | Запрос пользователя | done: пользователь принял активный дизайн и migration policy |
| N02 | Три authored площадки, ограниченное вертикальное движение камеры, выбор здания/карточка; жесты UI и камеры не конфликтуют | Engineer, UI reviewer | N01 review + production-запрос | done:0.5.0 Editor/build scope; QA + representative UI PASS; APKcode11 verified; device/playtest manual pending |
| N03 | Продукты/рецепты, локальные вход/выход/WIP и общий склад; атомарные сбор/загрузка, конечные улучшения, backpressure | Engineer, GD/QA reviewer | N01; код может идти параллельно N02, scene writer один | done:0.5.0 Editor/build scope; QA + representative UI PASS; APKcode11 verified; device/playtest manual pending |
| N04 | Два предложения/один активный заказ, набор1–2 продуктов, выполнимая генерация, сохранённые IDs/RNG, награда один раз, простой источник дохода | Engineer, GD/QA reviewer | N03 | done:0.5.0 Editor/build scope; QA + representative UI PASS; APKcode11 verified; device/playtest manual pending |
| N05 | Save2, migration/recovery, tutorial/UI; сквозной активный сценарий, APK и наблюдение игроков; время вне игры пока не засчитывается | Engineer, UI/GD/QA reviewer | N02–N04 | done:0.5.0 Editor/build scope; QA + representative UI PASS; APKcode11 verified; device/playtest manual pending |
| N06 | Баланс возвращений; ограниченный выпуск из загруженного сырья вне игры, атомарный checkpoint, summary и тест двух сессий | Engineer, GD/QA reviewer | N05 | queued:0.6.0 |
| N07 | После pilot review детализировать конечные главы, подарочную упаковку и одну ветвь; автоматизация снимает подтверждённую рутину | GD, UI, Engineer | N05/N06 playtest gate | queued:0.7.0, условный этап |

Код и схемы не подменяют evidence: сначала проверяем выбор в активной игре, затем возвращение, затем расширяем контент. Физический gate D12, ads M07–M09 и финальный DoD сохраняются.

## Post-MVP / Ideas

- Prestige, расширение сверх потолка4–5 производств, тема/скины.
- WebGL distribution, если пользователь выберет отдельный target.
- Локализация, дополнительные rewarded placements.
- Свободная стройка/вращение/zoom, дороги и стоимость расстояний, порча/сроки/ежедневные серии, cloud/social/live ops.

Offline production, ограниченное количество связанных фабрик и заказы перенесены из прежних идей в **принятый активный scope N02–N05 и будущие этапы N06–N07**. Это ещё не реализованный MVP. Дополнительные валюты и безлимитный каталог не включены.

Идеи не являются обязательствами. При добавлении идеи: проблема игрока, предполагаемая польза, стоимость; в MVP переносит только Producer после необходимого решения пользователя.

Рекомендации по будущим импортам M07/M08: [PLUGINYG2_MODULE_RECOMMENDATIONS](PLUGINYG2_MODULE_RECOMMENDATIONS.md). Минимальный нужный feature-модуль — RewardedAdv; InterstitialAdv только при принятом включении placement. Аудит не запускает интеграцию и не меняет design scope.

**M04–M05 / 0.4.0 — done для Editor/build scope (2026-10-05).** Прогрессия/save harness, Game Designer/UI/QA review PASS; финальный APKcode10 проверен после +4/+6 HUD fixes, build0 errors/1 existing warning, scene validation0/Console errors0. [M040_ACCEPTANCE](M040_ACCEPTANCE.md), [M040_QA](M040_QA.md), [BUILD_NOTES](BUILD_NOTES.md). Device D12,19.5:9/20:9 и process-kill/Android filesystem проверки остаются manual pending. Следующий этап перепланирован: N01 review → отдельный запуск N02–N05/0.5.0.

**N02–N05 /0.5.0 — done для Editor/build scope (2026-10-05).** User-approved active prototype; core QA and actual authored-button routes PASS, critical transfer HUD defect fixed, representative UI review PASS, APK0.5.0/code11 signature/manifest/hash/ABI verified. [M050_ACCEPTANCE](M050_ACCEPTANCE.md). Физические Android проверки и наблюдение игроков остаются manual; offline0.6+ не начаты.

## M051 — принятый патч 0.5.1

**Done для ограниченного Editor/build scope (2026-10-06):** по отзыву пользователя заменить нижний выбор количества кнопками у зданий: загрузка одного ресурса и сбор всего помещающегося объёма. Добавить наглядные движения производства/товаров и скролл с примагничиванием и автоматическим выбором участка.

Owner — Engineer; UI review — UI Visual; регрессия — QA Release. Критерии: загрузка 1+1 в сушильню, сохранение неполного входа, caps/failure; реальные кнопочные маршруты; эффекты только после commit и корректная остановка; скролл/snap/selection/bounds/save и изоляция UI-жестов; чистая сцена/Editor и проверенный APK 0.5.1/code 12. Рецепты, экономика, заказы, schema2 и v1 migration сохраняются. Offline 0.6 не запускается. [M051_UI_SPEC](M051_UI_SPEC.md), [M051_QA](M051_QA.md).

M051 evidence: [M051_ACCEPTANCE](M051_ACCEPTANCE.md). APK 0.5.1/code12 manifest/signature/CRC/ARM64/IL2CPP проверен; post-build scene/Console clean. Неполная visual/input матрица и startup-snap persistence отмечены, device gate открыт.
