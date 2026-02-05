-- Таблиця Members
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Members]') AND type in (N'U'))
BEGIN
    CREATE TABLE Members (
        member_id INT IDENTITY PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        nickname NVARCHAR(50),
        email NVARCHAR(256) NOT NULL UNIQUE,
        join_date DATE NOT NULL
    );
END
GO

-- Таблиця Games
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Games]') AND type in (N'U'))
BEGIN
    CREATE TABLE Games (
        game_id INT IDENTITY PRIMARY KEY,
        title NVARCHAR(100) NOT NULL,
        genre NVARCHAR(50),
        rules_system NVARCHAR(50) NOT NULL
    );
END
GO

-- Таблиця Roles
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE Roles (
        role_id INT IDENTITY PRIMARY KEY,
        role_name NVARCHAR(50) NOT NULL UNIQUE,
        description NVARCHAR(200)
    );
END
GO
