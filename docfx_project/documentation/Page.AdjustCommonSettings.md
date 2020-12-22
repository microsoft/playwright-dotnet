# How to adjust some common settings, including set BrowsersPath and DriverExecutablePath but not limited
_Contributors: [Scott Huang](https://github.com/ScottHuangZL)_

## Problem

It is not easy or not convenient to let end user to: 

    install nodejs firstly 
    
    and then run "playwright-cli install", 
    
    and then wait long time(if networking not good or have "great wall") to download browser before they can run program

You want make your exe portable, and put the browsers & drivers as subfolder into your main exe folder and ship to end user directly.

So, you want to manual set BrowsersPath and DriverExecutablePath. 

You want several programs both portable and share same customized browser/drivers to save disk space 

You want add web credential to pass authentication silently, or set web proxy to fast internet access, or adjust timeout settings and so on

You want published as single exe file format

## Solution

You can put the custom path parameter in your appseetings.json and read it when start program
```cs
_browsersPath = _config.GetValue<string>("BrowsersPath");
_driverExecutablePath = _config.GetValue<string>("DriverExecutablePath");
```
Or may be just use fixed strings for subfoders
```cs
_browsersPath = "ms-playwright";
_driverExecutablePath = "./playwright-cli.exe"; 
```

Note, take win-x64 as example, the browser ususaly default installed in C:\Users\your_name\AppData\Local\ms-playwright

You can copy that folder to your main published exe subfolder. 

The folder "ms-playwright" usually contain chromium/firefox/webkit together, you can delete some browsers folders after copy in case you no need it to save disk space.


I usually publish .netcore console program with command:
```
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
//After build, vs studio compiler will auto generate the playwright-cli.exe in publish folder, at least for version 0.170.1
```
Or you can click your project to open yourproject.csproj file to set as below
```
 <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishTrimmed>true</PublishTrimmed>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
```

The published folder structure would be like below:
```
publish
    yourprogram.exe  //the published single exe file
    playwright-cli.exe  //the driver
    //...some other dlls, but not much since you publish with single file format...
    ms-playwright  //the browser folder
        chromium-833159 //I only keep chromium and delete firefox/webkit, it depends on your need
            chrome-win
               //...the browser files
```
    
    


The key sample codes as below:

```cs
                //Note: all _ started variable is reading from appseetings.json, or you can replace it with your fixed string 
                //I use serilog to show log info in console and write into log file together, use DI to inject to your class, it is another topic, not discuss here
               
                //Start read parameters from setting files
                //-----------------------------------------------------------------------------------------------------
                _ntUser = _config.GetValue<string>("NTUSER");
                _ntPass = _config.GetValue<string>("NTPASS");
                _headless = _config.GetValue<bool>("Headless");
                _needInstallPlaywright = _config.GetValue<bool>("NeedInstallPlaywright");
          
                _proxyServer = _config.GetValue<string>("Proxy"); //such as proxy.us.yourcompany.com:80
                if (_proxyServer.ToLower() == "false") //To treat "false" as "" too, sine we base on string length to do decision later
                {
                    _proxyServer = "";
                }
                _proxySettings = new ProxySettings { Server = _proxyServer, Username = _ntUser, Password = _ntPass };
                _credentials = new Credentials { Username = _ntUser, Password = _ntPass };

                _defaultNavagationTimeout = _config.GetValue<int>("DefaultNavigationTimeout");
                _defaultTimeout = _config.GetValue<int>("DefaultTimeout");

                _browsersPath = _config.GetValue<string>("BrowsersPath");
                _driverExecutablePath = _config.GetValue<string>("DriverExecutablePath");
                //-----------------------------------------------------------------------------------------------------
               
                var _version = "1.02"; //Manual assign a string to tell end uers the latest version
                _log.LogInformation("Author: Scott_Huang , Version = {ver}", _version);//show the automation program version

                if (_needInstallPlaywright)
                {
                    _log.LogInformation("Start download Playwright from internet ...");
                    _log.LogInformation("It may take long time in case this computer never download it before, or else, would be quick.");
                    await Playwright.InstallAsync();
                    _log.LogInformation("After download Playwright");
                }
                else
                {
                    _log.LogInformation("No need download Playwright per setting this time");
                }

                //create playwright
                _log.LogInformation("Prepare create Playwright");
                if (_browsersPath.Length > 0)
                {
                    _log.LogInformation("Use manual assigned browser driver path = {path}", _browsersPath);
                }
                if (_driverExecutablePath.Length > 0)
                {
                    _log.LogInformation("Use manual assigned playwright driver path = {path}", _driverExecutablePath);
                }

                //setting the custom path
                using var playwright = await Playwright.CreateAsync(
                    browsersPath: _browsersPath.Length>0?_browsersPath:null, 
                    driverExecutablePath: _driverExecutablePath.Length>0?_driverExecutablePath:null
                    );
                _log.LogInformation("Prepare launch browser");
                _log.LogInformation("Will hide browser UI = {headless}", _headless);
                
                //launch browser
                await using var browser = await playwright.Chromium.LaunchAsync(
                    headless: _headless,
                    slowMo: _config.GetValue<int>("Delay"),
                    //setup proxy if setting have value
                    proxy: (_proxyServer.Length > 0) ? _proxySettings : null
                    );

                _log.LogInformation("After launch, browser version:{version}", browser.Version);


                _log.LogInformation("New apage and set credentials");
                var context = await browser.NewContextAsync(acceptDownloads: true);
                //setting credentials if necessary, you can comment below line if no needed
                await context.SetHttpCredentialsAsync(_credentials);
                var page = await context.NewPageAsync();
             

                //Try to adjust timeout value by yourselves
                page.DefaultNavigationTimeout = (int)TimeSpan.FromSeconds(
                    _defaultNavagationTimeout).TotalMilliseconds;
                page.DefaultTimeout = (int)TimeSpan.FromSeconds(
                    _defaultTimeout).TotalMilliseconds;
                    
                await page.GoToAsync("http://anyurl.for.testing");    

```




