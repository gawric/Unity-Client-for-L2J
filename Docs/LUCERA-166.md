# Lucera Classic 166 — второй сетевой профиль

## Правила

1. **ACIS — профиль по умолчанию.** DTO, опкоды, `GameServerPacketType`, `GameClientPacketType` и поток Interlude не меняются ради Lucera.
2. Lucera живёт **рядом**: `ServerProfile` / `IProtocolFactory` → `Lucera166Protocol`. Никаких `if (lucera)` в менеджерах.
3. Источник истины для Lucera — код сервера (`writeImpl` / `readImpl`). Поля не придумываются и не копируются из Interlude.
4. В DTO добавляются только фабрики / конструкторы; `ReadFrom` не меняется.
5. Профиль выбирается явно (`-serverProfile lucera`); автоопределения нет — authserver не сообщает хронику.

## Файлы

| Файл | Назначение |
|---|---|
| `Networking/Protocol/ServerProfile.cs`, `ChronicleProtocol.cs` | Профиль сервера и хроника |
| `Networking/Protocol/IProtocolFactory.cs`, `ProtocolFactory.cs` | Создание `IProtocol` по профилю |
| `Networking/Protocol/IProtocol.cs`, `Interlude/InterludeProtocol.cs` | Добавлено `ProtocolVersion` (Interlude = 746) |
| `Networking/Protocol/Lucera/Lucera166Protocol.cs` | Разбор s2c Lucera → существующие DTO |
| `Networking/Protocol/Lucera/LuceraWire.cs` | Кодирование c2s Lucera |
| `Di/AppLifetimeScope.cs`, `Networking/GameClient.cs` | DI и версия протокола |
| DTO в `ClientLibrary/Packet/Dto/Incoming/**` | Только фабрики `From(...)` |
| `Networking/UnityAutoLogin.cs` | Автотест (только при наличии `Temp/autologin.request`) |

## Отличия формата (примеры)

| Пакет | Interlude | Lucera 166 |
|---|---:|---:|
| `ProtocolVersion` | `0x00` + 746 | `0x0E` + 166 |
| `AuthLogin` | `0x08` | `0x2B` |
| `CharSelectionInfo` | `0x13` | `0x09` |
| `CharSelected` | `0x15` | `0x0B` |
| `CharInfo` | `0x03` | `0x31` |
| `UserInfo` | `0x04` | `0x32` (блоки по маске) |
| `NpcInfo` | `0x16` | `0x0C` (блоки по маске) |
| `ItemList` | `0x1B` | `0x11` (два пакета: заголовок, затем предметы) |
| `MoveToLocation` (c2s) | `0x01` | `0x0F` (+ `D` «движение мышью») |
| `ValidatePosition` (c2s) | `0x48` | `0x59` (+ `D` транспорт) |

ID предметов / классов совпадают; **формат пакетов — нет**.

## Проверка

- 663 реальных пакета с сервера Lucera читаются полностью (каждый байт).
- Все энкодеры c2s совпадают с `readImpl` сервера.
- Живой тест: логин → персонажи → мир → чат, без исключений и без ошибок на сервере.
- В рантайме Lucera пишет в лог `[Lucera] paquete 0xNN ... sobran N` если пакет прочитан не полностью.

## Не готово

`TradeOwnAdd` / `TradeOtherAdd` / `TradeUpdate`, `FriendList`, `HennaInfo` (пустые обработчики), `PledgeShowInfoUpdate` (нет обработчика), `ShopPreviewInfo` (пустой DTO), часть `Ex`-пакетов Classic без UI.
