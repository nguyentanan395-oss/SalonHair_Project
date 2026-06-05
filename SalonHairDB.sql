-- SQL SCRIPT CHO CƠ SỞ DỮ LIỆU SALON HAIR LUXURY (CẬP NHẬT THEO DỰ ÁN)
-- Server: . (Local)
-- Database: SalonHairDB

CREATE DATABASE SalonHairDB;
GO

USE SalonHairDB;
GO
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Hairstyles] (
    [Id] int NOT NULL IDENTITY,
    [StyleName] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [FaceShape] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    CONSTRAINT [PK_Hairstyles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Price] float NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Roles] (
    [RoleId] int NOT NULL,
    [RoleName] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
);
GO

CREATE TABLE [Services] (
    [Id] int NOT NULL IDENTITY,
    [ServiceName] nvarchar(max) NOT NULL,
    [Price] float NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [RoleId] int NOT NULL,
    [Language] nvarchar(max) NULL,
    [OtpCode] nvarchar(max) NULL,
    [OtpExpiryTime] datetime2 NULL,
    [Email] nvarchar(max) NOT NULL,
    [IsEmailVerified] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Name] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Customers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Bookings] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [ServiceId] int NOT NULL,
    [BookingDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookings_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [TotalAmount] float NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Reviews] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NULL,
    [ProductId] int NULL,
    [ServiceId] int NULL,
    [HairstyleId] int NULL,
    [CustomerName] nvarchar(max) NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reviews_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Hairstyles_HairstyleId] FOREIGN KEY ([HairstyleId]) REFERENCES [Hairstyles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [OrderDetails] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [Price] float NOT NULL,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([RoleId], [RoleName])
VALUES (1, N'Customer'),
(2, N'Admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO

CREATE INDEX [IX_Bookings_CustomerId] ON [Bookings] ([CustomerId]);
GO

CREATE INDEX [IX_Bookings_ServiceId] ON [Bookings] ([ServiceId]);
GO

CREATE UNIQUE INDEX [IX_Customers_UserId] ON [Customers] ([UserId]) WHERE [UserId] IS NOT NULL;
GO

CREATE INDEX [IX_OrderDetails_OrderId] ON [OrderDetails] ([OrderId]);
GO

CREATE INDEX [IX_OrderDetails_ProductId] ON [OrderDetails] ([ProductId]);
GO

CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
GO

CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews] ([CustomerId]);
GO

CREATE INDEX [IX_Reviews_HairstyleId] ON [Reviews] ([HairstyleId]);
GO

CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
GO

CREATE INDEX [IX_Reviews_ServiceId] ON [Reviews] ([ServiceId]);
GO

CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260525142058_UpdateRelations', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Hairstyles] ADD [Gender] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260525155730_AddGenderToHairstyle', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Hairstyles] ADD [AgeGroup] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260526161157_AddAgeGroupToHairstyle', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Payments] (
    [PaymentId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [TransactionCode] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260527154127_AddPaymentTable', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[Payments].[PaymentMethod]', N'Method', N'COLUMN';
GO

ALTER TABLE [Payments] ADD [ProofImage] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260527154357_AddPayment', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE UNIQUE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
GO

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528072928_AddPaymentRelation', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528073310_FixPaymentAmount', N'8.0.4');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Payments] DROP CONSTRAINT [FK_Payments_Bookings_BookingId];
GO

DROP INDEX [IX_Payments_BookingId] ON [Payments];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'BookingId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Payments] ALTER COLUMN [BookingId] int NULL;
GO

ALTER TABLE [Payments] ADD [OrderId] int NULL;
GO

ALTER TABLE [Payments] ADD [PaidAt] datetime2 NULL;
GO

CREATE UNIQUE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]) WHERE [BookingId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]) WHERE [OrderId] IS NOT NULL;
GO

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]);
GO

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260531060123_UpdatePaymentForOrder', N'8.0.4');
GO

COMMIT;
GO

-- ==========================================
-- CHÈN DỮ LIỆU THỰC TẾ CHO BÁO CÁO
-- ==========================================

-- Bảng Services
SET IDENTITY_INSERT [Services] ON;
INSERT INTO [Services] ([Id], [ServiceName], [Price], [Description]) VALUES 
(1, N'Cắt tóc nam', 100000, N'Cắt tóc nam trẻ trung, hiện đại.'),
(2, N'Cắt tóc nữ', 200000, N'Cắt tóc nữ tạo kiểu thời trang.'),
(3, N'Nhuộm tóc Luxury', 500000, N'Nhuộm màu chuẩn, không hư tổn tóc.'),
(4, N'Uốn tóc cao cấp', 800000, N'Uốn xoăn, bồng bềnh quyến rũ.'),
(5, N'Gội đầu thảo dược', 50000, N'Thư giãn cùng tinh chất tự nhiên.'),
(6, N'Hấp dầu phục hồi', 300000, N'Nuôi dưỡng tóc từ sâu bên trong.');
SET IDENTITY_INSERT [Services] OFF;
GO

