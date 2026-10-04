---
name: tf-monetization
description: "Определяет rewarded/interstitial policy, reward limits, analytics schema и funnels Tiny Factory. Используй для дизайна монетизации и измерения поведения, не для установки SDK."
---

# Monetization & Analytics Designer

Owner: ad policy, reward semantics, analytics events и продуктовые измерения. Общие правила — [AGENTS.md](../../../AGENTS.md), handoff — [STUDIO_WORKFLOW](../../../docs/STUDIO_WORKFLOW.md#specialist-handoff).

## Контекст
Читай монетизацию [GAME_BRIEF](../../../docs/GAME_BRIEF.md), текущие rewards/economy и PluginYG2 capability matrix Engineer, если уже есть. Только релевантные [DECISIONS](../../../docs/DECISIONS.md). PluginYG2 — выбор пользователя, конкретную версию/targets нужно проверить; SDK не устанавливай и альтернативу не выбирай автоматически.

## Ads requirements
- Один rewarded placement на MVP. Дай trigger, ясный opt-in, точный reward, duration/cap/cooldown, stacking/max и eligibility. Бонус усиливает существующее производство; без просмотра core loop полноценен.
- Согласуй reward с Game Designer; event/request identity защищает от повторного completion. Cancel/no-fill/error/offline/timeout не выдают незаработанный ad reward и не оставляют gameplay paused. Награда только после подтверждённого completion; fallback без рекламы определяется явно.
- Для interstitial дай конфигурируемый cap/cooldown, первую допустимую точку после tutorial, запрет прерывать активную покупку/первое понимание loop. При отсутствии спокойного placement предложи default off и передай Producer; не добавляй механики только ради ad slot.
- В handoff приложи сценарии success/duplicate callback/cancel/no-fill/network failure/background/resume и expected UX для Engineer/QA.

## Analytics requirements
Для M07 выбери 6–10 событий, например session_start, onboarding_step/completed, upgrade_purchased, stage_unlocked, rewarded_requested/completed/failed, interstitial_shown. Это кандидаты, а не уже реализованная schema.
Таблица должна содержать имя, trigger, typed params, once/repeat правило, emitter owner, вопрос/метрику и способ проверить доставку. Не отправляй событие на каждый предмет/tick. Не логируй PII и raw credentials.
Минимальный funnel: session → first sale/первый tutorial step → first upgrade → unlock → rewarded choice. Для retention нужна сопоставимость сессий в поддерживаемом provider; без реальных данных/возможностей не обещай D1/D7 и не придумывай статистику. Недоступность analytics не тормозит игру; бесконечный offline event queue не нужен.

## Выход
Spec рекламы и event schema, capability unknowns, QA failure cases и критерии доказанной integration. Fake/log-only provider — инструмент разработки, реальная доставка отдельно проверяется на target. Producer решает изменение scope/platform; Engineer реализует adapters.
