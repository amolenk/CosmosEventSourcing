 # Create a SQL database with a table that meets the criteria for your reporting (like example below).
 # In this example I want to table that shows the current state of the Meter Aggregate.
 
 # Add table definition here
 
 ---
 create table Meter
 (
     MeterId                   nvarchar(255) not null primary key,
     PostalCode                nvarchar(255),
     HouseNumber               nvarchar(255),
     IsActivated               bit,
     FailedActivationAttempts  int,
     LatestReadingDate         datetime,
     
     -- The next 2 lines should be on all tables. They allow code to decide if a change has been applied yet.
     LogicalCheckPoint_lsn     float         not null,
     LogicalCheckPoint_ItemIds nvarchar(max) not null
 )
 go
 
 create unique index Meter_MeterId_uindex
     on Meter (MeterId)
 go
 
---
 
 # Once the table is created you can run this script in your 'Projections' project root folder, and it will create your
 # SQL EF code for you.
 
 

 dotnet ef dbcontext scaffold "server=myServer;database=myDb;user=myUser;password=myPassword;" "Microsoft.EntityFrameworkCore.SqlServer" -v -f --schema dbo -o MSSQL/DbSchema
