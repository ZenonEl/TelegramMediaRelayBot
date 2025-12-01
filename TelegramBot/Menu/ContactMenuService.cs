// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.TelegramBot.Models;
using TelegramMediaRelayBot.TelegramBot.States;
using TelegramMediaRelayBot.TelegramBot.Utils;

namespace TelegramMediaRelayBot.TelegramBot.Services;

public interface IContactMenuService
{
    Task StartAddContactFlow(ITelegramBotClient botClient, Update update);
    Task StartDeleteContactFlow(ITelegramBotClient botClient, Update update);
    Task StartMuteContactFlow(ITelegramBotClient botClient, Update update);
    Task StartUnmuteContactFlow(ITelegramBotClient botClient, Update update);
    Task ViewContacts(ITelegramBotClient botClient, Update update);
    Task StartEditContactGroupFlow(ITelegramBotClient botClient, Update update);
    Task<List<ContactViewModel>> GetContactsForDisplay(int userId);
    Task<Message?> ShowAvailableContacts(ITelegramBotClient botClient, Update update);
}

public class ContactMenuService : IContactMenuService
{
    private readonly IUiResourceService _uiResources;
    private readonly IStatesResourceService _statesResources;
    private readonly IErrorsResourceService _errorsResources;
    private readonly IUserStateManager _stateManager;
    private readonly IUserGetter _userGetter;
    private readonly IContactGetter _contactGetter;
    private readonly IGroupGetter _groupGetter;
    private readonly IResourceService _resourceService;
    private readonly ITelegramInteractionService _interactionService;

    public ContactMenuService(
        IUiResourceService uiResources,
        IStatesResourceService statesResources,
        IErrorsResourceService errorsResources,
        IUserStateManager stateManager,
        IUserGetter userGetter,
        IContactGetter contactGetter,
        IGroupGetter groupGetter,
        IResourceService resourceService,
        ITelegramInteractionService interactionService)
    {
        _uiResources = uiResources;
        _statesResources = statesResources;
        _errorsResources = errorsResources;
        _stateManager = stateManager;
        _userGetter = userGetter;
        _contactGetter = contactGetter;
        _groupGetter = groupGetter;
        _resourceService = resourceService;
        _interactionService = interactionService;
    }

