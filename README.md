##Object.Assign linq extension


Use this Linq extension to simplify the object initialization when two objects have many common properties

Suppose that you have the following classes:
```
class GroupEntity { 
    public int Id { get; set; }
    public string Name { get; set; }
}

class ClientEntity {
    public int Id { get; set; }
    public string FirstName { get; set;}
    public string SecondName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    //Other entity properties...
    
}

class ClientDTO { 
    //Entity properties:
    public int Id { get; set;}
    public string FirstName { get; set;}
    public string SecondName { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    //Other entity properties...

    //Calculated properties:
    public bool CanDrink { get; set; }
    public string GroupName { get; set;}
    public string FullName { get; set;}
}
```

If we wanted to query the database to retrive directly `ClientDTO` objects the query would be something like:

```
using(var C = new Model()) {
    var query = C.Clients.Select( x => new ClientDTO {
        //Cloned properties:
        Id = x.Id,
        FirstName = x.FirstName,
        SecondName = x.SecondName,
        Email = x.Email,
        Age = x.Age, 
        //... maybe more cloned properties ...

        //Calculated properties:
        CanDrink = x.Age > 18,
        GroupName = x.Group.Name,
        FullName = x.FirstName + " " + x.SecondName
    }).ToList();
}
```

With the `Object.Assign` Linq extension we can assign only properties that are not automatically mapped by name and type:
```
using (var C = new Model()) {
    var query = C.Clients.SelectClone(x => new ClientDTO {
        //Properties with the same name and type are automatically assigned

		CanDrink = x.Age > 18,
        GroupName = x.Group.Name,
        FullName = x.FirstName + " " + x.SecondName
    }).ToList();
}
```