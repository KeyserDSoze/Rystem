### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Go online command
In the commit you can set up this message

### Debug
```
goownv=1 Package=0 Versioning=Patch IsAutomatic=true MinutesToWait=1 AddingNumberToCurrentVersion=1 SpecificVersion=null IsDebug=true
```

### Release
```
goownv=1 Package=0 Versioning=Patch IsAutomatic=true MinutesToWait=5 AddingNumberToCurrentVersion=1 SpecificVersion=null IsDebug=false
```

goownv: 1 or 0 is the command to go online with new version.

Package, Versioning, IsAutomatic, MinutesToWait, AddingNumberToCurrentVersion, SpecificVersion: are the parameters to set up the new version.

IsDebug, if true: the command will not go online, just show the new version.