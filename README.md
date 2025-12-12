Build Server
dotnet publish RCS.Server -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "RCS_Deploy/Server"

Build Client
dotnet publish RCS.Agent -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "RCS_Deploy/Agent"