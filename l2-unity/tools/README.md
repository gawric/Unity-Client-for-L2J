# Unity autologin

Этот файл — для другого AI. Он говорит, как запустить Unity-клиент так же, как официальный `l2.exe` запускает `AutoLoginInterlude.dll`.

Скрипт рядом: `unity_autologin.ps1`.

Официальный вход описан в `AutoLoginInterlude\AutoLoginInterlude\AutoLoginManager.cpp` и `py\runtime\AutoLoginInterlude.ini`. Unity не копирует хуки UE2. Он шлёт те же шаги уже существующими пакетами клиента.

## Что должно быть открыто

1. Редактор Unity на проекте `l2-unity`. Play выключен. Компиляция скриптов закончена.
2. LoginServer `:2106` и GameServer `:7777` того же aCis, что и для официального клиента.
3. На аккаунте есть персонаж в слоте, который передаётся скрипту.

Второй процесс Unity на этот проект не запускать. Кнопку Play извне нажать нельзя: редактор сам включает Play, когда видит файл-сигнал.

## Команда

Из любого каталога:

```powershell
powershell -File "c:\unity\l2 client\l2-git-interlude\Unity-Client-for-L2J\l2-unity\tools\unity_autologin.ps1"
```

Значения по умолчанию совпадают с `AutoLoginInterlude.ini`: аккаунт `2222`, пароль `2222`, сервер `1`, слот `2`. Ждать до 180 секунд.

Другой аккаунт или слот:

```powershell
powershell -File "c:\unity\l2 client\l2-git-interlude\Unity-Client-for-L2J\l2-unity\tools\unity_autologin.ps1" -Account 2222 -Password 2222 -ServerId 1 -Slot 2 -TimeoutSec 180
```

Успех: в консоли строка `[autologin] WORLD ENTER OK` и код выхода `0`. Иначе `exit 1`.

## Что происходит

| Шаг | Кто | Что делает |
|---|---|---|
| 1 | `unity_autologin.ps1` | пишет `l2-unity/Temp/autologin.request` и `autologin.play`, старый `autologin.world` удаляет |
| 2 | `Assets/Editor/UnityAutoLoginPlay.cs` | видит `autologin.play`, удаляет его и ставит `EditorApplication.isPlaying = true` |
| 3 | `Assets/Scripts/Networking/UnityAutoLogin.cs` | после загрузки сцены читает request и идёт по шагам ниже |
| 4 | скрипт | ждёт `autologin.world` |

Пакетный порядок, тот же смысл, что у DLL:

1. `RequestAuthLogin` — логин и пароль. Unity шлёт его сам после `GGAuth`, когда на `LoginClient` уже записаны account и password. Драйвер только вызывает `LoginClient.Connect()`.
2. Лицензия. В официальном клиенте это `FL2GameData::EulaSave`, затем `RequestServerList`. В Unity согласие с лицензией — это сразу `RequestServerList`.
3. `RequestServerLogin` на `serverId` (по умолчанию `1`, как `kAutoServerId` в DLL).
4. `PlayOk` уже сам отключает логин и подключает GameServer. Это делает `PlayOkIncoming`, драйвер его не дублирует.
5. `RequestGameStart` — в Unity это `CharacterSelect` на слот. Слот `2` значит третий персонаж, нумерация с нуля, как `CharacterSlot` в ini.
6. Сцена мира грузится, `GameManager.OnWorldSceneLoaded` шлёт `EnterWorld`. Когда `WorldSpawnReady`, драйвер пишет `Temp/autologin.world` со строкой `OK` и останавливается. Клиент остаётся в мире.

Лог редактора ищет по префиксу `[AutoLogin]`.

## Файлы сигнала

Все три лежат в `l2-unity/Temp/`. Это каталог Unity, в git его не класть.

| Файл | Кто пишет | Смысл |
|---|---|---|
| `autologin.request` | ps1 | `account`, `password`, `serverId`, `slot`, и при чате `chat`, `chatDelay` |
| `autologin.play` | ps1 | «включи Play». Редактор файл удаляет |
| `autologin.world` | драйвер | `OK` или `FAIL timeout` |
| `autologin.chat` | драйвер | `OK`, если в request был `chat` |

Без `autologin.request` обычный Play ничего сам не логинит.

## Чего не делать

