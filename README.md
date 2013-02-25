# EventPipe #

## Pre-requisites ##
To compile Netduino firmware
* Netduino on COM1 connected to PC via Serial plugin-configured COM port

To compile all server code
* MSBuild.exe src/EventPipe-Server-TrayApp\TrayApp.csproj

To use Lync server plugin:
* Lync 2010 client is running and signed in

To use RSS server plugin:
* Internet connection to websites over port 80

To use Serial port server plugin
* Serial port available on plugin-configured COM port

To use TFS server plugin:
* A TFS 2010 server with server-side work item queries prepared

To use CodeFlow server plugin:
* A CodeFlow server with ReviewService endpoint exposed; windows authentication

### Netduino Client ###
* .NET Micro 4.1 Runtime: http://www.netduino.com/downloads/MicroFrameworkSDK.msi
* Netduino SDK v4.1.0 (64-bit): http://www.netduino.com/downloads/netduinosdk_64bit.exe

## Adding server plugins ##
To remove add build dependency a new plugin project:
* Create server plugin folder in Server project
* Add plugin entry point class and plugin path configurations in server app.config
* Add output dll links + plugin.config (existing file as link) to server project plugin folder, with build mode set to content;copy if newer.
