IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Momo_IdentityDb')
BEGIN
    CREATE DATABASE Momo_IdentityDb;
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Momo_WalletDb')
BEGIN
    CREATE DATABASE Momo_WalletDb;
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Momo_TransactionDb')
BEGIN
    CREATE DATABASE Momo_TransactionDb;
END
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Momo_NotificationDb')
BEGIN
    CREATE DATABASE Momo_NotificationDb;
END
GO
