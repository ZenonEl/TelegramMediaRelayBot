// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information.

namespace TelegramMediaRelayBot.Config.Services;

/// <summary>
/// Service for accessing database configuration
/// </summary>
public interface IDatabaseConfigurationService
{
    /// <summary>
    /// Gets the database connection string
    /// </summary>
    /// <returns>Connection string</returns>
    string GetConnectionString();
    
    /// <summary>
    /// Gets the database type (mysql/sqlite)
    /// </summary>
    /// <returns>Database type</returns>
    string GetDatabaseType();
    
    /// <summary>
    /// Gets the database name
    /// </summary>
    /// <returns>Database name</returns>
    string GetDatabaseName();
} 