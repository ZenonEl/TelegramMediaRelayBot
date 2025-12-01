// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

using Dapper;
using MySql.Data.MySqlClient;

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
        using MySqlConnection connection = new MySqlConnection(connectionString);
        connection.Execute(query);
    }
}
