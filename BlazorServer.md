# Bed Brigade Server

The app has been converted from Blazor Client Side to Blazor Server.

At present, it is not running in Prerendered mode, but Server mode as we are using JsInterop. As soon as I can change to existing codes to work with Prerendering we'll switch.

The only project dependancy is Client and Server to Shared. 

**Client Structure**

* Components is where all client based components will live
* Data 
* Services - Components inject services and services handle the work, e.g. Call the API Server.
* Pages - will use a qualifiing folder, e.g. Administration, Administration/Manage  


**Server Structure**

* Controllers - Client Services will call into the controller. Controllers will use a Server Service which will responed with a ServiceResponse.
* Services - Handles calls from a controller return a unit of work with a ServerResponse obj.


These are the minimum requirements. As additional dev requirements evole please update this document.