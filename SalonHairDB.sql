-- SQL SCRIPT CHO CƠ SỞ DỮ LIỆU SALON HAIR LUXURY (PHIÊN BẢN BÁO CÁO TOÀN DIỆN)
-- Server: . (Local)
-- Database: SalonHairDB

CREATE DATABASE SalonHairDB;
GO

USE SalonHairDB;
GO

-- ==========================================
-- 1. TẠO CÁC BẢNG CƠ SỞ DỮ LIỆU (SCHEMA)
-- ==========================================

-- Bảng Dịch vụ (Services)
CREATE TABLE [dbo].[Services] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [ServiceName] NVARCHAR (MAX) NOT NULL,
    [Price]       FLOAT (53)     NOT NULL, 
    [Description] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Services] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Bảng Kiểu tóc AI (Hairstyles)
CREATE TABLE [dbo].[Hairstyles] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [StyleName]   NVARCHAR (MAX) NOT NULL,
    [FaceShape]   NVARCHAR (MAX) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [ImageUrl]    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Hairstyles] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Bảng Lịch đặt (Bookings)
CREATE TABLE [dbo].[Bookings] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [CustomerName] NVARCHAR (MAX) NOT NULL,
    [Phone]        NVARCHAR (MAX) NOT NULL,
    [ServiceId]    INT            NOT NULL,
    [BookingDate]  DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Bookings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id]) ON DELETE CASCADE
);

-- Bảng Sản phẩm (Products)
CREATE TABLE [dbo].[Products] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (MAX) NOT NULL,
    [Price]       FLOAT (53)     NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [ImageUrl]    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- ==========================================
-- 2. CHÈN DỮ LIỆU THỰC TẾ CHO BÁO CÁO
-- ==========================================

-- Chèn danh mục Dịch vụ Salon
SET IDENTITY_INSERT [dbo].[Services] ON;
INSERT INTO [dbo].[Services] ([Id], [ServiceName], [Price], [Description]) VALUES 
(1, N'Cắt tóc nam', 150000, N'Cắt tỉa tạo kiểu chuyên nghiệp từ thợ chính'),
(2, N'Combo Gội - Cắt - Sấy', 250000, N'Dịch vụ trọn gói giúp thư giãn và sạch sẽ'),
(3, N'Uốn Prepping / Xoăn', 500000, N'Tạo kiểu uốn hiện đại cho mái tóc cá tính'),
(4, N'Nhuộm màu thời trang', 600000, N'Màu sắc chuẩn salon, thuốc nhuộm cao cấp');
SET IDENTITY_INSERT [dbo].[Services] OFF;

-- Chèn dữ liệu gợi ý kiểu tóc cho tư vấn AI
INSERT INTO [dbo].[Hairstyles] ([StyleName], [FaceShape], [Description], [ImageUrl]) VALUES 
(N'High Fade Pompadour', N'Tròn', N'Tạo độ cao cho khuôn mặt, làm mặt trông dài và thon gọn hơn.', N'https://i.pinimg.com/564x/49/73/7a/49737a281804c86576b509f61b0c0342.jpg'),
(N'Layer Layer', N'Tròn', N'Lớp tóc tỉa giúp che bớt độ rộng hai bên má.', N'https://i.pinimg.com/564x/87/42/85/8742857476579895655.jpg'),
(N'Side Part 7/3', N'Tròn', N'Phong cách cổ điển, lịch lãm phù hợp mọi lứa tuổi.', N'https://i.pinimg.com/564x/23/e4/77/23e477651ad36a7a5601a35565551.jpg'),
(N'Crew Cut', N'Vuông', N'Nam tính, gọn gàng và tôn lên đường nét xương hàm.', N'https://i.pinimg.com/564x/07/77/80/0777800c0f9942d93699c824f11b2685.jpg'),
(N'Undercut vuốt ngược', N'Vuông', N'Kiểu tóc hot trend giúp tôn vẻ góc cạnh mạnh mẽ.', N'https://i.pinimg.com/564x/1a/2b/3c/1a2b3c4d5e6f7g8h9i0j.jpg'),
(N'Slick Back Classic', N'Vuông', N'Vuốt ngược nam tính thể hiện sự quyền uy.', N'https://i.pinimg.com/564x/12/34/56/1234567890abcdef1234.jpg'),
(N'Middle Part (Bổ luống)', N'Dài', N'Cân bằng độ dài khuôn mặt, tạo sự hài hòa.', N'https://i.pinimg.com/564x/31/32/33/31323334353637383940.jpg'),
(N'Side Swept', N'Dài', N'Tóc vuốt lệch một bên giúp mặt bớt cảm giác quá dài.', N'https://i.pinimg.com/564x/41/42/43/41424344454647484950.jpg'),
(N'Buzz Cut', N'Trái Xoan', N'Khoe trọn vẹn gương mặt cân đối hoàn hảo.', N'https://i.pinimg.com/564x/01/02/03/01020304050607080910.jpg'),
(N'Mullet thời thượng', N'Trái Xoan', N'Kiểu tóc cá tính và đầy phá cách dẫn đầu xu hướng.', N'https://i.pinimg.com/564x/11/12/13/11121314151617181920.jpg');

-- Chèn dữ liệu Lịch hẹn mẫu để báo cáo ĐẸP nhất
INSERT INTO [dbo].[Bookings] ([CustomerName], [Phone], [ServiceId], [BookingDate]) VALUES 
(N'Phạm Quang Vinh', N'0969301205', 1, '2026-02-01 13:25:00'),
(N'Nguyễn Văn An', N'0912345678', 2, '2026-04-02 18:00:00'),
(N'Trần Minh Hoàng', N'0988776655', 3, '2026-04-03 10:30:00');

-- Chèn dữ liệu Sản phẩm mẫu (Sử dụng đường dẫn tương đối để không bị mất ảnh)
INSERT INTO [dbo].[Products] ([Name], [Price], [Description], [ImageUrl]) VALUES 
(N'Sáp vuốt tóc Kevin Murphy', 650000, N'Giữ nếp cực tốt, không bóng tóc.', N'/images/products/sáp-kevin.jpg'),
(N'Tinh dầu dưỡng tóc Moroccanoil', 850000, N'Phục hồi tóc hư tổn từ sâu bên trong.', N'/images/products/moroccan-oil.jpg'),
(N'Gôm xịt tóc Butterfly Shadow', 120000, N'Cố định form tóc suốt cả ngày dài.', N'/images/products/gom-butterfly.jpg');

PRINT 'Da khoi tao Database SalonHairDB thanh cong voi du lieu mau chi tiet cho bao cao!';
