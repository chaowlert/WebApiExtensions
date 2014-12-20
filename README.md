WebApiExtension
===============

Extension for Asp.net webapi 2

### Usage
####1. Extend model binding
This will allow binding multiple complex models on request

**Configuration**  
```
GlobalConfiguration.Configuration.ExtendModelBinding()
```

**Example**  
For this method
```
public string Get(Type1 type1, Type2 type2) { ... }
```
This is how to send request from client  
>/path?type1[prop1]=a&type1[prop2]=b&type2[prop3]=c&type2[prop4]=d

For javascript, you can do something like
```
$.get('/path', {
    type1: { prop1: 'a', prop2: 'b' },
    type2: { prop3: 'c', prop4: 'd' }
});
```
**Note**  
- Parameters can be sent through either query string or content
- You can make parameter optional by add default value to method arguments
- Types are resolved by Json.net, so you can use `GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings` to config
- For file upload, type byte[] will return content byte array, and type MediaTypeHeaderValue will return media header information

---
####2. Extend XML formatter
This will use Json.net for xml formatting

**Configuration**  
```
GlobalConfiguration.Configuration.ExtendXmlFormatter()
```
**note**  
- If you use `ExtendedModelBinding` above, XmlFormatter is not supported. You can use this `ExtendXmlFormatter`
- This formatter use Json.net to resolve types, so this will be better than XmlFormatter, since it will support JObject, Dictionary, dynamic, and more

---
####3. Support Graph Controller
This will simplify api path to map with code structure. Api path concept will be similar to Facebook graph API.

**Configuration**  
```
GlobalConfiguration.Configuration.SupportGraphController()
```

**Example**  
For Controllers/UsersController.cs file
```
[HttpGet]
public User[] items() { .. }

[HttpGet]
public User item(int id) { .. }

[HttpPost]
public void broadcast() { .. }

[HttpGet]
public Photo[] photos(int id) { .. }
```
'items' method will map to `/users`, this is suitable with get all users  
'item' with id will map to `/users/id`, this is suitable with get or post to single user  
other method name (broadcast) will map to `/users/broadcast`, this is suitable with operation to all users  
other method name (photos) with id will map to `/users/id/photos`, this is suitable with getting specific user's collections

You can also easily add area to group controllers. Such as /Controllers/Admin/UsersController.cs will map to `/admin/users`

---
####4. Support JsonP
This will allow web api to support JsonP

**Configuration**  
```
GlobalConfiguration.Configuration.SupportGraphController()
```
**Example**  
Just add callback to query string
>/path?callback=xxx

---
####5. Support Action Injection
This will allow to inject services into actions

**Configuration**  
```
GlobalConfiguration.Configuration.SupportActionInjection()
```

**Example**  
```
public int calculate(int a, int b, ICalculator cal) { .. }
```
`ICalculator` will automatically inject to method
