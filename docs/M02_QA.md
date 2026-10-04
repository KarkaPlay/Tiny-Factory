# M02 / v0.2.0 — QA handoff

**Вердикт: PASS по техническому scope M02 с ограничениями D12.** Повторная QA-приёмка выполнена read-only по свежим Unity MCP evidence Engineer, актуальным package/settings/build notes и APK. Это не подтверждение gameplay, ad runtime, Android install/device readiness или финального DoD.

| Критерий | Статус | Evidence и границы |
|---|---|---|
| Правильный проект, Editor и стартовая сцена | PASS | Unity `6000.3.9f1`, `Tiny Factory@4b2dafe5`; post-build MCP: ready, compile=false, Play Mode off, Console errors 0. `SampleScene` сохранена; validation: 0 issues, 0 missing scripts, 0 broken prefabs. Editor Play Mode smoke прошёл и остановлен с назначенной Yandex Mobile Ads platform. |
| UI/save baseline M02 | PASS как принятые решения | uGUI 2.0.0, Input System 1.18.0, TMP подтверждён ранее. Выбран local versioned JSON/`JsonUtility` DTO baseline D11. HUD и runtime saves не входят в M02; save/recovery проверяются в M05. |
| Импортированная Android module composition | PASS по compile/build inclusion | `InterstitialAdv v1.02`, `RewardedAdv v1.011`, `Metrica v1.02`; Android defines включают `YandexMobileAdsPlatform_yg`, `InterstitialAdv_yg`, `RewardedAdv_yg`, `Metrica_yg`. Metrica выключена и Android gameplay analytics не поддерживается этой реализацией. |
| Защитные настройки smoke | PASS по MCP readback Engineer | YMA platform asset назначен; test mode=true, auto-load/app-open=false; app-open/interstitial/reward IDs пусты; first interstitial=false; Editor interstitial simulation=false; Metrica flags=false, counter=0. Ads не запрашивались и не показывались. |
| Android APK smoke через Unity MCP | PASS | Job `build-8a582018b4`, success, 75.061 s; app `0.2.0`, code `4`, ID `com.tinyfactory.prototype`; 0 errors / 1 warning. Артефакт [`TinyFactory-v0.2.0-code4.apk`](../Builds/Android/TinyFactory-v0.2.0-code4.apk), 38,389,549 bytes, SHA-256 `407a945388456074ec0b75b293ae9a5e905b9fd959c33585e18011b2662534aa`. Размер и SHA-256 независимо сверены QA по локальному артефакту. |
| Установка, запуск и финальный Android DoD | PENDING MANUAL по D12 | Пользователь отложил физическое устройство. Code 4 APK не устанавливался/не запускался; safe area/touch, background/resume, update, saves и performance не проверялись. |

Единственное build warning code 4: отсутствует `RuntimePipelineConfig`, поэтому Pipeline отключён в Player. Сцена M02 не содержит gameplay-визуала; влияние на будущий рендер оценить в M03/M06. AppMetrica AdRevenue adapter `1.1.0` закреплён отдельным upstream commit, тогда как manifest SDK указывает `1.0.0`; code 4 подтверждает compile/link, runtime совместимость ещё не установлена.

Показ рекламы, реальные callbacks, reward identity/exactly-once, отказ/no-fill/offline/lifecycle и analytics delivery не тестировались и остаются M07–M09. YMA Android runtime, device readiness, gameplay и финальный DoD не заявляются. Следующий шаг по продукту — отдельная production-задача M03; ручной Android smoke остаётся за пользователем по D12.
