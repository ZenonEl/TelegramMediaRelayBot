using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using System.Text.RegularExpressions;
using MediaTelegramBot.Menu;
using TikTokMediaRelayBot.SitesConfig;

namespace MediaTelegramBot;


public class PrivateUpdateHandler
{
    private static string ExtractDomain(string link)
    {
        var uri = new Uri(link);
        return uri.Host;
    }

    // Метод для проверки, соответствует ли ссылка паттернам
    private static bool IsLinkMatchPattern(string link, List<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(link, pattern))
            {
                return true;
            }
        }
        return false;
    }
    public static async Task ProcessMessage(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        // Проверка на команду /start
        // if (update.Message.Text == "/start")
        // {
        //     await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        //     return;
        // }

        // Проверка на ссылку
        string messageText = update.Message.Text;
        string link = "";
        string text = "";

        int newLineIndex = messageText.IndexOf('\n');

        if (newLineIndex != -1)
        {
            link = messageText.Substring(0, newLineIndex).Trim();
            text = messageText.Substring(newLineIndex + 1).Trim();
        }
        else
        {
            link = messageText.Trim();
        }

        // Извлечение домена из ссылки
        string domain = ExtractDomain(link);

        // Поиск домена в списке доменов
        if (SitesConfig.Domains.Contains(domain))
        {
            // Получение Getter по домену
            // Retrieve Getter by domain
            var getter = SitesConfig.GetGetterByDomain(domain);

            if (getter != null)
            {
                var patterns = getter.Patterns;
                var elementsPath = getter.elements_path;

                // Check if the link matches any pattern
                if (IsLinkMatchPattern(link, patterns))
                {
                    await botClient.SendMessage(chatId, "Подождите, идет скачивание видео...", cancellationToken: cancellationToken);

                    // Call the video handling function with the correct types
                    _ = TelegramBot.HandleVideoRequest(botClient, link, chatId, text, elementsPath);
                }
                else
                {
                    await botClient.SendMessage(chatId, "Ссылка не соответствует паттернам.", cancellationToken: cancellationToken);
                }
            }
            else
            {
                await botClient.SendMessage(chatId, "Домен найден, но Getter не определен.", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendMessage(chatId, "Домен не найден в конфигурации.", cancellationToken: cancellationToken);
        }
        if (update.Message.Text == "/start")
        {
            await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
        }
        else if (update.Message.Text == "/help")
        {
            string helpText = @"<b>Работа с ссылками для ТТ</>: 
            Просто отправь мне ссылку. Вот пример того как может выглядеть твоё сообщение: 
ссылка_на_тт_видео

Какой то твой крутой мега текст который будет под видео прислан всем твоим контактактикам, если ты их кнш заблаговременно добавил :D
Кстати советую использовать хештеги для навигации по видосам. Удобно будет искать как тому кто отправил так и тому кто получил :)

            <b>Добавление контактов:</>
1) Нужен тот кто будет согласен связаться с вами и возможно подвергнутся спам атакам из видео.
2) Вы даете ему свою (или берете его) ссылку вида: 1234ab56-cde-78fg-01hi-2j34k56790 (Ссылку можно найти в главном меню в кнопке Моя ссылка).
3) Потом нажимаете кнопочку Добавить контакт в главном меню. После нажатия вас попросят указать ссылку другого человека которого вы хотите добавить, вида: 1234ab56-cde-78fg-01hi-2j34k56790
Просто ссылка, больше писать ничего не нужно. Если ссылка будет не действительна и никого не будет по ней найдено то вас об этом уведомят и вернут обратно в главное меню
Также <s>если вдруг, вы решили не мучать человека видосиками, то</> можете нажать кнопочку Назад, процесс добавления будет остановлен. И вас вернет обратно в главное меню.
4) В случае если кто то найден, вас об этом уведомят и останется лишь нажать на пару кнопочек.
5) Будет показана информация о человеке и попросят подтверждения на отправку запроса к нему. <s>Это будет последний ваш момент одуматься и отказаться добавлять человека в контакты, будьте так сказать аккуратны.</>
6) Человек получит уведомление о запросе и вам останется лишь ждать когда вас добавят в ответ. После этого ваши видео будут рассылаться по вашим контактам (куда вы только что добавили нового человека).

            <b>Как принять заявку в контакты:</>
1) Нажимаете кнопочку ''Обзор входящих запросов на добавления в мои контакты.''
2) Находите нужную кнопочку с именем того кого вы знаете и хотите добавить к себе в контакты
3) Нажимаете по этой кнопочке и всё. Человек добавлен, да, пока что таким образом :)

<b>Приятного пользования! Проект ещё очень и очень молод, постоянно улучшается и меняется, поэтому следите за обновлениями!
Либо вручную, либо через человека, что дал вам ссылку на этого чудо бота :D</>";
            await Utils.SendMessage(botClient, update, KeyboardUtils.GetReturnButtonMarkup(), cancellationToken: cancellationToken, helpText);
        }
        else
        {
            await botClient.SendMessage(update.Message.Chat.Id, "И что мне с этим делать?", cancellationToken: cancellationToken);
        }
    }

    public static async Task ProcessCallbackQuery(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, long chatId)
    {
        var callbackQuery = update.CallbackQuery;

        switch (callbackQuery.Data)
        {
            case "main_menu":
                await KeyboardUtils.SendInlineKeyboardMenu(botClient, update, cancellationToken);
                break;
            case "add_contact":
                await CallbackQueryMenuUtils.AddContact(botClient, update, cancellationToken);
                if (!TelegramBot.userStates.ContainsKey(chatId))
                {
                    TelegramBot.userStates[chatId] = new ProcessContactState();
                }
                break;
            case "get_self_link":
                await CallbackQueryMenuUtils.GetSelfLink(botClient, update, cancellationToken);
                break;
            case "view_inbound_invite_links":
                await CallbackQueryMenuUtils.ViewInboundInviteLinks(botClient, update, cancellationToken);
                break;
            case "view_contacts":
                await Contacts.ViewContacts(botClient, update, cancellationToken);
                break;
            case "mute_user":
                await Contacts.MuteUserContact(botClient, update, cancellationToken, chatId);
                break;
            case "unmute_user":
                await Contacts.UnMuteUserContact(botClient, update, cancellationToken, chatId);
                break;
            case "whos_the_genius":
                await CallbackQueryMenuUtils.WhosTheGenius(botClient, update, cancellationToken);
                break;
            default:
                if (callbackQuery.Data.StartsWith("user_accept_inbounds_invite:")) 
                {
                    await CallbackQueryMenuUtils.AcceptInboundInvite(update);
                }
                break;
        }
    }
}