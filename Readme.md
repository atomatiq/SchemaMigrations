## Make your Revit Extensible Storage API experience comfortable 

[![Nuget](https://img.shields.io/nuget/v/Nice3point.Revit.Toolkit?style=for-the-badge)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Downloads](https://img.shields.io/nuget/dt/Nice3point.Revit.Toolkit?style=for-the-badge)](https://www.nuget.org/packages/Nice3point.Revit.Toolkit)
[![Last Commit](https://img.shields.io/github/last-commit/Nice3point/RevitToolkit/develop?style=for-the-badge)](https://github.com/Nice3point/RevitToolkit/commits/develop)

This library provides tools for making Revit Extensible Storage API similar to Entity Framework.
Define you models, add them to SchemaContext. Run Schema Migration Generator to Create migration. Then save your models 
in ES and load them from ES as an instances of your Models class, not only a primitive objects.

## Installation
**TODO: Change references after publishing**

You can install Toolkit as a [nuget package](https://www.nuget.org/packages/Nice3point.Revit.Toolkit).

Packages are compiled for a specific version of Revit, to support different versions of libraries in one project, use RevitVersion property.

```text
<PackageReference Include="Nice3point.Revit.Toolkit" Version="$(RevitVersion).*"/>
```

Package included by default in [Revit Templates](https://github.com/Nice3point/RevitTemplates).

## Table of contents

* [How to get started](#howtogetstarted)
* [ExternalApplication](#externalapplication)
* [ExternalDBApplication](#externaldbapplication)
* [External events](#external-events)
  * [ActionEventHandler](#actioneventhandler)
  * [IdlingEventHandler](#idlingeventhandler)
  * [AsyncEventHandler](#asynceventhandler)
  * [AsyncEventHandler\<T>](#asynceventhandlert)
* [ExternalCommandAvailability](#externalcommandavailability)
* [Context](#context)
* [Options](#options)
  * [FamilyLoadOptions](#familyloadoptions)
  * [DuplicateTypeNamesHandler](#duplicatetypenameshandler)
  * [SaveSharedCoordinatesCallback](#savesharedcoordinatescallback)
  * [FrameworkElementCreator](#frameworkelementcreator)
  * [SelectionConfiguration](#selectionconfiguration)
* [Decorators](#decorators)
  * [DockablePaneProvider](#dockablepaneprovider)
* [Helpers](#helpers)
  * [ResolveHelper](#resolvehelper)
  * [Add-ins Dependency Isolation](#add-ins-dependency-isolation)
* [Samples](#samples)
  * [External application flow control](#external-application-flow-control)
  * [External command flow control](#external-command-flow-control)

## Features

### How to get started

Create a solution for your Revit Application. You can use Nice3Point [Revit Templates](https://github.com/Nice3point/RevitTemplates),
but it is not mandatory.

You should create one project for you solution which will be responsible for working with Extensible Storage (next: Database project).
Other project would have references on it if they need.

Add reference to nuget-package SchemaMigrations.Database in Database project. You should use version of package according your Revit Version,
or simply change your .csproj like this if you are using Nice3Point or similar template:
```xml
<PackageReference Include="SchemaMigrations.Database" Version="$(RevitVersion).*" />
```

1. Create Models folder and define your Model:

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

You can have maximum 256 properties in your model class. The supported property types are Boolean, Byte, Int16, Int32, Float, Double, ElementId, GUID, String, XYZ, UV and Entity.

2. Create an implementation of SchemaContext class and add a SchemaSet<T> for every model class:
```c#
public class ApplicationSchemaContext : SchemaContext
{
    public SchemaSet<Person> Persons { get; set; } = [];
}
```
3. Install SchemaMigrations.Generator tool globally:
```powershell
dotnet install SchemaMigrations.Generator --global
```
4. Open your terminal and make sure that you are in your Database project folder. Run 'cd Directory'
to go to Directory. Run 'cd ..' to go to parent directory.

5. Run the following command in terminal:
```powershell
AddSchemaMigration InitialMigration ${PWD}
```
It will create a Migrations folder and a first migration class called "InitialMigration_{datetime_stamp}":
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

Add reference to nuget-package SchemaMigrations.Database in Database project. You should use version of package according your Revit Version,
or simply change your .csproj like this if you are using Nice3Point or similar template:
```xml
<PackageReference Include="SchemaMigrations.Database" Version="$(RevitVersion).*" />
``` 

Also, you will need a reference to your database project.

For saving and loading data you always need an element. Considering you have one, right this code to save a Person object to it:
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

That's it! Now your element contains entity of Person schema with 6 fields. 
To load an object from this element, just use **LoadObject()**:
```c#
var connection = new DatabaseConnection<Person>(instance);
var existedPerson = connection.LoadObject(); // connection is generic DatabaseConnection<Person>, so it will return correct type
```

### Add changes to your schema

If you need 

Contains an implementation for **IExternalDBApplication**.

```c#
public class Application : ExternalDBApplication
{
    public override void OnStartup()
    {
    }

    public override void OnShutdown()
    {
    }
}
```

Override method **OnStartup()** to execute some tasks when Revit starts.

Override method **OnShutdown()** to execute some tasks when Revit shuts down. You don't have to override this method if you don't plan to use it.

**ExternalDBApplication** contains the logic for resolving dependencies.
Now you may not encounter a `FileNotFoundException`. Dependencies are searched in the plugin folder.

Starting with Revit 2025, **ExternalDBApplication** is executed in an isolated context, providing independent execution and preventing conflicts due to incompatible library
versions.

### External events

Contains an implementations for **IExternalEventHandler**.

It is used to modify the document from another thread, for example, when using modeless windows.
You can create your own handlers by deriving from this class.

#### ActionEventHandler

A handler that provides access to modify a Revit document outside the execution context with the ability to queue Raise method calls.

Calling a handler in a Revit context will call it immediately, without adding it to the queue.

```c#
public ViewModel
{
    ActionEventHandler = new ActionEventHandler();
}

public ActionEventHandler ActionEventHandler { get; }
public ElementId ElementId { get; set; }

private void DeteleElement()
{
    ActionEventHandler.Raise(application =>
    {
        var document = application.ActiveUIDocument.Document;
        using var transaction = new Transaction(document, $"Delete element");
        transaction.Start();
        document.Delete(ElementId)
        transaction.Commit();
        
        Debug.WriteLine("Deleted");
    });

    Debug.WriteLine("Command completed");
}
```

Debug output in a Revit context:

```text
Command completed
Deleted
```

Debug output outside the Revit context:

```text
Deleted
Command completed
```

#### IdlingEventHandler

With this handler, you can queue delegates for method calls when Revit becomes available again.
Unsubscribing from the Idling event occurs immediately.
Suitable for cases where you need to call code when Revit receives focus.
For example, to display a window after loading a family into a project.

```c#
public ViewModel
{
    IdlingEventHandler = new IdlingEventHandler();
}

public IdlingEventHandler IdlingEventHandler { get; }

private void NotifyOnIdling()
{
    IdlingEventHandler.Raise(application =>
    {
        var view = new FamilyBrowser();
        view.Show();
        
        Debug.WriteLine("Idling");
    });

    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Command completed
Idling
```

#### AsyncEventHandler

With this handler, you can wait for the external event to complete.
The **RaiseAsync** method will return to its previous context after executing the method encapsulated in the delegate.
Suitable for cases where you need to maintain the sequence of code execution.

Exceptions in the delegate will not be ignored and will be rethrown in the original synchronization context.

Calling the handler in a Revit context will call it immediately without adding it to the queue and awaiting with `await` keyword will not cause a context switch, 
and you can still call API requests in the main Revit thread.

```c#
public ViewModel
{
    AsyncEventHandler = new AsyncEventHandler();
}

public AsyncEventHandler AsyncEventHandler { get; }

private async Task DeleteDoorsAsync()
{
    await AsyncEventHandler.RaiseAsync(application =>
    {
        var doorIds = document.GetInstanceIds(BuiltInCategory.OST_Doors);
        document.Delete(doorIds);

        Debug.WriteLine("Doors deleted");
    });

    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Doors deleted
Command completed
```

#### AsyncEventHandler\<T>

With this handler, you can wait for the external event to complete with the return value from the method encapsulated in the delegate.
The **RaiseAsync** method will return to its previous context after executing.
Suitable for cases where you need to maintain the sequence of code execution.

Exceptions in the delegate will not be ignored and will be rethrown in the original synchronization context

Calling the handler in a Revit context will call it immediately without adding it to the queue and awaiting with `await` keyword will not cause a context switch,
and you can still call API requests in the main Revit thread.

```c#
public ViewModel
{
    AsyncEventHandler = new AsyncEventHandler<int>();
}

public AsyncEventHandler<int> AsyncEventHandler { get; }

private async Task GetWindowsCountAsync()
{
    var windowsCount = await AsyncEventHandler.RaiseAsync(application =>
    {
        var uiDocument = application.ActiveUIDocument;
        var elementIds = uiDocument.Document.GetInstanceIds(BuiltInCategory.OST_Windows);
        uiDocument.Selection.SetElementIds(elementIds);

        Debug.WriteLine("Windows selected");
        return elementIds.Count;
    });

    Debug.WriteLine($"Windows count {windowsCount}");
    Debug.WriteLine("Command completed");
}
```

Debug output:

```text
Windows selected
Windows count 17
Command completed
```

### ExternalCommandAvailability

Contains an implementation for **IExternalCommandAvailability**.

It provides the accessibility check for a Revit add-in External Command.

Starting with Revit 2025, **ExternalCommandAvailability** is executed in an isolated context, providing independent execution and preventing conflicts due to incompatible library versions.
If your implementation does not include dependencies, use the **IExternalCommandAvailability** interface to reduce memory allocation.

### Context

Interface to global information about an application environment.

It allows access to application-specific data, as well as up-calls for application-level operations such as dialog and failure handling.

List of available environment properties:

- Context.Application;
- Context.UiApplication;
- Context.ActiveDocument;
- Context.ActiveUiDocument;
- Context.ActiveView;
- Context.ActiveGraphicalView;
- Context.IsRevitInApiMode;

**Context** data can be accessed from any application execution location:

```C#
public void Execute()
{
    Context.ActiveDocument.Delete(elementId);
    Context.ActiveView = view;
}
```

If your application can run in a separate thread or use API requests in an asynchronous context, perform an **IsRevitInApiMode** check.

A direct API call should be used if Revit is currently within an API context, otherwise API calls should be handled by `IExternalEventHandler`:

```C#
public void Execute()
{
    if (Context.IsRevitInApiMode)
    {
        ModifyDocument();
    }
    else
    {
        externalEventHandler.Raise(application => ModifyDocument());
    }
}
```

**Context** provides access to global application handlers for dialog and failure management:

```C#
try
{
    Context.SuppressDialogs();
    Context.SuppressDialogs(resultCode: 2);
    Context.SuppressDialogs(args =>
    {
        var result = args.DialogId == "TaskDialog_ModelUpdater" ? TaskDialogResult.Ok : TaskDialogResult.Close;
        args.OverrideResult((int)result);
    });
    
    //User operations
    LoadFamilies();
}
finally
{
    Context.RestoreDialogs();
}
```

By default, Revit uses manual error resolution control with user interaction.
Context provides automatic resolution of all failures without notifying the user or interrupting the program.

By default, all errors are handled for successful completion of the transaction.
However, if you want to cancel the transaction and undo all failed changes, pass false as the parameter:

```C#
try
{
    Context.SuppressFailures();
    Context.SuppressFailures(resolveErrors: false);
    
    //User transactions
    ModifyDocument();
}
finally
{
    Context.RestoreFailures();
}
```

### Options

The Toolkit provides implementation of various Revit interfaces, with the possibility of customization.

#### FamilyLoadOptions

Contains an implementation for **IFamilyLoadOptions**.
Provides a handler for loading families

```c#
document.LoadFamily(fileName, new FamilyLoadOptions(), out var family);
document.LoadFamily(fileName, new FamilyLoadOptions(false, FamilySource.Project), out var family);
document.LoadFamily(fileName, UIDocument.GetRevitUIFamilyLoadOptions(), out var family);
```

#### DuplicateTypeNamesHandler

Contains an implementation for **IDuplicateTypeNamesHandler**.
Provides a handler of duplicate type names encountered during a paste operation.

```c#
var options = new CopyPasteOptions();
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler());
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(args => DuplicateTypeAction.Abort));
options.SetDuplicateTypeNamesHandler(new DuplicateTypeNamesHandler(DuplicateTypeAction.UseDestinationTypes));
ElementTransformUtils.CopyElements(source, elementIds, destination, null, options);
```

#### SaveSharedCoordinatesCallback

Contains an implementation for **ISaveSharedCoordinatesCallback**.
Provides a handler for control Revit when trying to unload or reload a Revit link with changes in shared coordinates.

```c#
var linkType = elementId.ToElement<RevitLinkType>(Context.ActiveDocument);
linkType.Unload(new SaveSharedCoordinatesCallback());
linkType.Unload(new SaveSharedCoordinatesCallback(SaveModifiedLinksOptions.DoNotSaveLinks));
linkType.Unload(new SaveSharedCoordinatesCallback(type =>
{
    if (type.AttachmentType == AttachmentType.Overlay) return SaveModifiedLinksOptions.SaveLinks;
    return SaveModifiedLinksOptions.DoNotSaveLinks;
}));
```

#### FrameworkElementCreator

Contains an implementation for **IFrameworkElementCreator**.
Creator of `FrameworkElements` for the dockable pane.

```c#
DockablePaneProvider.Register(application, guid, title)
    .SetConfiguration(data =>
    {
        data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>();
        data.FrameworkElementCreator = new FrameworkElementCreator<DockPaneView>(serviceProvider);
    });
```

#### SelectionConfiguration

Contains an implementation for **ISelectionFilter**.
Creates a configuration for creating Selection Filters.

By default, all elements are allowed for selection:

```c#
var selectionConfiguration = new SelectionConfiguration();
uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

You can also customize the selection of Element or Reference separately:

```c#
var selectionConfiguration = new SelectionConfiguration()
        .Allow.Element(element => element.Category.Id.AreEquals(BuiltInCategory.OST_Walls));

uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

Or set rules for everything:

```c#
var selectionConfiguration = new SelectionConfiguration()
    .Allow.Element(element => element.Category.Id.AreEquals(BuiltInCategory.OST_Walls))
    .Allow.Reference((reference, xyz) => false);

uiDocument.Selection.PickObject(ObjectType.Element, selectionConfiguration.Filter);
```

### Decorators

Simplified implementation of raw Revit classes

#### DockablePaneProvider

Provides access to create a new dockable pane to the Revit user interface.

```c#
DockablePaneProvider
    .Register(application, new Guid(), "Dockable pane")
    .SetConfiguration(data =>
    {
        data.FrameworkElement = new RevitAddInView();
        data.InitialState = new DockablePaneState
        {
            MinimumWidth = 300,
            MinimumHeight = 400,
            DockPosition = DockPosition.Right
        };
    });
```

### Helpers

Provides auxiliary components

#### ResolveHelper

Provides handlers to resolve dependencies for Revit 2024 and older.

```c#
try
{
    ResolveHelper.BeginAssemblyResolve<DockView>();
    window.Show();
}
finally
{
    ResolveHelper.EndAssemblyResolve();
}
```

Enabled by default for `ExternalCommand`, `ExternalApplication` and `ExternalDBApplication`.

#### Add-ins Dependency Isolation

Provides dependency isolation for Revit 2025 and earlier.

This library enables running plugins in an isolated context using
.NET [AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext).
Each plugin executes independently, preventing conflicts from incompatible library versions.

How It Works:

The core functionality centers on `AssemblyLoadContext`, which creates an isolated container for each plugin.
When a plugin is loaded, it is assigned a unique `AssemblyLoadContext` instance, encapsulating the plugin and its dependencies to prevent interference with other plugins or the
main application.

To use this isolation feature, developers must inherit their classes from:

- ExternalCommand
- ExternalApplication
- ExternalDbApplication
- ExternalCommandAvailability

These classes contain the built-in isolation mechanism under the hood.
Plugins using interfaces such as `IExternalCommand` will not benefit from this isolation and will run in the default context.

Limitations:

- The isolated context feature is available starting with Revit 2025.
- For older Revit versions, this library uses a `ResolveHelper` to help load dependencies from the plugin's folder, but does not protect against conflicts arising from incompatible
  packages.
- Additionally, plugins that do not inherit from the specified classes will not be isolated and may experience compatibility issues if they rely on the default context.

### Samples

#### External application flow control

Adding a button to the Revit ribbon based on the username. 
`IExternalApplication` does not provide access to `Application`, but you can use the **Context** class to access the environment data to get the username:

```c#                                                                                
public class Application : ExternalApplication                                       
{                                                                                    
    public override void OnStartup()                                                 
    {                                                                                
        var panel = Application.CreatePanel("Commands", "RevitAddin");
        panel.AddPushButton<Command>("Execute");
        
        var userName = Context.Application.Username;                                 
        if (userName == "Administrator")                                                
        {                                                                            
            var panel = Application.CreatePanel("Secret Panel", "RevitAddin");
            panel.AddPushButton<Command>("Execute");
        }                                                                            
    }       
}                                                                                    
```   

Suppression of OnShutdown call in case of unsuccessful plugin startup:

```c#                                                                                
public class Application : ExternalApplication                                       
{         
    private ApplicationHosting _applicationHosting;
    
    public override void OnStartup()                                                 
    {        
        var isValid = LicenseManager.Validate();
        if (!isValid)                                                
        {     
            //If Result is overridden as Result.Failed, the OnShutdown() method will not be called
            Result = Result.Failed;                                                  
            return;                                                                  
        }
        
        //Running the plugin environment in case of successful license verification
        _applicationHosting = ApplicationHosting.Run();
    }       
    
    public override void OnShutdown()
    {
        //These methods will not be called if the license check fails on startup
        _applicationHosting.SaveData();
        _applicationHosting.Shutdown();
    }
}                                                                                    
```          

#### External command flow control

Automatic transaction management without displaying additional dialogs to the user in case of an error.
Can be used to use Modal windows when errors and dialogs change modal mode to modeless:

```c#
[Transaction(TransactionMode.Manual)]
public class Command : ExternalCommand
{
    public override void Execute()
    {
        //Suppresses all possible warnings and errors during command execution
        Context.SuppressDialogs();
        Context.SuppressFailures();
        
        try
        {
            //Action
            var selectedIds = UiDocument.Selection.GetElementIds();
            
            using var transaction = new Transaction(Context.ActiveDocument);
            transaction.Start("Delete elements");
            Document.Delete(selectedIds);
            transaction.Commit();
        }
        finally
        {
            //Restore normal application error and dialogs handling when exiting an external command            
            Context.RestoreDialogs();
            Context.RestoreFailures();
        }
    }
}
```

Redirecting errors to the revit dialog box, highlighting unsuccessfully deleted elements in the model:

```c#
[Transaction(TransactionMode.Manual)]
public class Command : ExternalCommand
{
    public override void Execute()
    {
        var selectedIds = UiDocument.Selection.GetElementIds();
        
        try
        {
            //Action
            using var transaction = new Transaction(Context.ActiveDocument);
            transaction.Start("Delete elements");
            Document.Delete(selectedIds);
            transaction.Commit();
        }
        catch
        {
            //Redirecting errors to the Revit dialog with elements highlighting
            Result = Result.Failed;
            ErrorMessage = "Unable to delete selected elements";
            foreach (var selectedId in selectedIds)
            {
                ElementSet.Insert(selectedId.ToElement(Document));
            }
        }
    }
}
```