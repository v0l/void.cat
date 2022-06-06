﻿namespace VoidCat.Services.Migrations;

public interface IMigration
{
    ValueTask Migrate(string[] args);
    bool ExitOnComplete { get; }
}