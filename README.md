# EventPipe #

## Pre-requisites ##
To fully load and compile all solution projects:
* Lync 2010 client is running and signed in
* Netduino on COM1 connected to PC via Serial plugin-configured COM port

### Server ###
* Lync 2010 SDK (found in enlistment): www.microsoft.com/en-us/download/details.aspx?id=18898

### Client ###
* .NET Micro 4.1 Runtime: http://www.netduino.com/downloads/MicroFrameworkSDK.msi
* Netduino SDK v4.1.0 (64-bit): http://www.netduino.com/downloads/netduinosdk_64bit.exe

## Adding/removing server plugins ##
To remove dependencies on building a particular plugin project:
* Remove plugin configuration from server app.config
* Remove copy dll links from server project plugin folder
* Unload project (optional, if you cannot build)