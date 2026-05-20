USE [SalonHairDB];
GO

CREATE TABLE [dbo].[Products] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Price] FLOAT NOT NULL,
    [ImageUrl] NVARCHAR(MAX) NULL
);
GO

CREATE TABLE [dbo].[Orders] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CustomerName] NVARCHAR(MAX) NOT NULL,
    [Phone] NVARCHAR(MAX) NOT NULL,
    [Address] NVARCHAR(MAX) NOT NULL,
    [OrderDate] DATETIME2(7) NOT NULL,
    [TotalAmount] FLOAT NOT NULL
);
GO

CREATE TABLE [dbo].[OrderDetails] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL,
    [Price] FLOAT NOT NULL,
    CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE
);
GO
