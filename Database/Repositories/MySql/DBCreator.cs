// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using Dapper;
using MySql.Data.MySqlClient;
using TelegramMediaRelayBot.Config.Services;

namespace TelegramMediaRelayBot.Database.Repositories.MySql;

public class MySqlDBCreator
{
    private readonly IDatabaseConfigurationService _databaseConfigurationService;

    public MySqlDBCreator(IDatabaseConfigurationService databaseConfigurationService)
    {
        _databaseConfigurationService = databaseConfigurationService;
    }

    public void CreateDatabase(string connectionString)
    {
        string databaseName = _databaseConfigurationService.GetDatabaseName();
        string query = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";
        using var connection = new MySqlConnection(connectionString);
        connection.Execute(query);
    }
}