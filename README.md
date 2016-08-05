##Object.Assign linq extension
www.nuget.org/packages/ObjectAssign/

Use this Linq extension to simplify object initialization when two classes have many common properties.
Plays nicely with Entity Framework queries

**Convert this:**

```c#
using(var C = new Model()) {
    var query = C.Clients.Select( x => new ClientDTO {
        Id = x.Id,
        FirstName = x.FirstName,
        SecondName = x.SecondName,
        Email = x.Email,
        Age = x.Age, 
        //... maybe more properties ...

        //Calculated properties:
        CanDrink = x.Age > 18,
        GroupName = x.Group.Name,
        FullName = x.FirstName + " " + x.SecondName
    }).ToList();
}
```

**To this:**

```c#
using (var C = new Model()) {
    var query = C.Clients.SelectClone(x => new ClientDTO {
        //Properties with the same name and type are automatically assigned

		CanDrink = x.Age > 18,
        GroupName = x.Group.Name,
        FullName = x.FirstName + " " + x.SecondName
    }).ToList();
}
```
