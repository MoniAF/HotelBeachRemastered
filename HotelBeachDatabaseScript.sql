USE master
GO

IF DB_ID(N'HotelBeachDB') IS NULL
BEGIN
    CREATE DATABASE HotelBeachDB;
END
GO

USE HotelBeachDB
GO

DROP TABLE IF EXISTS dbo.Cheques;
DROP TABLE IF EXISTS dbo.Reservaciones;
DROP TABLE IF EXISTS dbo.Clientes;
DROP TABLE IF EXISTS dbo.Paquetes;
DROP TABLE IF EXISTS dbo.Empleados;

DROP TABLE IF EXISTS dbo.Reservaciones_Auditoria;
DROP TABLE IF EXISTS dbo.Clientes_Auditoria;
DROP TABLE IF EXISTS dbo.Paquetes_Auditoria;
DROP TABLE IF EXISTS dbo.Empleados_Auditoria;

DROP SEQUENCE IF EXISTS dbo.ReservationNumberSequence;
DROP SEQUENCE IF EXISTS dbo.CheckNumberSequence;
GO

CREATE TABLE [Reservaciones](
Id int primary key not null,
CedulaCliente varchar(25) not null,
IdPaquete int not null,
TipoPago varchar(25) not null,
FechaReserva Date not null,
Duracion int not null,
Subtotal decimal(12,2) not null,
Impuesto decimal(12,2) not null,
Descuento decimal(12,2) not null,
MontoTotal decimal(12,2) not null,
Adelanto decimal(12,2) not null,
MontoMensualidad decimal(12,2) not null,
Estado char(1) not null)
GO

SELECT * FROM Reservaciones
GO

CREATE TABLE [Empleados](
ID int not null primary key identity,
NombreCompleto varchar(150) not null,
Email varchar(50) not null,
Password varchar(255) not null,
TipoUsuario int not null,
FechaRegistro datetime not null default getdate(),
Estado char(1) not null)
GO

INSERT INTO Empleados (NombreCompleto, Email, Password, TipoUsuario, Estado)
VALUES ('Monica Artavia', 'sahotelbeach@gmail.com', 'remaster1234', 1, 'A')
GO
INSERT INTO Empleados (NombreCompleto, Email, Password, TipoUsuario, Estado)
VALUES ('Lana Garcia', 'lgarcia@gmail.com', 'remaster1234', 2, 'A')
GO

SELECT * FROM Empleados
GO

CREATE TABLE [Paquetes](
ID int not null primary key identity,
NombrePaquete varchar(150) not null,
Precio decimal(12,2) not null,
PorcentajePrima decimal(12,2) not null,
LimiteMeses int not null,
FechaRegistro datetime not null default getdate(),
Estado char(1) not null)
GO

INSERT INTO Paquetes (NombrePaquete, Precio, PorcentajePrima, LimiteMeses, Estado)
VALUES ('All-inclusive', 450, 45, 24, 'A');
GO
INSERT INTO Paquetes (NombrePaquete, Precio, PorcentajePrima, LimiteMeses, Estado)
VALUES ('Meal plan', 275, 35, 18, 'A');
GO
INSERT INTO Paquetes (NombrePaquete, Precio, PorcentajePrima, LimiteMeses, Estado)
VALUES ('Accommodation', 210, 15, 12, 'A');
GO

SELECT * FROM Paquetes
GO

CREATE TABLE [Clientes](
Cedula varchar(25) not null primary key,
TipoCedula varchar(20) not null,
NombreCompleto varchar(150) not null,
Telefono varchar(15) not null,
Direccion varchar(150) not null,
Email varchar(150) not null,
Password varchar(255) not null,
TipoUsuario int not null,
Restablecer int not null,
FechaRegistro datetime not null default getdate(),
Estado char(1) not null)
GO

SELECT * FROM Clientes
GO

ALTER TABLE Reservaciones ADD
CONSTRAINT [FK_Reservaciones_Clientes] FOREIGN KEY (CedulaCliente)
REFERENCES [Clientes](Cedula)
GO

ALTER TABLE Reservaciones ADD
CONSTRAINT [FK_Reservaciones_Paquetes] FOREIGN KEY (IdPaquete)
REFERENCES [Paquetes](ID)
GO

CREATE TABLE [Cheques](
IdCheque int primary key not null,
NumeroCheque int not null,
NombreBanco varchar(100) not null,
IdReservacion int not null)
GO

SELECT * FROM Cheques
GO

ALTER TABLE Cheques ADD
CONSTRAINT [FK_Cheques_Reservaciones] FOREIGN KEY (IdReservacion)
REFERENCES [Reservaciones](Id)
GO

-- Identifier sequences

CREATE SEQUENCE dbo.ReservationNumberSequence
    AS int
    START WITH 1
    INCREMENT BY 1
    NO CYCLE;
GO

CREATE SEQUENCE dbo.CheckNumberSequence
    AS int
    START WITH 1
    INCREMENT BY 1
    NO CYCLE;
GO

-- Audit tables

CREATE TABLE [Reservaciones_Auditoria](
Accion varchar(25) not null,
FechaCambio datetime not null default getdate(),
Id int not null,
CedulaCliente varchar(25) not null,
IdPaquete int not null,
TipoPago varchar(25) not null,
FechaReserva Date not null,
Duracion int not null,
Subtotal decimal(12,2) not null,
Impuesto decimal(12,2) not null,
Descuento decimal(12,2) not null,
MontoTotal decimal(12,2) not null,
Adelanto decimal(12,2) not null,
MontoMensualidad decimal(12,2) not null,
Estado char(1) not null)
GO

SELECT * FROM Reservaciones_Auditoria
GO

CREATE TABLE [Empleados_Auditoria](
AuditId int IDENTITY(1,1) not null PRIMARY KEY,
Accion varchar(25) not null,
FechaCambio datetime not null default getdate(),
ID int not null,
NombreCompleto varchar(150) not null,
Email varchar(50) not null,
Password varchar(255) null,
TipoUsuario int not null,
FechaRegistro datetime not null,
Estado char(1) not null)
GO

SELECT * FROM Empleados_Auditoria
GO

CREATE TABLE [Paquetes_Auditoria](
Accion varchar(25) not null,
FechaCambio datetime not null default getdate(),
ID int not null,
NombrePaquete varchar(150) not null,
Precio decimal(12,2) not null,
PorcentajePrima decimal(12,2) not null,
LimiteMeses int not null,
FechaRegistro datetime not null,
Estado char(1) not null)
GO

SELECT * FROM Paquetes_Auditoria
GO

CREATE TABLE [Clientes_Auditoria](
AuditId int IDENTITY(1,1) not null PRIMARY KEY,
Accion varchar(25) not null,
FechaCambio datetime not null default getdate(),
Cedula varchar(25) not null,
TipoCedula varchar(20) not null,
NombreCompleto varchar(150) not null,
Telefono varchar(15) not null,
Direccion varchar(150) not null,
Email varchar(150) not null,
Password varchar(255) null,
TipoUsuario int not null,
Restablecer int not null,
FechaRegistro datetime not null,
Estado char(1) not null)
GO

SELECT * FROM Clientes_Auditoria
GO


