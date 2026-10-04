# Backlog

Статусы: queued (ждёт зависимости) → ready → active → review → done; blocked с причиной. M01 принят как документный этап 0.1.0; M02 принят как техническая основа 0.2.0 с APK и QA/provider PASS по D12. M03 принят как первый playable 0.3.0 для Editor/build scope; физический Android gate остаётся pending manual по D12. Owner — исполнитель; Producer назначает reviewer и обновляет статус. Оценки приблизительные и включают совместную работу ролей.

## MVP
| ID | Задача / acceptance criteria | Owner | Зависит от | Статус | Оценка |
|---|---|---|---|---|---|
| M01 | Зафиксировать 2–4 звена, 3 upgrades и конечные unlocks; таблица цен/эффектов, схема HUD, первый сценарий 60 секунд и QA criteria без новых систем | Game Designer, UI | — | done: GAMEPLAY_SPEC, UI/Monetization review, QA PASS документа | 0.5 дня |
| M02 | Проверить Unity MCP/проект, Android toolchain и smoke build; описать UI/save решение и PluginYG2 capability matrix Editor/Android/другие выбранные targets с источниками, версией и unknowns | Engineer | SDK/EDM для выбранной Yandex Mobile Ads | done: Editor smoke, YMA config/matrix, APK code 4 после импорта ad modules, QA PASS; device manual pending D12 | 0.5–1 день |
| M03 | Рабочая цепочка → продажа → валюта → upgrade; изменение выпуска измеримо, число объектов ограничено; Editor + Android smoke | Engineer | M01, техническая часть M02 | done: базовая цепочка/Speed L1, Editor tests + synthetic HUD path, APK code 6; device pending D12 | 2 дня |
| M04 | Конечные upgrades/unlocks из данных; понятные состояния locked/affordable/max; нет отрицательных денег/повторной покупки | Engineer, Game Designer | M03 | ready: M03 принят; следующий production этап 0.4.0 | 0.5–1 день |
| M05 | Save/load денег, уровней и onboarding; restart/background/clean install/update/invalid-save/offline проверены; выбран один source of truth, cloud sync не добавлен автоматически | Engineer | M02, M04 | queued | 1 день |
| M06 | HUD/safe area/portrait, короткий tutorial, звук toggle; путь новичка до первой покупки проверен на устройстве | UI, Engineer | M04, M05 | queued | 1 день |
| M07 | Проверить design baseline GAMEPLAY_SPEC: rewarded x2 доход/60 active seconds, caps, default-off interstitial и 8 events; уточнить provider mapping и QA failure cases после capability matrix, не закрывать integration дизайном | Monetization | M01, capability matrix M02 | queued | 0.5 дня |
| M08 | Интеграция ads через boundary и fake Editor; completion ровно один раз, cancel/no-fill/error/offline/timeout возвращают игру в рабочее состояние; реальные тесты provider на выбранной платформе | Engineer | M02 plugin gate, M06, M07 | queued | 0.5–1 день |
| M09 | Analytics boundary/fake, параметры и точность отправки; проверка реального endpoint/debug view, gameplay не зависит от доступности сервиса | Engineer | M02 plugin gate, M07 | queued | 0.5 дня |
| M10 | Полный регресс, clean install/update, background/restore, 10 минут performance на названном Android-устройстве; нет release-blocking дефектов | QA, Engineer | M05, M06, M08, M09, P01 | queued | 1 день |
| M11 | Reproducible Android build и build notes: Unity/plugin version, настройки, путь артефакта, устройство, результаты и ограничения; README с управлением и demo | Engineer, QA, Producer | M10 | queued | 0.5 дня |

M02 / v0.2.0 принят 2026-10-05: [TECHNICAL_FOUNDATION](TECHNICAL_FOUNDATION.md), [BUILD_NOTES](BUILD_NOTES.md), [M02_QA](M02_QA.md). Yandex Mobile Ads назначена, SDK/EDM установлены, APK code 4 с RewardedAdv/InterstitialAdv собран; Console errors 0. По D12 установка/запуск/performance на физическом Android остаются будущей ручной проверкой пользователя. Ad wrappers импортированы и compile/build проверены; real callbacks не проверены; PluginYG2 Metrica WebGL-only и не закрывает Android M09. Исторический результат M02; базовый gameplay теперь реализован в M03.

M03 / v0.3.0 принят 2026-10-05 для Editor/build scope: [M03_IMPLEMENTATION](M03_IMPLEMENTATION.md), [M03_QA](M03_QA.md), [BUILD_NOTES](BUILD_NOTES.md). Трасса t6/t42/t60 подтверждена Unity validation; saturated выпуск 39→58 за 120с, backpressure без потерь; synthetic путь через кнопки завершает первую покупку. Актуальный APK code 6; code 5 отклонён из-за тестового объекта, исправление и повторная проверка выполнены. Физический install/run/performance и hardware input остаются pending D12. Следующая задача — M04–M05 / 0.4.0, не запущена этим запросом.

## Polish

**Дефект M03-R01 / предлагаемая 0.3.1 — читаемость core loop.** Статус: ready к отдельному production-запросу; Owner UI + Engineer, review Game Designer. Пользовательская обратная связь и повторный screenshot/code review показали, что техническая приёмка M03 не подтвердила понимание игроком. Минимум: подписи у станций, заметный сбор/путь предмета/продажа +4, объяснение автоисточника, явная цель Speed L1 и причины недоступности. Критерий: игрок без устного объяснения начинает сбор, связывает предмет с продажей, понимает автоматический сбор и эффект покупки. DOTween уже импортирован пользователем и может использоваться для presentation после проверки настройки; симуляция остаётся источником истины. Новые механики, полный onboarding, sound/VFX и device UX pass сюда не добавляем.

| ID | Задача / acceptance criteria | Owner | Приоритет |
|---|---|---|---|
| P01 | Минимальный обязательный feel: читаемые продажа/покупка/обработка, лёгкие animation/VFX/sound, mute; не мешает видеть поток | UI, Engineer | До release, 0.5 дня |
| P02 | Короткое gameplay видео, 3–5 screenshots и описание вклада/архитектуры/ограничений; без ложных метрик | Producer, UI | До portfolio-ready, 0.5 дня |
| P03 | Дополнительные переходы/иконки/баланс после наблюдения игрока | UI, Game Designer | Только резерв |

## Post-MVP / Ideas
- Offline earnings с bounded elapsed time и тестами изменения часов.
- Дополнительные фабрики/товары, prestige, тема/скины.
- WebGL distribution, если пользователь выберет отдельный target.
- Локализация, дополнительные rewarded placements.
- Продление целей после полной настройки чайной линии: новые цели/заказы могли бы улучшить повторные сессии, но требуют отдельного дизайна и интерфейса; в M01 не включены. Сначала проверить на прототипе существующий короткий сценарий и перебалансировать цены без новых систем.

Идеи не являются обязательствами. При добавлении идеи: проблема игрока, предполагаемая польза, стоимость; в MVP переносит только Producer после необходимого решения пользователя.

Рекомендации по будущим импортам M07/M08: [PLUGINYG2_MODULE_RECOMMENDATIONS](PLUGINYG2_MODULE_RECOMMENDATIONS.md). Минимальный нужный feature-модуль — RewardedAdv; InterstitialAdv только при принятом включении placement. Аудит не запускает интеграцию и не меняет design scope.
