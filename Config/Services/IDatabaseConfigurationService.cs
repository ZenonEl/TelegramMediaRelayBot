// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

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