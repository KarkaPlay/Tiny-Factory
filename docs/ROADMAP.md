# Roadmap

Обновлено: 2026-10-06. **M051 / 0.5.1 реализован, APK code12 проверен; targeted QA и representative UI review с ограничениями — [M051_ACCEPTANCE](M051_ACCEPTANCE.md). Device gate открыт.** **0.5.0 / N02–N05 приняты для Editor/build scope: три производства, ручные партии/склад/заказы/upgrades/save2/migration, QA + representative UI PASS, Android APK0.5.0/code11 проверен. Device/playtest gates остаются manual. Offline production и0.6+ не включены в этот запуск.** Будущие этапы — по отдельному запросу и review. Источники правил: [META_GAMEPLAY_SPEC](META_GAMEPLAY_SPEC.md), [META_UI_SPEC](META_UI_SPEC.md), [M050_ACCEPTANCE](M050_ACCEPTANCE.md).

Первоначальный план одной линии на 10 рабочих дней + резерв заменён этим планом будущих этапов. Достигнутые результаты 0.1–0.4 сохраняются. Новая мета затрагивает модель производства, запасы, экономику, сцену, UI и save semantics; старый бюджет 1–3 недели не переносится автоматически. Числовые сроки не обещаны: Engineer оценивает 0.5.0 перед её запуском, следующие этапы переоцениваются после вертикального среза. Версии обозначают результаты, а не календарные даты.

## Выполненная основа

| Версия | Результат | Статус / evidence |
|---|---|---|
| 0.1.0 | M01: спецификация исходной одной линии | Документный этап принят; [GAMEPLAY_SPEC](GAMEPLAY_SPEC.md) теперь исторический baseline0.1–0.4. |
| 0.2.0 | M02: Unity/Android, uGUI/save выбор, PluginYG2 matrix | Editor/build PASS, code4; real ads/analytics/device gates не закрыты. [TECHNICAL_FOUNDATION](TECHNICAL_FOUNDATION.md). |
| 0.3.0–0.3.3 | M03 и patches: core loop, readability, material fix, authored scene/prefabs | Editor/build принято;0.3.2 rendering PASS по сообщению пользователя. [BUILD_NOTES](BUILD_NOTES.md). |
| **0.4.0** | M04–M05: finite upgrades/unlocks, local JSON/recovery | Editor/build PASS, code10. Пользователь сообщил «кажется версия 0.4.0 работает»; это положительная обратная связь, не полный device regression. [M040_QA](M040_QA.md). |

## Следующие этапы

| Версия | Этап | Конкретный результат / gate | Owner / задачи |
|---|---|---|---|
| **До 0.5.0** | Review дизайна меты | Пользователь принял scope/правила/поле и migration contract 2026-10-05; production0.5 запущен. Engineer оценивает реализацию и риски до первого handoff. | Producer, GD, UI; N01 |
| **0.5.0** | Активный вертикальный срез | Три площадки на прокручиваемом поле; local input/output, общий склад, ручные collect/load, два предложения/один активный заказ, награда ровно один раз, покупка/улучшения из данных. Auto-sale старой линии заменена контрактами. Save новой модели и безопасный переход 0.4 обязательны уже здесь. Понятный tutorial до первого заказа; цель среза — три построенных узла и смешанный контракт на сухой чай/пакетики. Playtest проверяет выбор распределения. Это активный прототип, ожидание вне игры не засчитывается. | Engineer, GD/UI review, QA; N02–N05 |
| **0.5.1 (Editor/build принято)** | Наглядность и прямое управление | User-requested patch M051: resource/production world animations, near-building load+1/collectallfit, scrollsnap и автоматический выбор видимого участка. Runtime рецепты/экономика/save2 прежние, odd queuedinput послеload+1 сохраняется. Editor targetedQA/UI и APKcode12; device manual отдельно. | Engineer, UI, QA; M051 done с ограничениями покрытия |
| **0.6.0** | Возвращение и offline production | Сначала отдельный review ёмкости/длительностей под возвращения (быстрый профиль0.5 сюда не переносится); затем сохранённые запасы/input/output/WIP/заказы и ограниченный elapsed-time выпуск только загруженных производств, без auto collect/load/денег. Повторный resume не удваивает выпуск, часы/cap/crash cases проверены. Две разнесённые сессии дают понятный следующий шаг. Device D12 остаётся отдельным gate. | Engineer, GD/QA; N06 |
| **0.7.0** | Главы роста и снижение рутины | Только после playtest0.5/0.6: конечные главы предприятия, подарочная упаковка и одна дополнительная ветвь в пределах согласованного потолка. Автоматизация освоенного маршрута вводится лишь если тест показал курьерскую рутину. Детальный balance/content review перед запуском; после финальной главы видимое завершение. | GD + Engineer, UI/QA review; N07 |
| **0.8.0** | Мобильный UX и game feel | Safe area и portrait16:9/9:16/19.5:9/20:9, панорамирование/карточки/заказы без конфликтов, sound/mute, читабельные статусы и возвращение. Newcomer проходит весь основной сценарий на устройстве без рекламы. | UI + Engineer, QA; M06 переработан, P01 |
| **0.9.0** | Пересмотр рекламы и аналитики | Старый x2 auto-sale и event schema не переносятся на заказы автоматически. Monetization предлагает подходящий placement/reward и события; пользователь принимает изменение policy/provider при необходимости. Реальные adapters/exactly-once/error/delivery подтверждаются на target; fake не закрывает gate. | Monetization + Engineer, QA; M07–M09 переработаны |
| **1.0.0** | Законченный MVP/портфолио | Конечная кампания и full device DoD: install/update/migration/offline/background/order/upgrade/unlock, performance и build reproduction. README/video/screenshots с честными статусами. Публикация отдельно. | QA, Engineer, Producer; M10–M11, P02 |
| **1.0.1**, если нужно | Стабилизация | После принятого 1.0: defects/balance/polish без новых систем. Если DoD не закрыт — завершаем 1.0, не объявляем фиктивный release. | По дефектам; P03 |

Реклама/аналитика остаются исходными продуктовыми задачами, но отложены до проверки нового core/meta loop. Если понадобится исключить их из release, это отдельное решение пользователя. Подготовка design docs не подтверждает retention или готовность любого будущего APK.

## Gates и правила версий

- N01 — design review, N02–N06 — проверяемая новая петля; баланс возвращений0.6 ещё требует отдельной подготовки; N07 расширяется только после наблюдения игроков. Новые идеи не превращаются в обязательства молча.
- 0.5.0 не рекламируется как idle/ежедневная игра: offline появляется 0.6.0. Проверка интереса к двум сессиям не равна доказанному ежедневному удержанию.
- Save1 старой линии не перезаписывается при разработке новой схемы. Migration и backup имеют отдельную приёмку; отсутствие процесса миграции блокирует передачу 0.5 поверх 0.4.
- Формат major.minor.patch; patches исправляют принятый этап. Android version code увеличивается при следующей передаваемой сборке; фактические значения и артефакты записываются в BUILD_NOTES.
- Unity через MCP, один writer сцены; сохраняем GUID/.meta/packages/providers и пользовательские изменения. Scope/design request не разрешает Editor implementation.
- D12: Editor/build и user-reported smoke не закрывают Android device DoD. Публикация, платные зависимости, изменение платформы требуют отдельного решения.
