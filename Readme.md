## Make your Revit Extensible Storage API experience comfortable 

[![Nuget](https://img.shields.io/nuget/v/Atomatiq.SchemaMigrations.Database?style=for-the-badge)](https://www.nuget.org/packages/Atomatiq.SchemaMigrations.Database)
[![Downloads](https://img.shields.io/nuget/dt/Atomatiq.SchemaMigrations.Database?style=for-the-badge)](https://www.nuget.org/packages/Atomatiq.SchemaMigrations.Database)
[![Last Commit](https://img.shields.io/github/last-commit/atomatiq/SchemaMigrations/develop?style=for-the-badge)](https://github.com/atomatiq/SchemaMigrations/commits/develop)

This library provides tools for making Revit **Extensible Storage** API similar to Entity Framework.
Define your models, add them to **SchemaContext**.
Run **Schema Migration Generator** to create migration.
Then save your models 
in ES and load them from ES as instances of your Models class, not only primitive objects.

## Installation
You can install this tool as a [nuget package](https://www.nuget.org/packages/Atomatiq.SchemaMigrations.Database).

Packages are compiled for a specific version of Revit, to support different versions of libraries in one project, use RevitVersion property.

```text
<PackageReference Include="Atomatiq.SchemaMigrations.Database" Version="$(RevitVersion).*"/>
```

## Table of contents

* [How to get started](#how-to-get-started)
* [Save data to schema and load data from schema](#Save-data-to-schema-and-load-data-from-schema)
* [Add changes to your schema](#Add-changes-to-your-schema)

## Features

### How to get started

Create a solution for your Revit Application. You can use Nice3Point [Revit Templates](https://github.com/Nice3point/RevitTemplates),
but it is not mandatory.

You should create one project for your solution which will be responsible for working with Extensible Storage (next: **Database project**).
Other projects would have references on it if they needed.

Add reference to nuget-package SchemaMigrations.Database in the **Database project**.
You should use the version of the package according to your Revit Version,
or simply change your .csproj like this if you are using Nice3Point or similar template:
```xml
<PackageReference Include="Atomatiq.SchemaMigrations.Database" Version="$(RevitVersion).*" />
```

1. Create Models folder and define your **Model**:

```c#
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Occupation { get; set; }
    public List<string> Hobbies { get; set; } = [];
    public Dictionary<string, int> Scores { get; set; } = [];
}
```

You can have a maximum of 256 properties in your model class. The supported property types are Boolean, Byte, Int16, Int32, Float, Double, ElementId, GUID, String, XYZ, UV and Entity.

2. Create an implementation of **SchemaContext** class and add a **SchemaSet<T>** for every model class:
```c#
public class ApplicationSchemaContext : SchemaContext
{
    public SchemaSet<Person> Persons { get; set; } = [];
}
```
3. Install **SchemaMigrations.Generator** tool globally:
```powershell
dotnet tool install Atomatiq.SchemaMigrations.Generator --global
```
4. Open your terminal and make sure that you are in your Database project folder. Run `cd Directory`
to go to Directory. Run `cd ..` to go to parent directory.

5. Run the following command in terminal:
```powershell
schema-migration-add InitialMigration
```
This command will build your solution to affect all the changes and after that start adding a migration.
If you do not want to build, add `--no-build` flag:
```powershell
schema-migration-add InitialMigration --no-build
```
The command will create a Migrations folder (if needed) and a first migration class called `InitialMigration_{datetime_stamp}`:
```c#
using SchemaMigrations.Abstractions;
using SchemaMigrations.Database.Schemas;
using SchemaMigrations.Abstractions.Models;

namespace SchemaMigrationsExample.EntityCreator.Database.Migrations;
public class InitialMigration_20241010_0301 : Migration
{
    public override Dictionary<string, Guid> GuidDictionary { get; } = new Dictionary<string, Guid>()
    {
        { "Persons", new Guid("5c047012-e225-4c9f-b02a-dc7f2aebca5d") },
    };

    public override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddSchemaData(new SchemaBuilderData()
        {
            Guid = GuidDictionary["Persons"],
            Documentation = "Initial schema for Person",
            Name = "Persons",
            VendorId = "Atomatiq"
        },
        new SchemaDescriptor("Persons")
        {
            Fields = new List<FieldDescriptor>()
            {
                new FieldDescriptor( "Id", typeof(Int32) ),
                new FieldDescriptor( "Name", typeof(String) ),
                new FieldDescriptor( "Surname", typeof(String) ),
                new FieldDescriptor( "Occupation", typeof(String) ),
                new FieldDescriptor( "Hobbies", typeof(List<String>) ),
                new FieldDescriptor( "Scores", typeof(Dictionary<String, Int32>) )
            }
        });

    }    
}
```

Now you are all set to create your first schema.

### Save data to schema and load data from schema

For saving and loading data, you always need an element. Considering you have one, right this code to save a **Person** object to it:
```c#
using var transaction = new Transaction(instance.Document, "Seeding database");
transaction.Start();

var connection = new DatabaseConnection<Person>(instance);
var person = new Person
{
    Id = 69,
    Name = "Mirko",
    Surname = "Petrović",
    Occupation = "Software development",
    Scores = new Dictionary<string, int>
    {
        { "Scores", 1 }
    },
    Hobbies = ["Sports"]
};
connection.SaveObject(person);

transaction.Commit();
```

That's it! Now your element contains entity of **Person** schema with six fields. 
To load an object from this element, use **LoadObject()**:
```c#
var connection = new DatabaseConnection<Person>(instance);
var existedPerson = connection.LoadObject(); // connection is generic DatabaseConnection<Person>, so it will return correct type
```

### Add changes to your schema

If you need to add more objects to your schema context, it is not an issue anymore. 
Add new **SchemaSet** to your **SchemaContext**, or modify your models as you need.
**SchemaMigrations.Generator** will create new migrations, and **SchemaMigrations.Database** package will create a new schema
according to new migrations, move all the existing data from old schema to the new one for all the elements, and then delete all the entities of old schema.

#### How does it work?
Let's add a new Model class, **Instrument**, and **SchemaSet** of it to our **SchemaContext**, and modify the **Person** class: remove the occupation and add the height.
```c#
public class Instrument
{
    public string Name { get; set; }
    public string Type { get; set; }
}
```
```c#
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    //public string Occupation { get; set; }
    public double Height { get; set; }
    public List<string> Hobbies { get; set; } = [];
    public Dictionary<string, int> Scores { get; set; } = [];
}
```
```c#
public class ApplicationSchemaContext : SchemaContext
{
    public SchemaSet<Person> Persons { get; set; } = [];
    public SchemaSet<Instrument> Instruments { get; set; } = [];
}
```

Then build a project and add new migration.

Go to your **Database** project folder in the terminal and run the same command:
```powershell
AddSchemaMigration SecondMigration ${PWD}
```
It will generate a new migration:
```c#
public class SecondMigraion_20241011_1122 : Migration
{
    public override Dictionary<string, Guid> GuidDictionary { get; } = new Dictionary<string, Guid>()
    {
        { "Persons", new Guid("7fccc4a5-2166-4973-82f4-8c4ed5ce8ef8") },
        { "Instruments", new Guid("5b657730-5315-469c-8b6c-9c8799c51320") },
    };

    public override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateGuid("Persons", GuidDictionary["Persons"]);
        migrationBuilder.AddColumn("Persons", "Height", typeof(Double));
        migrationBuilder.DropColumn("Persons", "Occupation");
        migrationBuilder.AddSchemaData(new SchemaBuilderData()
        {
            Guid = GuidDictionary["Instruments"],
            Documentation = "Initial schema for Instrument",
            Name = "Instruments",
            VendorId = "Atomatiq"
        },
        new SchemaDescriptor("Instruments")
        {
            Fields = new List<FieldDescriptor>()
            {
                new FieldDescriptor( "Name", typeof(String) ),
                new FieldDescriptor( "Type", typeof(String) )
            }
        });

    }    
}
```
It generated new guid values, **Add** and **Drop Columns** commands, and added a new **Schema**.
Great! 
Now you can create database connection of **Person** and of **Instrument** as in the previous chapter

#### Supported commands

Migration generator now supports adding new schemas, adding columns to schemas and deleting columns from schemas
It cannot change the type of property or delete existing schema now
