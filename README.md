# Event Sourcing with Azure Cosmos DB

This repository contains an example of an Event Store built on Azure Cosmos DB.

It contains all demo code that I use in my Cosmos Event Sourcing talk. See [here](https://github.com/amolenk/CosmosEventSourcing/blob/master/Event%20Sourcing%20with%20Azure%20Cosmos%20DB.pdf) for the slidedeck.

Demo on YouTupe: https://youtu.be/UejwRlmV6E4

# mssql branch notes
On 8/15/2021, Added the ability to have views written to MSSQL. 

  In this example I want a MSSQL
  table that shows the current state of the 
  Meter Aggregate. This is helpful in scenarios where
  you need to run quick ad hoc queries to view the 
  current state.
  
 You may want this instead of Cosmos views, or 
in addition to Cosmos views. For the example 
we keep the Cosmos views and have this as an additional
way of viewing your current state.


 ### 1) Create a SQL database
 ### 2) Run this script to create your Meter views table.
 
 ---
<pre>
<code>
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
 </code>
</pre>
---
 ### 3) Update the connection string in your project to point to your db
 You will find it in Projections/MSSQL/DBSchema/esdemo3Context.cs 
 
 ### Notes) 
  This Project already includes Projections/MSSQL/DBSchema/esdemo3Context.cs 
  and Projections/MSSQL/DBSchema/Meter.cs
 
  Once the table is created you can run this script in your 'Projections' project root folder, 
  and it will create your SQL EF code for you.
 
  You need to go to  Projections/MSSQL/DBSchema/esdemo3Context.cs  and set the connection string
 to match the database you created at Azure

 If you alter the table and you want to udpdate your .Net models use this with your correct connection string
 You should run it from the terminal in the Projections folder location
 dotnet ef dbcontext scaffold "server=myServer;database=myDb;user=myUser;password=myPassword;" "Microsoft.EntityFrameworkCore.SqlServer" -v -f --schema dbo -o MSSQL/DbSchema

