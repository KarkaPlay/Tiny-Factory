# Backlog

Статусы: queued (ждёт зависимости) → ready → active → review → done; blocked с причиной. M01 принят как документный этап 0.1.0; остальные production-задачи ещё не начаты. Owner — исполнитель; Producer назначает reviewer и обновляет статус. Оценки приблизительные и включают совместную работу ролей.

## MVP
| ID | Задача / acceptance criteria | Owner | Зависит от | Статус | Оценка |
|---|---|---|---|---|---|
| M01 | Зафиксировать 2–4 звена, 3 upgrades и конечные unlocks; таблица цен/эффектов, схема HUD, первый сценарий 60 секунд и QA criteria без новых систем | Game Designer, UI | — | done: GAMEPLAY_SPEC, UI/Monetization review, QA PASS документа | 0.5 дня |
| M02 | Проверить Unity MCP/проект, Android toolchain и smoke build; описать UI/save решение и PluginYG2 capability matrix Editor/Android/другие выбранные targets с источниками, версией и unknowns | Engineer | —; плагин для окончательной проверки | ready | 0.5–1 день |
| M03 | Рабочая цепочка → продажа → валюта → upgrade; изменение выпуска измеримо, число объектов ограничено; Editor + Android smoke | Engineer | M01, техническая часть M02 | queued | 2 дня |
| M04 | Конечные upgrades/unlocks из данных; понятные состояния locked/affordable/max; нет отрицательных денег/повторной покупки | Engineer, Game Designer | M03 | queued | 0.5–1 день |
| M05 | Save/load денег, уровней и onboarding; restart/background/clean install/update/invalid-save/offline проверены; выбран один source of truth, cloud sync не добавлен автоматически | Engineer | M02, M04 | queued | 1 день |
| M06 | HUD/safe area/portrait, короткий tutorial, звук toggle; путь новичка до первой покупки проверен на устройстве | UI, Engineer | M04, M05 | queued | 1 день |
| M07 | Проверить design baseline GAMEPLAY_SPEC: rewarded x2 доход/60 active seconds, caps, default-off interstitial и 8 events; уточнить provider mapping и QA failure cases после capability matrix, не закрывать integration дизайном | Monetization | M01, capability matrix M02 | queued | 0.5 дня |
| M08 | Интеграция ads через boundary и fake Editor; completion ровно один раз, cancel/no-fill/error/offline/timeout возвращают игру в рабочее состояние; реальные тесты provider на выбранной платформе | Engineer | M02 plugin gate, M06, M07 | queued | 0.5–1 день |
| M09 | Analytics boundary/fake, параметры и точность отправки; проверка реального endpoint/debug view, gameplay не зависит от доступности сервиса | Engineer | M02 plugin gate, M07 | queued | 0.5 дня |
| M10 | Полный регресс, clean install/update, background/restore, 10 минут performance на названном Android-устройстве; нет release-blocking дефектов | QA, Engineer | M05, M06, M08, M09, P01 | queued | 1 день |
| M11 | Reproducible Android build и build notes: Unity/plugin version, настройки, путь артефакта, устройство, результаты и ограничения; README с управлением и demo | Engineer, QA, Producer | M10 | queued | 0.5 дня |

Следующая рекомендуемая задача — M02. Текущий запрос разрешает только M01/документацию, техническая работа ещё не начата. Отсутствие PluginYG2 блокирует окончательную capability matrix и реальные адаптеры; остальная техническая проверка полезна после отдельного запроса.

## Polish
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
