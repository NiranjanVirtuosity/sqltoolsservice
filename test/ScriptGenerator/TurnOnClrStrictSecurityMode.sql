USE [master]
GO

EXEC sp_configure 'clr strict security', 1; RECONFIGURE;
go
Exec sp_configure 'show advanced option', '0'; RECONFIGURE;
go