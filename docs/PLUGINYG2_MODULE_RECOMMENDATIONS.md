# Рекомендации по модулям PluginYG2 для Tiny Factory

Статус: историческая дизайн-рекомендация перед импортом рекламных модулей, 2026-10-05. Позже пользователь добавил RewardedAdv v1.011 и InterstitialAdv v1.02; текущий состав прошёл M02 smoke/build code 4 ([BUILD_NOTES](BUILD_NOTES.md)). Таблицы ниже фиксируют состояние и рекомендации на момент аудита; реальные ads/callbacks ещё не проверены.

## Текущее состояние и решение

В проекте есть PluginYG2 core `v2.0092`, platform-модули YandexGames `v1.01` и YandexMobileAds `v1.1`, а также feature-модуль Metrica `v1.02`. Android APK успешно собран с Yandex Mobile Ads Unity SDK `8.5.0`, EDM `1.2.190` и AppMetrica AdRevenue adapter `1.1.0`. В `ModulesListYG2.txt` из feature-модулей указан только Metrica; `RewardedAdv`, `InterstitialAdv` и `BannerAdv` отсутствуют. Назначение YMA platform asset и успешная сборка сами по себе не подтверждают runtime-инициализацию или доставку рекламы. Подробности — в [TECHNICAL_FOUNDATION](TECHNICAL_FOUNDATION.md#pluginyg2-capability-matrix).

| Решение | Модуль / слой | Применимость |
|---|---|---|
| **Нужен для MVP; подготовить mapping в M07, интегрировать в M08 после gate** | `RewardedAdv` (`1.011` в кэшированном каталоге проекта) | Нужен для единственного opt-in placement: удвоить монеты с продаж на 60 секунд активной игры. Документация показывает `YG2.RewardedAdvShow(id, callback)` и `onRewardAdv(id)`. Версию из каталога надо считать кэшированной, а не доказательством актуальности или совместимости с core `2.0092` и YMA `1.1`. До интеграции проверить доступность, API и callback semantics. В исходнике YMA `1.1` вознаграждение сейчас вызывается через `YGInsides.RewardAdv()` без игрового reward ID; Engineer должен обеспечить соответствие одному открытому request и однократному grant. QA-сценарии: duplicate/late callback, cancel, no-fill, error, offline, background/resume. При сбое бонус не начислять. |
| **Условный; оставить выключенным** | `InterstitialAdv` (`1.02` в кэшированном каталоге) | В дизайне есть только кандидатная спокойная точка после unlock reveal; interstitial остаётся default-off до отдельных UX, QA и provider/target gates. Автоматически не активировать. Если Producer позже примет решение включить placement, это нужный wrapper для `YG2.InterstitialAdvShow()`; caps/cooldown остаются ответственностью Tiny Factory. |
| **Не добавлять в MVP** | `BannerAdv` (`1.0`), `StickyAdv` (`1.0`) | Для них нет placement в портретном HUD фабрики. |
| **Уже установлен; не решает Android gameplay analytics** | `Metrica` (`1.02`) | По локальной capability matrix текущая реализация ограничена WebGL. Она не обеспечивает Android events M09. Внешний AppMetrica AdRevenue adapter — отдельная зависимость SDK и не gameplay analytics. Android event delivery остаётся открытым gate; этот отчёт не выбирает нового provider/SDK. |
| **Не требуется по текущему дизайну** | Остальные feature-модули из каталога ниже | Они не нужны текущему single-player MVP. В частности, не заменять выбранное локальное versioned JSON-хранилище модулем Storage: у него иной save путь и не доказаны нужные backup/schema/recovery свойства. |

## Кэшированный каталог проекта

`Assets/PluginYourGames/Editor/ServerInfoYG2.json` содержит кэшированный список названий и версий, который PluginYG2 использует в модульном инструменте. Это полезнее документационных упоминаний для точных имён и записей версий, но это не подтверждение, что список актуален, версия доступна для загрузки сейчас или совместима с нашим runtime. Platform entries, feature modules и editor tool — разные категории.

| Категория | Запись каталога и версия | Примечание |
|---|---|---|
| Core | `PluginYG2 2.0092` | Установлено. |
| Реклама | `RewardedAdv 1.011`, `InterstitialAdv 1.02`, `BannerAdv 1.0`, `StickyAdv 1.0` | Feature-модули; наличие в кэше не доказывает совместимость конкретных версий. |
| Данные игрока / аккаунт | `Authorization 1.023`, `Storage 1.021`, `PlayerStats 1.01`, `RedefinePlayerPrefs 1.001`, `Leaderboards 1.01` | Сохранения и аккаунтные функции; `PlayerStats` и `Leaderboards` в каталоге зависят от Authorization. Не требуются текущему MVP. |
| Другие feature-модули | `EnvirData 1.0031`, `Localization 1.02`, `AutoTranslateLangs 1.003`, `Payments 1.02`, `Metrica 1.02`, `Review 1.0`, `ServerTime 1.02`, `Fullscreen 1.0`, `Flags 1.001`, `OpenURL 1.002`, `TV 1.0`, `GameLabel 1.0`, `QuitGameEvent 1.001`, `Clipboard 1.01`, `TextGameLoad 1.0` | Опциональные возможности. `OpenURL` и `TV` имеют зависимость EnvirData в каталоге. `Async Multiplayer` описан в локальной документации, но отдельной записи с точным module ID в этом кэше нет. |
| Platform-модули | `YandexGames 1.01`, `YandexMobileAds 1.1`, `CrazyGames 1.041`, `GamePix 1.0`, `GameDistribution 1.0`, `GameMonetize 1.0`, `Y8 1.0`, `Appodeal 1.0`, `WelwiseGames 1.0`, `EmptyWebGL 1.0` | Это адаптеры целевой платформы. В данном проекте для Android назначен YandexMobileAds. Запись в каталоге не подтверждает поддержку каждого feature-модуля каждой платформой. |
| Инструмент | `MobileADBConnect 1.0` | Вспомогательный инструмент, не runtime feature-модуль и не часть текущей задачи. |

## Технические границы, влияющие на выбор

- **Storage не нужен как замена текущему save design.** Вендор описывает Storage с `SavesYG`/`SaveProgress`; для Android локальный fallback использует PlayerPrefs. `PlayerStats` предназначен для облачных integer-значений, а `RedefinePlayerPrefs` переопределяет PlayerPrefs. Эти возможности не подтверждают versioned JSON, backup и recovery проекта. По D11 оставляем выбранный локальный JSON путь; cloud save не входит в дизайн.
- **Пауза — API ядра, отдельный pause-модуль не нужен.** Встроенный `YG2.onPauseGame`/`PauseGame` и `PauseGameYG.SetState` — средства синхронизации паузы, а не дополнительный standalone module. Реальное поведение при Android background/resume всё равно нуждается в runtime/device проверке.
- **Safe Area — Unity API.** Для экранных отступов доступен `Screen.safeArea`; EnvirData не является источником safe area и не нужен ради неё. EnvirData даёт метаданные окружения вроде языка и типа устройства.
- **Localization и ServerTime — условные функции, не MVP-зависимости.** Текущая локализация/онбординг не требует provider module; offline earnings отсутствует, поэтому серверное время сейчас не нужно.

## Подтверждение и ограничения

- Текущая страница вендора описывает Yandex Mobile Ads как Android/iOS platform integration с interstitial, rewarded-video и banner форматами; отдельно требует Yandex Mobile Ads SDK и EDM. Это подтверждает назначение слоя, но не runtime для точных версий проекта. [PluginYG2 — Yandex Mobile Ads](https://max-games.ru/plugin-yg/doc/yandex-mobile-ads/)
- Страница RewardedAdv называет feature-модуль, строковый reward ID, callback/event завершения и события загрузки/ошибки/паузы. [PluginYG2 — Rewarded Adv](https://max-games.ru/plugin-yg/doc/reward-ad/)
- Страница InterstitialAdv называет модуль и `YG2.InterstitialAdvShow()`. [PluginYG2 — Interstitial Adv](https://max-games.ru/plugin-yg/doc/inter-ad/)
- Гайд платформ объясняет, что реализация модулей зависит от конкретного platform adapter; WebGL/Yandex Games поведение нельзя переносить на Android без проверки. [PluginYG2 — Platforms](https://max-games.ru/plugin-yg/doc/platform/)
- Локальная документация: [сохранения](PluginYG2%20%D0%94%D0%BE%D0%BA%D1%83%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%86%D0%B8%D1%8F/%D0%A1%D0%BE%D1%85%D1%80%D0%B0%D0%BD%D0%B5%D0%BD%D0%B8%D1%8F.md), [пауза и фокус](PluginYG2%20%D0%94%D0%BE%D0%BA%D1%83%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%86%D0%B8%D1%8F/%D0%9F%D0%B0%D1%83%D0%B7%D0%B0%20%D0%B8%20%D1%84%D0%BE%D0%BA%D1%83%D1%81%20%D0%B8%D0%B3%D1%80%D1%8B.md), [данные окружения](PluginYG2%20%D0%94%D0%BE%D0%BA%D1%83%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%86%D0%B8%D1%8F/%D0%94%D0%B0%D0%BD%D0%BD%D1%8B%D0%B5%20%D0%BE%D0%BA%D1%80%D1%83%D0%B6%D0%B5%D0%BD%D0%B8%D1%8F.md).
- Кэшированные версии `RewardedAdv` и `InterstitialAdv` не считаются latest или compatibility proof. Runtime показ рекламы, callbacks, analytics и lifecycle не проверялись этим исследованием.

## Handoff Producer

Минимальный целевой набор для рекламной политики — один feature-модуль `RewardedAdv`; задача M07 подготавливает provider mapping и проверку версии, интеграция относится к M08 после gate. `InterstitialAdv` остаётся отдельным условным решением пользователя/Producer и выключенным по умолчанию. Остальные модули из каталога не добавлять в MVP без новой необходимости и проверки platform coverage. Модулей, SDK и settings в этой задаче не устанавливали и не меняли.
