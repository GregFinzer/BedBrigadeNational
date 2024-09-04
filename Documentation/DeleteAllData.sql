--This deletes all data except for the Locations and MetroAreas
delete from BedRequests
go
delete from Configurations
go
delete from ContactUs
go
delete from Content
go
delete from Donations
go
delete from EmailQueue
go
delete from Media
go
delete from Roles
go
delete from Schedules
go
delete from Templates
go
delete from Users
go
delete from SignUps
go
delete from Volunteers
go
delete from Locations where LocationId > 3
go
delete from UserPersist
go