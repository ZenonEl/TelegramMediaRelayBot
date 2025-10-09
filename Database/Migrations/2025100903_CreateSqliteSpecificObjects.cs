using FluentMigrator;

namespace TelegramMediaRelayBot.Migrations
{
    [Migration(2025100903, "Create SQLite Specific Objects")]
    [Tags("sqlite")] // <-- Ключевой момент!
    public class M003_CreateSqliteSpecificObjects : Migration
    {
        public override void Up()
        {
            // Включаем поддержку foreign keys для сессии
            Execute.Sql("PRAGMA foreign_keys = ON;");

            // Для SQLite не создаем foreign keys, только индексы
            Create.Index("IX_Contacts_UserId").OnTable("Contacts").OnColumn("UserId");
            Create.Index("IX_Contacts_ContactId").OnTable("Contacts").OnColumn("ContactId");
            Create.Index("IX_MutedContacts_MutedByUserId").OnTable("MutedContacts").OnColumn("MutedByUserId");
            Create.Index("IX_MutedContacts_MutedContactId").OnTable("MutedContacts").OnColumn("MutedContactId");
            Create.Index("IX_UsersGroups_UserId").OnTable("UsersGroups").OnColumn("UserId");
            Create.Index("IX_GroupMembers_UserId").OnTable("GroupMembers").OnColumn("UserId");
            Create.Index("IX_GroupMembers_GroupId").OnTable("GroupMembers").OnColumn("GroupId");
            Create.Index("IX_GroupMembers_ContactId").OnTable("GroupMembers").OnColumn("ContactId");
            Create.Index("IX_DefaultUsersActions_UserId").OnTable("DefaultUsersActions").OnColumn("UserId");
            Create.Index("IX_DefaultUsersActionTargets_UserId").OnTable("DefaultUsersActionTargets").OnColumn("UserId");
            Create.Index("IX_DefaultUsersActionTargets_ActionID").OnTable("DefaultUsersActionTargets").OnColumn("ActionID");
            Create.Index("IX_PrivacySettings_UserId").OnTable("PrivacySettings").OnColumn("UserId");
            Create.Index("IX_InboxItems_OwnerUserId").OnTable("InboxItems").OnColumn("OwnerUserId");
        }

        public override void Down()
        {
            // Удаляем все созданные индексы
            Delete.Index("IX_Contacts_UserId");
            // ... и так далее для всех индексов
        }
    }
}