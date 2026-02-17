using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTrack.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNullableFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make entire migration idempotent - handle existing database schema
            migrationBuilder.Sql(@"
                -- Drop foreign keys if they exist
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PendingRegistrations_Users_ProcessedByUserId')
                    ALTER TABLE [PendingRegistrations] DROP CONSTRAINT [FK_PendingRegistrations_Users_ProcessedByUserId];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Projects_ProjectId')
                    ALTER TABLE [Tasks] DROP CONSTRAINT [FK_Tasks_Projects_ProjectId];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTimes_Users_UserId')
                    ALTER TABLE [TaskTimes] DROP CONSTRAINT [FK_TaskTimes_Users_UserId];

                -- Drop indexes if they exist
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeLogs_UserId_Date' AND object_id = OBJECT_ID('TimeLogs'))
                    DROP INDEX [IX_TimeLogs_UserId_Date] ON [TimeLogs];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTimes_TaskId_UserId_Date' AND object_id = OBJECT_ID('TaskTimes'))
                    DROP INDEX [IX_TaskTimes_TaskId_UserId_Date] ON [TaskTimes];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Projects_Status' AND object_id = OBJECT_ID('Projects'))
                    DROP INDEX [IX_Projects_Status] ON [Projects];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PendingRegistrations_Email' AND object_id = OBJECT_ID('PendingRegistrations'))
                    DROP INDEX [IX_PendingRegistrations_Email] ON [PendingRegistrations];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId_Status' AND object_id = OBJECT_ID('Notifications'))
                    DROP INDEX [IX_Notifications_UserId_Status] ON [Notifications];

                -- Clear nullable FK references and conditionally delete seeded user
                UPDATE [Tasks] SET [ApprovedByUserId] = NULL WHERE [ApprovedByUserId] = 1;
                UPDATE [PendingRegistrations] SET [ProcessedByUserId] = NULL WHERE [ProcessedByUserId] = 1;

                IF NOT EXISTS (SELECT 1 FROM [Tasks] WHERE [AssignedToUserId] = 1 OR [CreatedByUserId] = 1)
                   AND NOT EXISTS (SELECT 1 FROM [TimeLogs] WHERE [UserId] = 1)
                   AND NOT EXISTS (SELECT 1 FROM [TaskTimes] WHERE [UserId] = 1)
                   AND NOT EXISTS (SELECT 1 FROM [Notifications] WHERE [UserId] = 1)
                BEGIN
                    DELETE FROM [Users] WHERE [UserId] = 1;
                END

                -- Remove default constraints if they exist before altering columns
                DECLARE @constraintName nvarchar(200);

                SELECT @constraintName = d.name FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('Users') AND c.name = 'Status';
                IF @constraintName IS NOT NULL EXEC('ALTER TABLE [Users] DROP CONSTRAINT [' + @constraintName + ']');

                SELECT @constraintName = d.name FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('Tasks') AND c.name = 'Status';
                IF @constraintName IS NOT NULL EXEC('ALTER TABLE [Tasks] DROP CONSTRAINT [' + @constraintName + ']');

                SELECT @constraintName = d.name FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('Tasks') AND c.name = 'Priority';
                IF @constraintName IS NOT NULL EXEC('ALTER TABLE [Tasks] DROP CONSTRAINT [' + @constraintName + ']');

                SELECT @constraintName = d.name FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('Projects') AND c.name = 'Status';
                IF @constraintName IS NOT NULL EXEC('ALTER TABLE [Projects] DROP CONSTRAINT [' + @constraintName + ']');

                SELECT @constraintName = d.name FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('PendingRegistrations') AND c.name = 'Status';
                IF @constraintName IS NOT NULL EXEC('ALTER TABLE [PendingRegistrations] DROP CONSTRAINT [' + @constraintName + ']');

                SELECT @constraintName = d.name FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('Notifications') AND c.name = 'Status';
                IF @constraintName IS NOT NULL EXEC('ALTER TABLE [Notifications] DROP CONSTRAINT [' + @constraintName + ']');

                -- Create indexes if they don't exist
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeLogs_UserId' AND object_id = OBJECT_ID('TimeLogs'))
                    CREATE INDEX [IX_TimeLogs_UserId] ON [TimeLogs] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTimes_TaskId' AND object_id = OBJECT_ID('TaskTimes'))
                    CREATE INDEX [IX_TaskTimes_TaskId] ON [TaskTimes] ([TaskId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId' AND object_id = OBJECT_ID('Notifications'))
                    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

                -- Re-add foreign keys if they don't exist
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PendingRegistrations_Users_ProcessedByUserId')
                    ALTER TABLE [PendingRegistrations] ADD CONSTRAINT [FK_PendingRegistrations_Users_ProcessedByUserId]
                    FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([UserId]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Projects_ProjectId')
                    ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Projects_ProjectId]
                    FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTimes_Users_UserId')
                    ALTER TABLE [TaskTimes] ADD CONSTRAINT [FK_TaskTimes_Users_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Drop foreign keys if they exist
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PendingRegistrations_Users_ProcessedByUserId')
                    ALTER TABLE [PendingRegistrations] DROP CONSTRAINT [FK_PendingRegistrations_Users_ProcessedByUserId];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Projects_ProjectId')
                    ALTER TABLE [Tasks] DROP CONSTRAINT [FK_Tasks_Projects_ProjectId];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTimes_Users_UserId')
                    ALTER TABLE [TaskTimes] DROP CONSTRAINT [FK_TaskTimes_Users_UserId];

                -- Drop indexes if they exist
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeLogs_UserId' AND object_id = OBJECT_ID('TimeLogs'))
                    DROP INDEX [IX_TimeLogs_UserId] ON [TimeLogs];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTimes_TaskId' AND object_id = OBJECT_ID('TaskTimes'))
                    DROP INDEX [IX_TaskTimes_TaskId] ON [TaskTimes];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId' AND object_id = OBJECT_ID('Notifications'))
                    DROP INDEX [IX_Notifications_UserId] ON [Notifications];

                -- Recreate original indexes
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TimeLogs_UserId_Date' AND object_id = OBJECT_ID('TimeLogs'))
                    CREATE INDEX [IX_TimeLogs_UserId_Date] ON [TimeLogs] ([UserId], [Date]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTimes_TaskId_UserId_Date' AND object_id = OBJECT_ID('TaskTimes'))
                    CREATE INDEX [IX_TaskTimes_TaskId_UserId_Date] ON [TaskTimes] ([TaskId], [UserId], [Date]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Projects_Status' AND object_id = OBJECT_ID('Projects'))
                    CREATE INDEX [IX_Projects_Status] ON [Projects] ([Status]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PendingRegistrations_Email' AND object_id = OBJECT_ID('PendingRegistrations'))
                    CREATE UNIQUE INDEX [IX_PendingRegistrations_Email] ON [PendingRegistrations] ([Email]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notifications_UserId_Status' AND object_id = OBJECT_ID('Notifications'))
                    CREATE INDEX [IX_Notifications_UserId_Status] ON [Notifications] ([UserId], [Status]);

                -- Re-add original foreign keys
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PendingRegistrations_Users_ProcessedByUserId')
                    ALTER TABLE [PendingRegistrations] ADD CONSTRAINT [FK_PendingRegistrations_Users_ProcessedByUserId]
                    FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Projects_ProjectId')
                    ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Projects_ProjectId]
                    FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]) ON DELETE SET NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTimes_Users_UserId')
                    ALTER TABLE [TaskTimes] ADD CONSTRAINT [FK_TaskTimes_Users_UserId]
                    FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);
            ");
        }
    }
}
