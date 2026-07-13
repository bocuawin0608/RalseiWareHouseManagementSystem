use master
DROP DATABASE RalseiWarehouse
CREATE DATABASE RalseiWarehouse;
GO
USE RalseiWarehouse;
GO

CREATE TABLE Unit(
	UnitId INT IDENTITY(1,1) PRIMARY KEY,
	DisplayName NVARCHAR(MAX)
);
GO

CREATE TABLE Supplier(
	SupplierId INT IDENTITY(1,1) PRIMARY KEY,
	DisplayName NVARCHAR(MAX),
	[Address] NVARCHAR(MAX),
	Phone NVARCHAR(20),
	Email NVARCHAR(200),
	MoreInfo NVARCHAR(MAX),
	ContractDate DATETIME
);
GO

CREATE TABLE Customer(
	CustomerId INT IDENTITY(1,1) PRIMARY KEY,
	DisplayName NVARCHAR(MAX),
	[Address] NVARCHAR(MAX),
	Phone NVARCHAR(20),
	Email NVARCHAR(200),
	MoreInfo NVARCHAR(MAX),
	ContractDate DATETIME
);
GO

CREATE TABLE [Object](
	ObjectId NVARCHAR(128) PRIMARY KEY,
	DisplayName NVARCHAR(MAX),
	UnitId INT NOT NULL,
	SupplierId INT NOT NULL, 
	QRCode NVARCHAR(MAX),
	BarCode NVARCHAR(MAX),
	
	FOREIGN KEY (UnitId) REFERENCES Unit(UnitId),
	FOREIGN KEY (SupplierId) REFERENCES Supplier(SupplierId)
);
GO

-- 6. Bảng Quyền người dùng
CREATE TABLE [Role](
	RoleId INT IDENTITY(1,1) PRIMARY KEY,
	DisplayName NVARCHAR(MAX)
);
GO

-- 7. Bảng Người dùng (Nhân viên)
CREATE TABLE [User](
	UserId INT IDENTITY(1,1) PRIMARY KEY, -- Thêm PRIMARY KEY bị thiếu ở code cũ
	DisplayName NVARCHAR(MAX),
	UserName NVARCHAR(100) UNIQUE, -- Thêm UNIQUE để tránh trùng tên đăng nhập
	[Password] NVARCHAR(MAX),
	RoleId INT NOT NULL,
	
	FOREIGN KEY (RoleId) REFERENCES [Role](RoleId)
);
GO

-- 8. Bảng Phiếu Nhập kho
CREATE TABLE Input(
	InputId NVARCHAR(128) PRIMARY KEY,
	DateInput DATETIME DEFAULT GETDATE() -- Bổ sung ngày nhập kho
);
GO

-- 9. Bảng Chi tiết phiếu nhập (Bổ sung đầy đủ theo đặc tả)
CREATE TABLE InputInfo(
	Id NVARCHAR(128) PRIMARY KEY,
	ObjectId NVARCHAR(128) NOT NULL,
	InputId NVARCHAR(128) NOT NULL,
	Count INT NOT NULL,
	InputPrice FLOAT DEFAULT 0,
	OutputPrice FLOAT DEFAULT 0,
	[Status] NVARCHAR(MAX),
	
	FOREIGN KEY (ObjectId) REFERENCES [Object](ObjectId),
	FOREIGN KEY (InputId) REFERENCES Input(InputId)
);
GO

-- 10. Bảng Phiếu Xuất kho (Bổ sung đầy đủ theo đặc tả)
CREATE TABLE Output(
	OutputId NVARCHAR(128) PRIMARY KEY,
	DateOutput DATETIME DEFAULT GETDATE() -- Bổ sung ngày xuất kho
);
GO

-- 11. Bảng Chi tiết phiếu xuất (Bổ sung đầy đủ theo đặc tả)
CREATE TABLE OutputInfo(
	Id NVARCHAR(128) PRIMARY KEY,
	ObjectId NVARCHAR(128) NOT NULL,
	OutputId NVARCHAR(128) NOT NULL,
	CustomerId INT NOT NULL, -- Khách hàng mua sản phẩm xuất kho
	Count INT NOT NULL,
	[Status] NVARCHAR(MAX),
	
	FOREIGN KEY (ObjectId) REFERENCES [Object](ObjectId),
	FOREIGN KEY (OutputId) REFERENCES Output(OutputId),
	FOREIGN KEY (CustomerId) REFERENCES Customer(CustomerId)
);
GO