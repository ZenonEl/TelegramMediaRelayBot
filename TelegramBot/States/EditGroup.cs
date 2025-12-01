// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Services;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.States;

public class EditContactGroupStateHandler : IStateHandler
{
    private readonly IContactGroupRepository _contactGroupRepository;
    private readonly IUserGetter _userGetter;
    private readonly IGroupSetter _groupSetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IContactGetter _contactGetter;
    private readonly Config.Services.IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;
    private readonly IStateBreakService _stateBreaker;

    public string Name => "EditContactGroup";

    public EditContactGroupStateHandler(
        IContactGroupRepository contactGroupRepository,
        IGroupSetter groupSetter,
        IUserGetter userGetter,
        IGroupGetter groupGetter,
        IContactGetter contactGetter,
        Config.Services.IResourceService resourceService,
        ITelegramInteractionService interactionService,
        IStateBreakService stateBreaker)
    {
        _contactGroupRepository = contactGroupRepository;
        _groupSetter = groupSetter;
        _userGetter = userGetter;
        _groupGetter = groupGetter;
        _contactGetter = contactGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
        _stateBreaker = stateBreaker;
    }

    public async Task<StateResult> Process(UserStateData stateData, Update update, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        long chatId = _interactionService.GetChatId(update);
        if (await _stateBreaker.HandleStateBreak(botClient, update)) return StateResult.Complete();

        if (!stateData.Data.TryGetValue("GroupId", out object? grpObj) || grpObj is not int groupId)
        {
            return StateResult.Complete();
        }

        switch (stateData.Step)
        {
            // ========================================================================
            // ШАГ 0: Выбор действия в меню
            // ========================================================================
            case 0:
                if (update.CallbackQuery?.Data == null) return StateResult.Ignore();
                string data = update.CallbackQuery.Data;

                // --- ЛОГИКА ПЕРЕКЛЮЧЕНИЯ DEFAULT (Сразу выполняем и обновляем меню) ---
                if (data == "group_action:toggle_default")
                {
                    // Получаем текущее состояние
                    string group = await _groupGetter.GetGroupNameById(groupId);
                    if (group != string.Empty)
                    {
                        bool newState = !await _groupGetter.GetIsDefaultGroup(groupId);
                        // TODO: Реализовать метод UpdateGroupDefault в репозитории
                        await _groupSetter.SetIsDefaultGroup(groupId);
                        string status = newState ? "включена" : "выключена";
                        await botClient.AnswerCallbackQuery(update.CallbackQuery.Id, $"Рассылка по умолчанию {status}", cancellationToken: cancellationToken);
                    }
                    return StateResult.Ignore();
                }

                // --- ОПРЕДЕЛЕНИЕ ДЕЙСТВИЯ ---
                string? action = null;
                if (data == "group_action:add") action = "Add";
                else if (data == "group_action:remove") action = "Remove";
                else if (data == "group_action:rename") action = "Rename";
                else if (data == "group_action:desc") action = "Description";

                if (action == null) return StateResult.Ignore();

                stateData.Data["Action"] = action;
                int userId = _userGetter.GetUserIDbyTelegramID(chatId);
                StringBuilder sb = new StringBuilder();
                InlineKeyboardMarkup backKb = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Назад", $"edit_group_selected:{groupId}"));

                if (action == "Description")
                {
                    // TODO: Move "Group.Description.Prompt"
                    string prompt = "📝 <b>Введите новое описание группы:</b>\n" +
                                    "<i>Отправьте точку (.), чтобы удалить описание.</i>";

                    await _interactionService.ReplyToUpdate(botClient, update, backKb, cancellationToken, prompt);
                    stateData.Step = 1;
                    return StateResult.Continue();
                }

                // --- СЦЕНАРИЙ: ПЕРЕИМЕНОВАНИЕ ---
                if (action == "Rename")
                {
                    // TODO: Move "Group.Rename.Prompt"
                    await _interactionService.ReplyToUpdate(botClient, update, backKb, cancellationToken, "✏️ <b>Введите новое название группы:</b>");
                    stateData.Step = 1;
                    return StateResult.Continue();
                }

                // --- СЦЕНАРИЙ: ДОБАВЛЕНИЕ (Улучшенный) ---
                if (action == "Add")
                {
                    // 1. Получаем тех, кто УЖЕ в группе
                    List<int> existingMembers = (List<int>)await _groupGetter.GetAllUsersIdsInGroup(groupId);

                    // 2. Получаем ВСЕ контакты
                    List<long> allContactTgIds = (List<long>)await _contactGetter.GetAllContactUserTGIds(userId);

                    sb.AppendLine("➕ <b>Добавление участников</b>\n");

                    // Показываем текущий состав (чтобы не дублировать)
                    if (existingMembers.Any())
                    {
                        sb.AppendLine("📋 <b>Уже в группе:</b>");
                        foreach (int cid in existingMembers)
                        {
                            string uname = _userGetter.GetUserNameByID(cid) ?? "ID " + cid;
                            sb.AppendLine($"• {uname}");
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("👇 <b>Кого можно добавить (ID):</b>");

                    bool foundCandidates = false;
                    foreach (long tgId in allContactTgIds)
                    {
                        int cid = _userGetter.GetUserIDbyTelegramID(tgId);

                        // ФИЛЬТР: Показываем только тех, кого НЕТ в группе
                        if (!existingMembers.Contains(cid))
                        {
                            string uname = _userGetter.GetUserNameByTelegramID(tgId);
                            sb.AppendLine($"<code>{cid}</code> — {uname}");
                            foundCandidates = true;
                        }
                    }

                    if (!foundCandidates)
                    {
                        sb.AppendLine("<i>(Все ваши контакты уже в этой группе)</i>");
                    }
                    else
                    {
                        sb.AppendLine("\n✍️ <b>Введите ID через пробел:</b>");
                    }
                }
                // --- СЦЕНАРИЙ: УДАЛЕНИЕ ---
                else if (action == "Remove")
                {
                    sb.AppendLine("➖ <b>Удаление участников</b>\n");
                    sb.AppendLine("👇 <b>Текущие участники (нажмите ID, чтобы скопировать):</b>");

                    List<int> members = (List<int>)await _groupGetter.GetAllUsersIdsInGroup(groupId);
                    if (members.Any())
                    {
                        foreach (int cid in members)
                        {
                            string uname = _userGetter.GetUserNameByID(cid) ?? "Unknown";
                            sb.AppendLine($"<code>{cid}</code> — {uname}");
                        }
                        sb.AppendLine("\n✍️ <b>Введите ID для удаления:</b>");
                    }
                    else
                    {
                        sb.AppendLine("<i>(Группа пуста)</i>");
                    }
                }

                await _interactionService.ReplyToUpdate(botClient, update, backKb, cancellationToken, sb.ToString());
                stateData.Step = 1;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 1: Обработка ввода (Текст с названием или ID)
            // ========================================================================
            case 1:
                // Обработка кнопки "Назад"
                if (update.CallbackQuery?.Data?.StartsWith("edit_group_selected") == true) return StateResult.Complete();
                if (string.IsNullOrWhiteSpace(update.Message?.Text)) return StateResult.Ignore();

                string userText = update.Message.Text.Trim();
                string currentAction = (string)stateData.Data["Action"];

                // --- НОВАЯ ЛОГИКА ДЛЯ СОХРАНЕНИЯ ОПИСАНИЯ ---
                if (currentAction == "Description")
                {
                    string newDescription = userText == "." ? string.Empty : userText;
                    await _groupSetter.SetGroupDescription(groupId, newDescription);
                    // TODO: Move "Group.Description.Success"
                    string msg = string.IsNullOrEmpty(newDescription)
                        ? "🗑 Описание удалено."
                        : "✅ Описание обновлено.";

                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, msg);
                    return StateResult.Complete();
                }

                // --- ЛОГИКА ПЕРЕИМЕНОВАНИЯ ---
                if (currentAction == "Rename")
                {
                    // TODO: Реализовать RenameGroupAsync в репозитории
                    await _groupSetter.SetGroupName(groupId, userText);

                    // TODO: Move "Group.Rename.Success"
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, $"✅ Группа переименована в: <b>{userText}</b>");
                    return StateResult.Complete();
                }

                // --- ЛОГИКА ДОБАВЛЕНИЯ/УДАЛЕНИЯ (Парсинг ID) ---
                List<int> contactIDs;
                try
                {
                    contactIDs = userText
                        .Split(new[] { ' ', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.Parse(x.Trim()))
                        .Distinct()
                        .ToList();
                }
                catch
                {
                    await botClient.SendMessage(chatId, "⚠️ Ошибка формата. Введите цифры.", cancellationToken: cancellationToken);
                    return StateResult.Continue();
                }

                // Фильтрация дублей ПРИ ДОБАВЛЕНИИ
                if (currentAction == "Add")
                {
                    List<int> alreadyInGroup = (List<int>)await _groupGetter.GetAllUsersIdsInGroup(groupId);
                    // Оставляем только тех, кого НЕТ в группе
                    contactIDs = contactIDs.Where(id => !alreadyInGroup.Contains(id)).ToList();

                    if (!contactIDs.Any())
                    {
                        await botClient.SendMessage(chatId, "⚠️ Все введенные пользователи уже состоят в этой группе.", cancellationToken: cancellationToken);
                        return StateResult.Continue();
                    }
                }

                stateData.Data["ContactIDs"] = contactIDs;

                // Подтверждение
                StringBuilder confirmSb = new StringBuilder();
                confirmSb.AppendLine(currentAction == "Add" ? "❓ <b>Добавить пользователей?</b>" : "❓ <b>Удалить пользователей?</b>");
                foreach (int cid in contactIDs)
                {
                    string uName = _userGetter.GetUserNameByID(cid) ?? "ID " + cid;
                    confirmSb.AppendLine($"• {uName} (ID: {cid})");
                }

                await botClient.SendMessage(chatId, confirmSb.ToString(),
                    replyMarkup: KeyboardUtils.GetConfirmForActionKeyboardMarkup("accept_action"),
                    cancellationToken: cancellationToken,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                stateData.Step = 2;
                return StateResult.Continue();

            // ========================================================================
            // ШАГ 2: Выполнение (Add/Remove)
            // ========================================================================
            case 2:
                if (update.CallbackQuery?.Data == "decline")
                {
                    await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, "❌ Отмена.");
                    return StateResult.Complete();
                }
                if (update.CallbackQuery?.Data != "accept_action") return StateResult.Ignore();

                List<int> ids = (List<int>)stateData.Data["ContactIDs"];
                string act = (string)stateData.Data["Action"];
                int myUserId = _userGetter.GetUserIDbyTelegramID(chatId);

                int successCount = 0;
                foreach (int cid in ids)
                {
                    bool ok = false;
                    if (act == "Add") ok = _contactGroupRepository.AddContactToGroup(myUserId, cid, groupId);
                    else ok = _contactGroupRepository.RemoveContactFromGroup(myUserId, cid, groupId);
                    if (ok) successCount++;
                }

                string icon = act == "Add" ? "✅" : "🗑";
                await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.SendInlineKeyboardMenu(), cancellationToken, $"{icon} Успешно: {successCount} из {ids.Count}");
                return StateResult.Complete();
        }

        return StateResult.Ignore();
    }
}
