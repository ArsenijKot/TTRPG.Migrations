-- Таблиця Sessions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Sessions]') AND type in (N'U'))
BEGIN
    CREATE TABLE Sessions (
        session_id INT IDENTITY PRIMARY KEY,
        date DATE NOT NULL,
        online BIT NOT NULL,
        online_link NVARCHAR(200),
        table_number INT,
        game_id INT NOT NULL,
        CONSTRAINT FK_Sessions_Games
            FOREIGN KEY (game_id) REFERENCES Games(game_id)
    );
END
GO

-- Таблиця Session_Participants
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Session_Participants]') AND type in (N'U'))
BEGIN
    CREATE TABLE Session_Participants (
        session_id INT NOT NULL,
        member_id INT NOT NULL,
        role_id INT NOT NULL,
        character_name NVARCHAR(100),
        CONSTRAINT PK_SessionParticipants PRIMARY KEY (session_id, member_id),
        CONSTRAINT FK_SP_Session FOREIGN KEY (session_id) REFERENCES Sessions(session_id),
        CONSTRAINT FK_SP_Member FOREIGN KEY (member_id) REFERENCES Members(member_id),
        CONSTRAINT FK_SP_Role FOREIGN KEY (role_id) REFERENCES Roles(role_id)
    );
END
GO
