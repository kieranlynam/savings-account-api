CREATE TABLE IF NOT EXISTS SavingsAccounts (
    Id TEXT PRIMARY KEY NOT NULL,
    Balance DECIMAL(18,2) NOT NULL,
    InterestRate DECIMAL(5,4) NOT NULL,
    CreatedAt TEXT NOT NULL,
    Version INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Transactions (
    Id TEXT PRIMARY KEY NOT NULL,
    AccountId TEXT NOT NULL,
    Type INTEGER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Timestamp TEXT NOT NULL,
    IdempotencyKey TEXT,
    FOREIGN KEY (AccountId) REFERENCES SavingsAccounts(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId ON Transactions(AccountId);
CREATE INDEX IF NOT EXISTS IX_Transactions_IdempotencyKey ON Transactions(IdempotencyKey);