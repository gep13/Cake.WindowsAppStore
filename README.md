# Cake.WindowsAppStore - An Addin for Cake

![Cake.WindowsAppStore](https://raw.githubusercontent.com/cake-contrib/Cake.WindowsAppStore/develop/Cake.WindowsAppStore.png)

[![AppVeyor master branch](https://img.shields.io/appveyor/ci/cakecontrib/cake-windowsappstore.svg)](https://ci.appveyor.com/project/cakecontrib/cake-windowsappstore)
[![nuget pre release](https://img.shields.io/nuget/vpre/Cake.WindowsAppStore.svg)](https://www.nuget.org/packages/Cake.WindowsAppStore)


Cake.WindowsAppStore allows you to upload an app package to the Windows App Store with just two lines of code. In order to use the exposed
commands you have to add the following line at top of your build.cake file:

```cake
#addin Cake.WindowsAppStore
```

Then you can upload your package to WindowsAppStore:

```cake
UploadToWindowsAppStore("./output/myApp.appxupload");
```

That's all!

> Note that this addin will only create the submission in the Windows Store. You will still need to actually **verify and publish your app manually**.

> Don't forget to set your api token from Windows App Store as environment variable: `WINDOWSAPPSTORE_API_TOKEN` on your local machine or CI system.

----

## More Examples

### Upload an app to the Windows Store

```cake
Task("Upload-To-Windows-Store")
    .IsDependentOn("Build")
    .Does(() => 
{
    UploadToWindowsAppStore("./output/myApp.appxupload"));
};
```

### Upload an app to the Windows Store with result.

```cake
Task("Upload-To-Windows-Store")
    .IsDependentOn("Build")
    .Does(() =>
{
    var result = UploadToWindowsAppStore("./output/myApp.appxupload"));
    // Use result.PublicUrl to inform others where they can download the newly uploaded package.
}
```

> **REMEMBER** For all request you make you either have to set your API token from Windows App Store as environment variable: `WINDOWSAPPSTORE_API_TOKEN`
> or pass it into the call via <see cref="WindowsAppStoreUploadSettings.AppId" />

----

## Build

To build this package we are using Cake.

On Windows PowerShell run:

```powershell
./build
```

On OSX/Linux run:
```bash
./build.sh
```