- Не жать кнопки логина, лицензии, выбора сервера и персонажа. Драйвер шлёт те же команды, что эти кнопки.
- Не запускать `l2.exe` и `AutoLoginInterlude.dll` вместе с этим Play: оба зайдут одним аккаунтом.
- Не вызывать `Unity.exe -executeMethod` и не открывать второй редактор.
- Не запускать ps1, пока Play уже включён. Сначала остановить Play.
- Не ждать вход, если консоль Unity ещё пишет `Compiling`. После первой установки этих скриптов дождись конца компиляции и запусти ps1 ещё раз.
- Таймаут `world timeout` значит: редактор закрыт, Play уже был включён, сервер не поднят, или в слоте нет персонажа.

## Чат после входа

`-Chat` — текст в общий чат (Say2, тип 0). `-ChatDelaySec` — пауза в секундах после `WorldSpawnReady`, по умолчанию 20. Пустой `-Chat` оставляет старое поведение: скрипт выходит сразу после `WORLD ENTER OK`.

```powershell
powershell -File "c:\unity\l2 client\l2-git-interlude\Unity-Client-for-L2J\l2-unity\tools\unity_autologin.ps1" -Chat s -ChatDelaySec 20 -TimeoutSec 180
```

Успех чата: `[autologin] CHAT OK` и `Temp/autologin.chat` со строкой `OK`. В `Editor.log` ищи `[AutoLogin] sent chat`.

Сервер на текст `s` или `.s` запускает Replay и не рассылает эту строку как обычное сообщение, если сессия стартовала. Повтор раньше чем через 1.5 с игнорируется.

## Replay

Сервер: `ChainReplay`. Старт — чат `s`. Берётся самый новый `session.json` по времени файла. Корни поиска, по порядку:

1. `C:\Users\hh-soft\source\repos\AutoLoginInterlude\ChainLogs\Sessions\<имя>\session.json`
2. `./data/chains/Sessions` от рабочей папки GameServer
3. `./chains/Sessions`

`localActorId` — это вошедший персонаж (UserInfo). Его класс из JSON не переписывается. Остальные `PLAYER` — временные CharInfo (`createTransient`, test dummy). `NPC` спавнится как моб. События идут по `tMs`, потом по `seq`. Неизвестный тип события молча пропускается.

Часы `tMs` стартуют после входа в мир: 1200 мс, если персонаж ещё телепортируется, иначе 200 мс. `durationMs` — сколько сцена живёт от нуля этих часов. Пока время не вышло, стенд-ины не удаляются. Раньше мир закрывался на последнем событии, и труп исчезал до пакета Revive. Теперь `Replay complete` и `decayMe` приходят в конце `durationMs`.

Скилл с `SkillType.RESURRECT` (2014 Scroll of Resurrection, 1016 Resurrection) бьёт по трупу. Revive уходит через `hitTime` миллисекунд после `MAGIC_SKILL_USE`.

Текущая самая новая сессия, пока её файл новее остальных: `ChainLogs\Sessions\charinfo_resurrection`. Локальный игрок кастует 2014 в мёртвого CharInfo Fallen. `durationMs` 16000: падение на 400 мс, каст на 2200 мс, `hitTime` 2700, дальше Fallen стоит до конца длительности.

## Как проверить

1. Скрипт: `WORLD ENTER OK`, при `-Chat` ещё `CHAT OK`, код выхода 0.
2. `l2-unity/Temp/autologin.world` = `OK`. Если был чат, `autologin.chat` = `OK`.
3. `%LOCALAPPDATA%\Unity\Editor\Editor.log`: `[AutoLogin] WORLD ENTER OK` и `[AutoLogin] sent chat`.
4. Консоль GameServer `aCis_gameserver\build\dist\gameserver\log\console\console_0.txt`:
   - `starts replay ... with N actors and M events` — сессия реально прочитана
   - `Replay #... DIE` / `MAGIC_SKILL_USE` — пакеты ушли
   - `Replay holding ... ms after the last event` — стенд-ин ещё в мире
   - `Replay resurrected Fallen` — труп поднят, это раньше `Replay complete`
   - `Replay complete` — после `durationMs`, тогда CharInfo удаляется
5. В картинке: CharInfo падает, локальный игрок кастует, Fallen встаёт и стоит до `Replay complete`. Если `Replay complete` в той же секунде, что `MAGIC_SKILL_USE`, сервер ещё на старом классе и стенд-ин сотрётся до Revive.