    public async Task StartAddContactFlow(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        UserStateData newState = new UserStateData { StateName = "AddContact", Step = 0 };
        _stateManager.Set(chatId, newState);
        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, _statesResources.GetString("State.AddContact.Prompt.EnterLink"));
    }

    public async Task StartDeleteContactFlow(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        UserStateData newState = new UserStateData { StateName = "RemoveContacts", Step = 0 };
        _stateManager.Set(chatId, newState);

        List<long> tgIds = (await _contactGetter.GetAllContactUserTGIds(userId)).ToList();
        List<string> infoList = new List<string>();

        foreach (long tg in tgIds)
        {
            int id = _userGetter.GetUserIDbyTelegramID(tg);
            string uname = _userGetter.GetUserNameByTelegramID(tg);
            string membership = await BuildMembershipInfo(userId, id);

            string info = string.Format(_uiResources.GetString("UI.Format.ContactInfo"), id, uname, "") +
                            (string.IsNullOrEmpty(membership) ? "" : $"\n{membership}");
            infoList.Add(info);
        }

        string prompt = $"{_uiResources.GetString("UI.Header.YourContacts")}\n{string.Join("\n", infoList)}\n\n{_statesResources.GetString("State.RemoveContact.Prompt.EnterIds")}";
        await botClient.SendMessage(chatId, prompt, cancellationToken: CancellationToken.None);
    }

    public async Task StartMuteContactFlow(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);
        List<ContactViewModel> contacts = await GetContactsForDisplay(userId);

        if (!contacts.Any())
        {
            // TODO Move: "Contacts.Empty"
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, "У вас нет контактов.");
            return;
        }

        List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>();
        foreach (ContactViewModel c in contacts)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔇 {c.Name}", $"mute_contact_select:{c.Id}")
            });
        }

        buttons.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });

        // TODO Move: "Mute.Menu.Prompt"
        await _interactionService.ReplyToUpdate(botClient, update, new InlineKeyboardMarkup(buttons), CancellationToken.None, "👇 <b>Выберите, кого заглушить:</b>");
    }

    public async Task StartUnmuteContactFlow(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        IEnumerable<int> mutedIds = await _contactGetter.GetMutedContactIds(userId);

        if (!mutedIds.Any())
        {
            // TODO Move: "Unmute.Empty"
            await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), CancellationToken.None, "✅ У вас нет заглушенных контактов.");
            return;
        }

        List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>();
        foreach (int id in mutedIds)
        {
            string name = _userGetter.GetUserNameByID(id) ?? "Unknown";
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔊 {name}", $"unmute_contact_select:{id}")
            });
        }

        buttons.Add(new[] { KeyboardUtils.GetReturnButton("main_menu") });

        // TODO Move: "Unmute.Menu.Prompt"
        await _interactionService.ReplyToUpdate(botClient, update, new InlineKeyboardMarkup(buttons), CancellationToken.None, "👇 <b>Выберите, кого размутить:</b>");
    }

    public async Task ViewContacts(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        List<long> contactUserTGIds = await _contactGetter.GetAllContactUserTGIds(userId);
        List<string> contactUsersInfo = contactUserTGIds.Select(tgId =>
        {
            long id = _userGetter.GetUserIDbyTelegramID(tgId);
            string username = _userGetter.GetUserNameByTelegramID(tgId);
            string link = _userGetter.GetUserSelfLink(tgId);
            return string.Format(_uiResources.GetString("UI.Format.ContactInfo"), id, username, link);
        }).ToList();

        await _interactionService.ReplyToUpdate(botClient, update, KeyboardUtils.GetViewContactsKeyboardMarkup(), CancellationToken.None, $"{_uiResources.GetString("UI.Header.YourContacts")}\n{string.Join("\n", contactUsersInfo)}");
    }

    public async Task StartEditContactGroupFlow(ITelegramBotClient botClient, Update update)
    {
        long chatId = _interactionService.GetChatId(update);
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        // 1. УБИРАЕМ установку состояния.
        // Состояние установится само, когда юзер выберет группу.
        // _stateManager.Set(chatId, newState); <--- УДАЛИТЬ

        // 2. Получаем список групп (объекты, а не строки)
        // Убедись, что в IGroupGetter есть метод возвращающий список объектов (Id, Name)
        // Если нет, добавь. Примерно: IEnumerable<GroupModel> GetGroupsByUserId(int userId)
        IEnumerable<int> groups = await _groupGetter.GetGroupIDsByUserId(userId);

        if (!groups.Any())
        {
            await _interactionService.ReplyToUpdate(
                botClient,
                update,
                KeyboardUtils.GetReturnButtonMarkup(),
                CancellationToken.None,
                _uiResources.GetString("UI.Info.NoGroups") // "У вас нет групп"
            );
            return;
        }

        // 3. Создаем клавиатуру из групп
        List<InlineKeyboardButton[]> buttons = new List<InlineKeyboardButton[]>();
        foreach (int group in groups)
        {
            // Генерируем кнопку для каждой группы
            // callbackData должен совпадать с EditGroupSelectedCommand.Name
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: await _groupGetter.GetGroupNameById(group),
                    callbackData: $"edit_group_selected:{group}"
                )
            });
        }
        // Кнопка "Назад"
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(_uiResources.GetString("UI.Button.BackToMenu"), "show_groups") });

        // 4. Отправляем меню
        // TODO: Вынести текст в ресурсы "Group.SelectForEdit"
        await _interactionService.ReplyToUpdate(
            botClient,
            update,
            new InlineKeyboardMarkup(buttons),
            CancellationToken.None,
            "📂 <b>Выберите группу для редактирования:</b>"
        );
    }

    public async Task<List<ContactViewModel>> GetContactsForDisplay(int userId)
    {
        List<ContactViewModel> contactViewModels = new List<ContactViewModel>();
        List<long> tgIds = await _contactGetter.GetAllContactUserTGIds(userId);

        foreach (long tgId in tgIds)
        {
            int contactId = _userGetter.GetUserIDbyTelegramID(tgId);
            string membershipInfo = await BuildMembershipInfo(userId, contactId); // Используем наш приватный метод

            contactViewModels.Add(new ContactViewModel
            {
                Id = contactId,
                Name = _userGetter.GetUserNameByTelegramID(tgId),
                Link = _userGetter.GetUserSelfLink(tgId),
                MembershipInfo = membershipInfo
            });
        }
        return contactViewModels;
    }

    public async Task<Message?> ShowAvailableContacts(ITelegramBotClient botClient, Update update)
    {
        long chatId = update.CallbackQuery!.Message!.Chat.Id;
        int userId = _userGetter.GetUserIDbyTelegramID(chatId);

        List<ContactViewModel> contacts = await GetContactsForDisplay(userId);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(_uiResources.GetString("UI.Header.YourContacts"));

        if (contacts.Any())
        {
            foreach (ContactViewModel contact in contacts)
            {
                sb.AppendLine(string.Format(_uiResources.GetString("UI.Format.ContactInfo"), contact.Id, contact.Name, contact.Link));
                sb.AppendLine(contact.MembershipInfo);
            }
        }
        else
        {
            sb.AppendLine(_errorsResources.GetString("Error.Input.NoUsersFound"));
        }
        sb.AppendLine($"\n{_statesResources.GetString("State.RemoveContact.Prompt.EnterIds.Generic")}");

        return await _interactionService.ReplyToUpdate(
            botClient,
            update,
            KeyboardUtils.GetCancelKeyboardMarkup(update.CallbackQuery.Message.Id),
            cancellationToken: CancellationToken.None,
            text: sb.ToString()
        );
    }

    private async Task<string> BuildMembershipInfo(int ownerUserId, int contactUserId)
    {
        IEnumerable<int> groupIds = await _groupGetter.GetGroupIDsByUserId(ownerUserId);
        List<string> membership = new List<string>();
        foreach (int gid in groupIds)
        {
            IEnumerable<int> members = await _groupGetter.GetAllUsersIdsInGroup(gid);
            if (members.Contains(contactUserId))
            {
                string name = await _groupGetter.GetGroupNameById(gid);
                membership.Add($"{name} (ID: {gid})");
            }
        }
        if (membership.Count == 0) return string.Empty;
        return $"<i>{_uiResources.GetString("UI.Label.Groups")}</i> {string.Join(", ", membership)}";
    }
}
