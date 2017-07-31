# Cake.WindowsAppStore - An Addin for Cake

![Cake.WindowsAppStore](https://raw.githubusercontent.com/cake-contrib/Cake.WindowsAppStore/develop/Cake.WindowsAppStore.png)

[![AppVeyor master branch](https://img.shields.io/appveyor/ci/cakecontrib/cake-windowsappstore.svg)](https://ci.appveyor.com/project/cakecontrib/cake-windowsappstore)
[![nuget pre release](https://img.shields.io/nuget/vpre/Cake.WindowsAppStore.svg)](https://www.nuget.org/packages/Cake.WindowsAppStore)


Cake.WindowsAppStore allows you to upload an app package to the Windows App Store with just two lines of code. This addin automates the creation of submissions in
the Windows Store, as explained in the [official documentation](https://docs.microsoft.com/en-us/windows/uwp/monetize/csharp-code-examples-for-the-windows-store-submission-api).

## One time set up

To use automation to the Windows App Store, it's required to associate an [Azure AD application with your Windows Dev Center account](https://docs.microsoft.com/en-us/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services#how-to-associate-an-azure-ad-application-with-your-windows-dev-center-account). 
After doing this, you should have the following 3 values:

- Client ID
- Client Secret
- Tenant ID

## Usage in Cake

In order to use the exposed commands you have to add the following line at top of your build.cake file:

```cake
#addin Cake.WindowsAppStore
```

### CreateWindowsStoreAppSubmission

To create a submission, use the code below:

```cake
CreateWindowsStoreAppSubmission("./output/myApp.appxupload", new WindowsStoreAppSubmissionSettings
{
    ApplicationId = "my app id"
});
```

If you don't have the secrets stored in environment variables, use the code below:

```cake
CreateWindowsStoreAppSubmission("./output/myApp.appxupload", new WindowsStoreAppSubmissionSettings
{
    ApplicationId = "my app id",
	ClientId = "<client_id>",
    ClientSecret = "<client_secret>",
	TenantId = "<tenant_id>"
});
```

That's all!

> Note that this addin will only create the submission in the Windows Store. You will still need to actually **verify and publish your app manually**.

> Note that this addin does not (yet) support release notes yet since this requires release notes per listing (language)

> Don't forget to set your api token from Windows App Store as environment variables on your local machine or CI system:
> -`WINDOWSAPPSTORE_CLIENT_ID`
> -`WINDOWSAPPSTORE_CLIENT_SECRET`
> -`WINDOWSAPPSTORE_TENANT_ID`

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
