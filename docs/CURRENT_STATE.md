# Current state

Обновлено: 2026-10-05. Принят этап **0.1.0 — спецификация Tiny Factory / M01**. Это документация, игрового билда нет.

- Источник правил: [GAMEPLAY_SPEC](GAMEPLAY_SPEC.md). Карманная чайная мастерская, одна линия максимум из четырёх узлов с источником, один пакетик чая, монеты, три направления по три уровня, два unlock.
- Game Designer подготовил дизайн; UI-visual и Monetization дали профильные read-only reviews. QA после устранения неоднозначностей дал PASS документной согласованности; Producer принял handoffs, scope и проверяемые AC. Все четыре specialist subagents вызваны через настроенные роли GPT-6 Luna / high. Это проверка вызова ролей, не Unity runtime.
- Арифметика воспроизведена Producer: первая продажа t6, первая покупка t42, unlock t65/t127, все upgrades t207; на t300 184 продажи и 563 монеты после трат. Hypotheses для прототипа: короткие цели 3–5 минут, ценность Speed L2, различимость bottlenecks, понятность cold-load сброса WIP.
- Принят baseline x2 доход/60 active seconds, ограниченный opt-in rewarded, interstitial default-off и 8 analytics events. Actual SDK behavior/delivery неизвестны; M07 не закрыт документацией.
- Unity MCP проверен 2026-10-05: один активный экземпляр `Tiny Factory`, путь `/Users/sergeikarpuskin/Documents/My Small Games/Tiny Factory/Assets`, Unity 6000.3.9f1. Editor state свежий и `ready_for_tools=true`, открыт `Assets/Scenes/SampleScene.unity`, Play Mode выключен, компиляция не идёт, Console Error query вернул 0 записей. Read-only поиск нашёл `Main Camera`. Это подтверждает живой доступ к правильному Editor/project; Play Mode, проектные assets/packages шире этого запроса, Android и smoke build не проверялись.
- Частичная проверка M02: живое подключение Unity MCP к правильному Editor/project подтверждено; остаются toolchain, UI/local save выбор, Android smoke build на указанном устройстве и PluginYG2 capability matrix. Плагин добавит пользователь; его текущая версия, Android/analytics/save semantics, модули и устройство ещё не подтверждены.
- После M02 — M03: базовая цепочка и первая покупка по спецификации. M04–M06 добавляют полную прогрессию, saves и UX; M07–M09 ждут capability gate. Никакая техническая задача текущим design-only запросом не запущена.
- BACKLOG, ROADMAP, GAME_BRIEF и DECISIONS актуализированы. Следующий рекомендуемый запрос — M02 / 0.2.0 Unity Engineer; ближайший playable — M03 / 0.3.0.