-- Bảng Hairstyles
SET IDENTITY_INSERT [Hairstyles] ON;
INSERT INTO [Hairstyles] ([Id], [StyleName], [FaceShape], [Gender], [AgeGroup], [Description], [ImageUrl]) VALUES 
(1, N'High Fade Pompadour', N'Tròn', N'Nam', N'18-30, 31-45', N'Tạo độ cao cho khuôn mặt, làm mặt trông dài và thon gọn hơn.', N'https://i.pinimg.com/564x/49/73/7a/49737a281804c86576b509f61b0c0342.jpg'),
(2, N'Layer Layer', N'Tròn', N'Nữ', N'Dưới 18, 18-30', N'Lớp tóc tỉa giúp che bớt độ rộng hai bên má.', N'https://i.pinimg.com/564x/87/42/85/8742857476579895655.jpg'),
(3, N'Side Part 7/3', N'Tròn', N'Nam', N'18-30, 31-45, 46+', N'Phong cách cổ điển, lịch lãm phù hợp mọi lứa tuổi.', N'https://i.pinimg.com/564x/23/e4/77/23e477651ad36a7a5601a35565551.jpg'),
(4, N'Crew Cut', N'Vuông', N'Nam', N'18-30, 31-45, 46+', N'Nam tính, gọn gàng và tôn lên đường nét xương hàm.', N'https://i.pinimg.com/564x/07/77/80/0777800c0f9942d93699c824f11b2685.jpg'),
(5, N'Undercut vuốt ngược', N'Vuông', N'Nam', N'Dưới 18, 18-30', N'Kiểu tóc hot trend giúp tôn vẻ góc cạnh mạnh mẽ.', N'https://i.pinimg.com/564x/1a/2b/3c/1a2b3c4d5e6f7g8h9i0j.jpg'),
(6, N'Ivy League', N'Vuông', N'Nam', N'18-30, 31-45', N'Mẫu mực và sang trọng cho quý ông công sở.', N'https://i.pinimg.com/564x/a1/b1/c1/a1b1c1d1e1f1g1h1i1j1.jpg'),
(7, N'Buzz Cut', N'Trái xoan', N'Nam', N'Dưới 18, 18-30', N'Khoe trọn vẹn gương mặt cân đối hoàn hảo.', N'https://i.pinimg.com/564x/01/02/03/01020304050607080910.jpg'),
(8, N'Mullet thời thượng', N'Trái xoan', N'Nam', N'Dưới 18, 18-30', N'Kiểu tóc cá tính và đầy phá cách.', N'https://i.pinimg.com/564x/11/12/13/11121314151617181920.jpg'),
(9, N'Uốn xoăn nhẹ', N'Trái xoan', N'Nữ', N'18-30, 31-45', N'Tạo vẻ lãng tử và trẻ trung.', N'https://i.pinimg.com/564x/21/22/23/21222324252627282930.jpg'),
(10, N'Middle Part (Bổ luống)', N'Dài', N'Nam', N'Dưới 18, 18-30', N'Cân bằng độ dài khuôn mặt, tạo sự hài hòa.', N'https://i.pinimg.com/564x/31/32/33/31323334353637383940.jpg'),
(11, N'Side Swept', N'Dài', N'Nam', N'18-30, 31-45', N'Tóc vuốt lệch một bên giúp mặt bớt cảm giác quá dài.', N'https://i.pinimg.com/564x/41/42/43/41424344454647484950.jpg'),
(12, N'Tóc Mái (Fringe)', N'Dài', N'Nam', N'Dưới 18, 18-30, 31-45', N'Che bớt phần trán giúp khuôn mặt cân đối hơn.', N'https://i.pinimg.com/564x/51/52/53/51525354555657585960.jpg');
SET IDENTITY_INSERT [Hairstyles] OFF;
GO

-- Bảng Products
SET IDENTITY_INSERT [Products] ON;
INSERT INTO [Products] ([Id], [Name], [Price], [Description], [ImageUrl]) VALUES 
(1, N'Sáp vuốt tóc Volcanic Clay', 250000, N'Độ giữ nếp cao, không bóng, dễ dàng gội rửa, phù hợp cho mọi loại tóc.', N'https://bizweb.dktcdn.net/thumb/1024x1024/100/171/314/products/sap-vuot-toc-volcanic-clay-chinh-hang.jpg'),
(2, N'Gôm xịt tóc Butterfly Shadow', 150000, N'Giữ nếp cực tốt, mùi hương trái cây nhẹ nhàng, dễ chịu.', N'https://bizweb.dktcdn.net/100/171/314/products/gom-xit-toc-butterfly-shadow-5.jpg'),
(3, N'Dầu dưỡng tóc Moroccanoil', 750000, N'Phục hồi tóc hư tổn, mang lại độ bóng mượt vượt trội.', N'https://bizweb.dktcdn.net/100/171/314/products/tinh-dau-duong-toc-moroccanoil-treatment.jpg'),
(4, N'Sáp vuốt tóc Kevin Murphy', 650000, N'Chất sáp cao cấp, bảo vệ tóc và giữ nếp hoàn hảo.', N'https://bizweb.dktcdn.net/100/171/314/products/sap-vuot-toc-kevin-murphy-rough-rider-100g.jpg');
SET IDENTITY_INSERT [Products] OFF;
GO

-- Bảng Users (Admin)
SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [Username], [Password], [RoleId], [Language], [OtpCode], [OtpExpiryTime], [Email], [IsEmailVerified]) VALUES 
(1, N'admin', N'$2a$11$WzB4x2E8G9h/O8q9rQvT.u7X6b4x8M7g0d2V4G5M6N7O8P9Q0.1R2', 2, NULL, NULL, NULL, N'admin@salonhair.com', 1);
SET IDENTITY_INSERT [Users] OFF;
GO

PRINT 'Da khoi tao Database SalonHairDB thanh cong voi du lieu mau chi tiet cho bao cao!';
