// Copyright (C) 2024-2025 ZenonEl
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Эта программа является свободным программным обеспечением: вы можете распространять и/или изменять
// её на условиях Стандартной общественной лицензии GNU Affero, опубликованной
// Фондом свободного программного обеспечения, либо версии 3 лицензии, либо
// (по вашему выбору) любой более поздней версии.

using TelegramMediaRelayBot;


namespace DataBase.DBCreating;

public class AllCreatingFunc
{
    public static string connectionString = Config.sqlConnectionString!;
    public static void CreateDatabase()
    {
        string query = $"CREATE DATABASE IF NOT EXISTS {Config.databaseName};";
        Utils.executeVoidQuery(query);
    }

    public static void CreateUsersTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS Users (
                ID INT PRIMARY KEY AUTO_INCREMENT,
                TelegramID BIGINT NOT NULL,
                Name VARCHAR(255) NOT NULL,
                Link VARCHAR(255) NOT NULL,
                Status VARCHAR(255)
            )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateContactsTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS Contacts (
                UserId INT,
                ContactId INT,
                status VARCHAR(255),
                PRIMARY KEY (UserId, ContactId),
                FOREIGN KEY (UserId) REFERENCES Users(ID),
                FOREIGN KEY (ContactId) REFERENCES Users(ID)
            )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateMutedContactsTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS MutedContacts (
                MutedId INT PRIMARY KEY AUTO_INCREMENT,
                MutedByUserId INT NOT NULL,
                MutedContactId INT NOT NULL,
                MuteDate DATETIME NOT NULL,
                ExpirationDate DATETIME NULL,
                IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                UNIQUE (MutedByUserId, MutedContactId),
                FOREIGN KEY (MutedByUserId) REFERENCES Users(ID),
                FOREIGN KEY (MutedContactId) REFERENCES Users(ID)
            )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateGroupsTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS UsersGroups (
                ID INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT NOT NULL,
                GroupName VARCHAR(255) NOT NULL,
                Description TEXT NULL,
                IsDefaultEnabled BOOLEAN NOT NULL DEFAULT TRUE,
                UNIQUE (UserId, GroupName),
                FOREIGN KEY (UserId) REFERENCES Users(ID)
            )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateGroupMembersTable()
    {
        string query = @$"
            USE {Config.databaseName};
            CREATE TABLE IF NOT EXISTS GroupMembers (
                ID INT PRIMARY KEY AUTO_INCREMENT,
                UserId INT NOT NULL,
                ContactId INT NOT NULL,
                GroupId INT NOT NULL,
                Status BOOLEAN NOT NULL DEFAULT TRUE,
                UNIQUE (GroupId, ContactId),
                FOREIGN KEY (UserId) REFERENCES Users(ID),
                FOREIGN KEY (ContactId) REFERENCES Users(ID),
                FOREIGN KEY (GroupId) REFERENCES UsersGroups(ID)
            )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateDefaultUsersActions()
    {
        string query = @$"
        USE {Config.databaseName};
        CREATE TABLE IF NOT EXISTS DefaultUsersActions (
            ID INT PRIMARY KEY AUTO_INCREMENT,
            UserId INT NOT NULL,
            Type VARCHAR(255) NOT NULL,
            Action VARCHAR(255),
            IsActive BOOLEAN NOT NULL DEFAULT TRUE,
            ActionCondition VARCHAR(255),
            UNIQUE (UserId, Type),
            FOREIGN KEY (UserId) REFERENCES Users(ID)
        )";

        Utils.executeVoidQuery(query);
    }

    public static void CreateDefaultUsersActionTargets()
    {
        string query = @$"
        USE {Config.databaseName};
        CREATE TABLE IF NOT EXISTS DefaultUsersActionTargets (
            ID INT PRIMARY KEY AUTO_INCREMENT,
            UserId INT NOT NULL,
            ActionID INT NOT NULL,
            TargetType VARCHAR(255) NOT NULL,
            TargetID VARCHAR(255) NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users(ID) ON DELETE CASCADE,
            FOREIGN KEY (ActionID) REFERENCES DefaultUsersActions(ID) ON DELETE CASCADE,
            UNIQUE (UserId, ActionID, TargetID)
        );";

        Utils.executeVoidQuery(query);
    }
}