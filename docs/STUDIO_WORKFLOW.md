# Работа виртуальной студии

## Запуск и поддерживаемый формат
Producer — основная роль текущего чата по AGENTS.md. Можно явно написать: «Используй $tf-producer: подготовь первую production-задачу». Это не запускает шесть постоянных процессов и не создаёт отдельные чаты.

Skills живут в `.agents/skills/<name>/SKILL.md`; YAML frontmatter содержит name/description. `agents/openai.yaml` — только UI metadata skill, не custom agent. Custom agents — самостоятельные `.codex/agents/<name>.toml` с name/description/developer_instructions и ссылкой на skill. В каждом файле заданы model и model_reasoning_effort: Producer — `gpt-6.1-sol` / `medium`, остальные пять ролей — `gpt-6-luna` / `high`. Sandbox и MCP наследуются; глобальные настройки не меняются. Отдельный `.codex/config.toml` не требуется. Вызов skill в текущем чате сам по себе не переключает выбранную пользователем модель чата.

Формат проверен по актуальной [документации skills](https://learn.chatgpt.com/docs/build-skills) и [custom agents](https://learn.chatgpt.com/docs/agent-configuration/subagents), локальный CLI — 0.160.0. После создания файлы могут потребовать новый чат/перезапуск для обнаружения; trust/permissions определяются клиентом. Формат не гарантирует, что конкретная текущая tool surface умеет выбрать custom agent type.

## Роли и минимальный контекст
| Agent / Skill | Owner | Читать по задаче |
|---|---|---|
| tf-producer | Scope, roadmap, backlog, decisions, CURRENT_STATE, интеграция | CURRENT_STATE + нужные строки BACKLOG/ROADMAP; BRIEF при продуктовых решениях; DoD при приёмке |
| tf-game-design | Core loop, экономика, unlocks, onboarding/rewards | BRIEF + конкретная задача/данные баланса; DECISIONS только релевантные |
| tf-unity-engineer | Gameplay/data/state/save/UI implementation, SDK adapters, performance/build | Технический target BRIEF + конкретные файлы/спецификация; DoD перед handoff |
| tf-ui-visual | HUD, UX, visual direction, feedback/asset requirements | UX/visual BRIEF + конкретный экран/сценарий |
| tf-monetization | Ads policy/rewards, event schema/metrics | Монетизация BRIEF + PluginYG2 matrix/релевантные решения и gameplay rewards |
| tf-qa-release | Acceptance criteria, сценарии/regression, build readiness | DoD + задача, actual build/device и изменённые системы |

Специалист не должен читать все документы и skills. Полученный task brief определяет нужные ссылки. Более подробные specs добавляем только когда появилась конкретная feature; не создаём пустые папки для будущих отчётов.

## Делегирование
1. Пользователь задаёт цель Producer. Producer сверяет scope/state, выбирает backlog ID или добавляет обоснованную задачу, определяет acceptance criteria и owner.
2. Для содержательной профильной работы Producer должен вызвать specialist subagent. Если tool умеет agent type — выбрать thin definition; иначе передать subagent точный путь `.agents/skills/<name>/SKILL.md` и попросить прочитать его, явно задав model/reasoning_effort из agent TOML. При model override использовать допустимый режим fork_turns (например `none`) с достаточным task brief. Если модель недоступна, сообщить blocker и запросить замену; не менять её молча. Никакой выдуманной команды выбора агента.
3. Один specialist может закрыть связный блок задач; не создавать агента на каждую мелочь. Обычно один executor и один reviewer последовательно. Независимые design/research/review задачи можно параллелить в доступных лимитах, не запуская всю студию.
4. Назначить writable paths и read-only reviewer. Один владелец пишет сцену/prefab или общий файл в каждый момент; Unity MCP serial, не два одновременных редактора. Не перетирать чужие изменения. Specialists не делегируют далее без поручения Producer.
5. Producer ждёт handoff, проверяет actual files/evidence, направляет результат нужному reviewer, возвращает конкретные проблемы owner. QA не подменяет реализацию исправлениями без назначения.
6. После проверки Producer интегрирует согласованные результаты, обновляет статус/память и сообщает пользователю результат и ограничения. Публикация/платёж/важное изменение scope требуют отдельного решения пользователя.

Если delegation tools недоступны, использовать тот же workflow последовательно в одном чате, явно обозначив роли и отсутствие независимого review. Не утверждать, что specialist был запущен, если его не было.

## Task brief
Краткий шаблон для prompt subagent, не обязательный отдельный файл:

```text
Task ID / цель:
Роль и путь SKILL.md:
Контекст: только нужные документы/разделы/файлы.
Входные данные и принятое решение:
Acceptance criteria:
Разрешённые файлы/системы; что не входит в задачу:
Зависимости и известные unknowns:
Проверки / evidence; кому передать дальше:
Верни handoff; scope не расширяй, идеи вынеси отдельно.
```

## Specialist handoff
```text
Task ID / статус (готово к review, blocked, partial):
Сделано:
Решения (принятые в задаче / предложения):
Затронутые файлы/системы:
Проверки: шаг/ожидание/факт, build/device или ссылка; что не проверено:
Следующему специалисту:
Риски / TODO / blockers:
Post-MVP идеи (если есть):
```

Handoff обычно 5–12 строк плюс необходимое evidence; не копировать полный tool log. Producer получает результаты через subagent result/mailbox; сообщения в пользовательские чаты или внешние сервисы не разрешаются этим workflow.

## Память и review
- Producer — единственный writer BACKLOG/ROADMAP/CURRENT_STATE/DECISIONS, пока явно не назначил другого. CURRENT_STATE короткий, обновляется после meaningful completion/blocker, не после каждой команды.
- Специалист возвращает рекомендации для общих документов; принятие факта/решения делает Producer. Уточнённый BRIEF и DoD проходят review соответствующей роли.
- Для риска saves/rewards QA проверяет отрицательные сценарии; для экономики — Game Designer, для UX — UI Designer, для интеграции provider — Monetization + QA. Не нужен полный круг всех ролей на каждую правку.
- Конфликт разрешается по приоритету: рабочий законченный MVP → feel/читаемость → простая архитектура → дополнительный polish. Незакрытое продуктовое решение вынести пользователю с конкретными вариантами и стоимостью; независимую работу продолжить.

## Проверка bootstrap
Проверено 2026-10-05:
- Штатный initializer skill-creator использован для всех шести skills; quick_validate подтвердил формат каждого SKILL.md.
- Разобраны 6 UI YAML и 6 agent TOML; проверены обязательные поля, совпадение имени, ссылка agent→skill, default_prompt и длина UI descriptions. Все локальные Markdown links существуют, scaffold placeholders отсутствуют.
- `codex debug prompt-input` локального CLI 0.160.0 обнаружил все 6 project skills в каталоге доступных skills и корневой AGENTS.md в контексте. `--strict-config` для этой диагностической команды не поддерживается; его не считать пройденной проверкой.
- Thin agent definitions проверены по документированной standalone schema и TOML parser; runtime запуск каждой роли не выполнялся. Текущая desktop tool surface может потребовать передачу пути skill вместо выбора agent type.
- Tracked Unity-файлы не изменены; проверены whitespace и итоговый набор новых файлов. Gameplay/Unity MCP/Android не тестировались в bootstrap.

Validation зависимости и diagnostic output остаются во временной папке, не в Unity/repository. Нативная диагностика подтверждает обнаружение skills, а не независимую работу команды или готовность игры.
