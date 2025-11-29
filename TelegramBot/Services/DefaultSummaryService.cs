// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using TelegramMediaRelayBot.Database.Interfaces;
using TelegramMediaRelayBot.Database;

namespace TelegramMediaRelayBot.TelegramBot.Services;

/// <summary>
/// Builds formatted summary texts for default actions and privacy settings.
/// </summary>
public interface IDefaultSummaryService
{
	Task<string> BuildDefaultsSummary(Update update);
	Task<string> BuildTargetsSummary(Update update);
	Task<string> BuildTimeoutSummary(Update update);
	Task<string> BuildPrivacySummary(Update update);
}

public sealed class DefaultSummaryService : IDefaultSummaryService
{
	private readonly IUserGetter _userGetter;
	private readonly IDefaultActionGetter _defaultGetter;
	private readonly IGroupGetter _groupGetter;
	private readonly IPrivacySettingsGetter _privacyGetter;
	private readonly IResourceService _resourceService;

	public DefaultSummaryService(IUserGetter userGetter, IDefaultActionGetter defaultGetter, IGroupGetter groupGetter, IResourceService resourceService, IPrivacySettingsGetter privacyGetter)
	{
		_userGetter = userGetter;
		_defaultGetter = defaultGetter;
		_groupGetter = groupGetter;
		_privacyGetter = privacyGetter;
		_resourceService = resourceService;
	}

	public async Task<string> BuildDefaultsSummary(Update update)
	{
		try
		{
			long chatId = update.CallbackQuery!.Message!.Chat.Id;
			int userId = _userGetter.GetUserIDbyTelegramID(chatId);
			string da = _defaultGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
			string action = string.Empty; string condition = string.Empty;
			if (!string.IsNullOrEmpty(da) && da != UsersAction.NO_VALUE && da.Contains(';'))
			{
				var parts = da.Split(';');
				action = parts.ElementAtOrDefault(0) ?? string.Empty;
				condition = parts.ElementAtOrDefault(1) ?? string.Empty;
			}
			int actionId = _defaultGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
			var users = _defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
			var groups = _defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
			var groupNames = new List<string>();
			foreach (var gid in groups)
			{
				try { var name = await _groupGetter.GetGroupNameById(gid); groupNames.Add($"{System.Net.WebUtility.HtmlEncode(name)} (ID: {gid})"); } catch { }
			}
			return $"<b>{_resourceService.GetResourceString("Summary.Defaults.Header")}</b>\n" +
			       $"{_resourceService.GetResourceString("Summary.Defaults.Action")}: <code>{System.Net.WebUtility.HtmlEncode(action)}</code>\n" +
			       $"{_resourceService.GetResourceString("Summary.Defaults.Timeout")}: <code>{System.Net.WebUtility.HtmlEncode(condition)}</code>\n" +
			       $"{_resourceService.GetResourceString("Summary.Defaults.Users")}: <code>{string.Join(", ", users)}</code>\n" +
			       (groupNames.Count > 0 ? $"{_resourceService.GetResourceString("Summary.Defaults.Groups")}: {string.Join(", ", groupNames)}\n\n" : "\n");
		}
		catch { return string.Empty; }
	}

	public async Task<string> BuildTargetsSummary(Update update)
	{
		try
		{
			long chatId = update.CallbackQuery!.Message!.Chat.Id;
			int userId = _userGetter.GetUserIDbyTelegramID(chatId);
			int actionId = _defaultGetter.GetDefaultActionId(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
			var users = _defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.USER, actionId);
			var groups = _defaultGetter.GetAllDefaultUsersActionTargets(userId, TargetTypes.GROUP, actionId);
			var groupNames = new List<string>();
			foreach (var gid in groups) { try { var name = await _groupGetter.GetGroupNameById(gid); groupNames.Add($"{System.Net.WebUtility.HtmlEncode(name)} (ID: {gid})"); } catch { } }
			return $"{_resourceService.GetResourceString("Summary.Defaults.Users")}: <code>{string.Join(", ", users)}</code>\n" +
			       (groupNames.Count > 0 ? $"{_resourceService.GetResourceString("Summary.Defaults.Groups")}: {string.Join(", ", groupNames)}\n\n" : "\n");
		}
		catch { return string.Empty; }
	}

	public Task<string> BuildTimeoutSummary(Update update)
	{
		try
		{
			long chatId = update.CallbackQuery!.Message!.Chat.Id;
			int userId = _userGetter.GetUserIDbyTelegramID(chatId);
			string da = _defaultGetter.GetDefaultActionByUserIDAndType(userId, UsersActionTypes.DEFAULT_MEDIA_DISTRIBUTION);
			string condition = string.Empty;
			if (!string.IsNullOrEmpty(da) && da != UsersAction.NO_VALUE && da.Contains(';'))
			{
				var parts = da.Split(';');
				condition = parts.ElementAtOrDefault(1) ?? string.Empty;
			}
			return Task.FromResult($"{_resourceService.GetResourceString("Summary.Defaults.Timeout")}: <code>{System.Net.WebUtility.HtmlEncode(condition)}</code>\n\n");
		}
		catch { return Task.FromResult(string.Empty); }
	}

	public Task<string> BuildPrivacySummary(Update update)
	{
		try
		{
			long chatId = update.CallbackQuery!.Message!.Chat.Id;
			int userId = _userGetter.GetUserIDbyTelegramID(chatId);
			var enabled = new List<string>();
			if (_privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SOCIAL_SITE_FILTER)) enabled.Add(_resourceService.GetResourceString("PrivacyFilter.Social"));
			if (_privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.NSFW_SITE_FILTER)) enabled.Add(_resourceService.GetResourceString("PrivacyFilter.NSFW"));
			if (_privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.UNIFIED_SITE_FILTER)) enabled.Add(_resourceService.GetResourceString("PrivacyFilter.Unified"));
			bool domainsOn = _privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.SITES_BY_DOMAIN_FILTER);
			bool inboxOn = _privacyGetter.GetIsActivePrivacyRule(userId, PrivacyRuleType.INBOX_DELIVERY);
			string domainInfo = domainsOn ? _resourceService.GetResourceString("DomainsFilterOn") : _resourceService.GetResourceString("DomainsFilterOff");
			string inboxInfo = inboxOn ? _resourceService.GetResourceString("InboxOn") : _resourceService.GetResourceString("InboxOff");
			string preface = string.Format(_resourceService.GetResourceString("PrivacyPrefaceTemplate"), string.Join(", ", enabled), domainInfo, inboxInfo);
			return Task.FromResult(preface);
		}
		catch { return Task.FromResult(string.Empty); }
	}
}

