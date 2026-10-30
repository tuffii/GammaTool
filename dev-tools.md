# Инструменты для сборки GammaTool

Временный список. Удалить после окончания разработки.

Источник требований: `GammaTool-build-guide.md`, раздел 2.1. Сборка идёт на Linux-хосте. На Windows SDK не нужен: публикуется один self-contained `GammaTool.exe`.

Проверено 2026-10-30. Ничего из недостающего ещё не скачивалось.

## Уже есть

| Инструмент | Зачем | Состояние |
|------------|--------|-----------|
| Редактор (Cursor) | Правка C# и XAML | Есть |
| bash 5.3.20 | Запуск `dotnet-install.sh` | `/usr/bin/bash` |
| curl 8.22.0 | Скачивание скрипта установки SDK | `/usr/bin/curl` |
| git 2.56.0 | Репозиторий | `/usr/bin/git` |
| python3 3.13.15 | Временная раздача exe в ВМ (`python3 -m http.server`), раздел 6.3 | `/usr/bin/python3` |
| .NET SDK 8.0.426 | Не целевая версия плана | Лежит в `~/.dotnet`, в `PATH` не добавлен, в `~/.bashrc` нет `DOTNET_ROOT` |

## Нужно поставить

Один пакет.

**.NET SDK 10.0**

План собирает проект под `net10.0-windows`. Атрибут `ThemeMode` в окне настроек есть только с .NET 9 и новее, поэтому SDK 8 из `~/.dotnet` для этого плана не подходит.

Установка в домашнюю папку, без root (как в разделе 2.1):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
bash dotnet-install.sh --channel 10.0
```

После установки добавить в `~/.bashrc`, чтобы `dotnet` был в каждой новой оболочке:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

Проверка: `dotnet --version` должна показать 10.x.

Пакеты таргетинга Windows Desktop (`EnableWindowsTargeting`) план не ставит отдельно: их скачает NuGet при первом `dotnet build`. Для этого нужен интернет в момент сборки.

## Не ставим

- На Windows (ВМ и реальный ПК) SDK, Visual Studio и рантайм не нужны.
- Редактор менять не нужно.
- SDK 8.0.426 не удаляем и не обновляем отдельно: канал 10.0 ставится рядом в тот же `~/.dotnet`.
