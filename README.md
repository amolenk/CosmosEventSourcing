# Event Sourcing with Azure Cosmos DB

This repository contains an example of an Event Store built on Azure Cosmos DB.

It contains all demo code that I use in my Cosmos Event Sourcing talk. See here for the slidedeck.

Demo on YouTube: https://youtu.be/UejwRlmV6E4

The mssql branch contains code to have views written to MSSQL (contributed by marctalcott, thanks!)

# MSSQL Views
## Added the ability to have views written to MSSQL. 
(see the Readme.txt at https://github.com/marctalcott/CosmosEventSourcing/blob/main/src/Projections/MSSQL/Readme.DbScript.txt



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

# Migrator
## Added the Migrator project
Have you ever wanted to change some of your events that were recorded in the event log. There are times this is valid in event sourcing. In my case we had a project where we got the Ubiquitous Language wrong.  We had fundamental aggregate, which one part of the business used a particularly odd name for. Since this was the only part of the business we were exposed to at that point, we used their terminology, but later found out that it was not the most common name to be used by other parts of the business, nor did it make much sense to the dev team. So we wanted to change the name of an Aggregate (and therefore its events). If that was all, I might suggest you avoid re-writing the event history, but on top of that, this is such a core piece that nearly every other aggregate maintains an Id for the item. So migrating the code would involve touching almost every Aggregate, Event, View, and more. It also would mean maintaining logic to handle the old events by the first name, and the new events with the new name. And every aggregate would have to know how to apply both sets of events. And most views would have to be updated to handle both types of events. And new views were needed to reflect the new naming.

The cleaner option was to create a new set of events and aggregates and act as if we had named it correctly from the beginning (rather than having code to handle both types of events going forward).

Hence the Migrator. 

To use the Migrator, you create a new set of events (I created Monitor events which will replace Meter events in this sample code). Run the Migration engine using the DemoScenariosForMigrator. And then run the individual tests within that same file. As Meter events are recorded you will see them get transformed into Monitor events and written into a separate container. Events that are not specifically transformed are also moved to the new container without making any changes to them.

You will still need to write your new events, but you can also basically refactor the rest of your code to use the new naming after the migration is done and you won't need to work with the old event names and the new events names simultaneously.


