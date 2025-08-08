// Copyright (C) 2024-2025 ZenonEl
// GNU AGPL v3 or later

using System.Data;

namespace TelegramMediaRelayBot.Database.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }

    void Begin();
    void Commit();
    void Rollback();
}

