# Инструменты для сборки GammaTool

Временный список. Удалить после окончания разработки.

Источник требований: `GammaTool-build-guide.md`, раздел 2.1. Сборка на Linux-хосте. Готовый exe передаётся на Windows по SSH (`scp`), без локального HTTP-сервера.

Проверено 2026-10-30.

## Готово к написанию кода

| Инструмент | Зачем | Состояние |
|------------|--------|-----------|
| Редактор (Cursor) | Правка C# и XAML | Есть |
| bash 5.3.20 | Оболочка | `/usr/bin/bash` |
| curl 8.22.0 | Скачивание SDK | `/usr/bin/curl` |
| git 2.56.0 | Репозиторий | `/usr/bin/git` |
| OpenSSH 10.5 | Передача `GammaTool.exe` на Windows (`ssh` / `scp`) | `/usr/bin/ssh`, `/usr/bin/scp` |
| .NET SDK 10.0.403 | Сборка `net10.0-windows` | `~/.dotnet`, в `PATH` через `~/.bashrc` |

Рядом остаётся .NET SDK 8.0.426. Сборка идёт десятой версией: `dotnet` в `PATH` указывает на `~/.dotnet`, хост 10.0.14.

В `~/.bashrc` добавлено:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

Пакеты таргетинга Windows Desktop (`EnableWindowsTargeting`) скачает NuGet при первом `dotnet build`. Для этого нужен интернет в момент сборки.

## Не используем

- python3 для раздачи exe. Передача файла — по SSH.
- Установку SDK на Windows. ВМ и реальный ПК получают один self-contained `GammaTool.exe`.
