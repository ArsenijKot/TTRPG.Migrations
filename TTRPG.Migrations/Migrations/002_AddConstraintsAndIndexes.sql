-- Обмеження на email
IF NOT EXISTS (
    SELECT * FROM sys.check_constraints 
    WHERE name = 'CHK_Members_Email'
)
BEGIN
    ALTER TABLE Members
    ADD CONSTRAINT CHK_Members_Email
    CHECK (email LIKE '%_@_%._%');
END
GO

-- Індекс на Members(name, nickname)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'ix_members_name_nickname' AND object_id = OBJECT_ID('Members')
)
BEGIN
    CREATE INDEX ix_members_name_nickname
    ON Members(name, nickname);
END
GO

-- Індекс на Games(rules_system)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'ix_games_rules_system' AND object_id = OBJECT_ID('Games')
)
BEGIN
    CREATE INDEX ix_games_rules_system
    ON Games(rules_system);
END
GO
