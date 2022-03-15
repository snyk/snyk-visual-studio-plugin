# Snyk for Visual Studio

**The Visual Studio extension (Snyk Security - Code and Open Source Dependencies) helps you find and fix security vulnerabilities in your projects. Within a few seconds, the extension will provide a list of all the different types of security vulnerabilities identified together with actionable fix advice. The extension combines the power of two Snyk products: Snyk Open Source and Snyk Code:**

1. With Snyk Open Source we find known vulnerabilities in both the direct and in-direct (transitive) open source dependencies you are pulling into the project.
2. With Snyk Code we find known security vulnerabilities and code quality issues at a blazing speed looking at the code you and your team wrote.

# Table of Contents

- [Introduction](#introduction)
    - [Software requirements](#software-requirements)
    - [Supported languages](#supported-languages)
- [Install the extension](#install-the-extension)
  - [Configuration](#configuration)    
  - [Authentication](#authentication)
- [Run analysis](#run-analysis)
  - [Open Source vulnerabilities](#open-source-vulnerabilities)
  - [Snyk Code vulnerabilities](#snykcode-vulnerabilities)
- [View analysis results](#view-analysis-results)
- [Extension Configuration](#extension-configuration)
- [Known Caveats](#known-caveats)
    - [Could not detect supported target files](#could-not-detect-supported-target-files)
- [How to](#how-to)
    - [How to find the log files](#how-to-find-the-log-files)
    - [Build process](#build-process)
- [Contacts](#contacts)
- [Conclusion](#conclusion)
- [Useful links](#useful-links) 

## Introduction

### Software requirements

* Operating system - Windows. 
* Supported versions of Visual Studio: 2015, 2017, 2019, 2022. Compatible with Community, Professional and Enterprise.

### Supported languages

- Supported languages for Snyk Open Source are C#, JavaScript, TypeScript, Java, Go, Ruby, Python, PHP, Scala, Swift, Objective-C. See the full list [here](https://docs.snyk.io/products/snyk-open-source/language-and-package-manager-support).
- Supported languages for Snyk Code are C# ([frameworks](https://docs.snyk.io/products/snyk-code/snyk-code-language-and-framework-support#c-frameworks)), Java, JavaScript, TypeScript, Python. See the full list [here](https://docs.snyk.io/products/snyk-code/snyk-code-language-and-framework-support).

## Install the extension

The Snyk extension can be installed directly from IDE. To install it open *Extensions > Manage Extensions* menu.

![Vsix install via Marketplace](doc/images/readme_image_2_1_1.png)

Search for *Snyk*

![Vsix install via Marketplace](doc/images/readme_image_2_1_2.png)

Once installed, open the Snyk tool window by going to *View > Other Windows* as shown in the screenshot below.

![Snyk Toolwindow menu item](doc/images/readme_image_2_2.png "Snyk Toolwindow menu item")

Once the tool window appears, wait while Snyk extension downloads the latest Snyk CLI version.

![Snyk CLI download](doc/images/readme_image_2_3.png "Snyk CLI download")

By now you should have the extension installed and the Snyk CLI downloaded. Time to authenticate. The first way is to click "Connect Visual Studio to Snyk" link.

## Configuration
### Environment
To analyse projects, the plugin uses the Snyk CLI which needs some environment variables. The following variables are needed or helpful, dependent on the type of project you analyse:

- `PATH` should contain the path to needed binaries, e.g. to maven, gradle, python, go.
- `JAVA_HOME` should contain the path to the JDK you want to use for analysis of Java dependencies
- `http_proxy` and `https_proxy` can be used to provide information about a proxy server. The format is `http_proxy=http://username:password@proxyhost:proxyport`

You can set the variables, using the GUI or on the command line using the `setx` tool.

## Authentication

Authenticate using *"Connect Visual Studio to Snyk"* link on Overview page.

![Authenticate from overview panel](doc/images/readme_image_2_4.png "Authenticate from overview panel")

Or authenticate via Options. Open Visual Studio *Options* and go to the *General Settings* of the Snyk extension.

![Authenticate from Options](doc/images/readme_image_2_5.png "Authenticate from Options")

Authentication can be triggered by pressing the “Authenticate” button. If for some reason the automated way doesn’t work or input user API token by hand.

If, however, the automated authentication doesn’t work for some reason, please reach out to us. We would be happy to investigate!

![Authenticate button or enter API token](doc/images/readme_image_2_6.png "Authenticate button or enter API token")

You will be taken to the website to verify your identity and connect the IDE extension.  Click the **Authenticate** button.

![Authenticate on webseite](doc/images/readme_image_2_7.png "Authenticate on webseite")

Once the authentication has been confirmed, please feel free to close the browser and go back to the IDE extension. The Token field should have been populated with the authentication token. With that the authentication part should be done!

![Authentication finished](doc/images/readme_image_2_8.png "Authentication finished")

## Run analysis

* Thank you for installing Snyk’s Visual Studio Extension! By now it should be fully installed. If you have any questions or you feel something is not as it should be, please don’t hesitate to reach out us.
* Let’s now see how to use the extension (continues on the next page).

Open your solution and run Snyk scan. Depending on the size of your solution, time to build a dependency graph, it might take from less than a minute to a couple of minutes to get the vulnerabilities. 

The extension provides the user with two kinds of results:

* Open Source vulnerabilities
* Snyk Code issues

### Open Source vulnerabilities

* Note that your solution will have to successfully build in order to allow the CLI to pick up the dependencies (and find the vulnerabilities).
* If you see only NPM vulnerabilities or vulnerabilities that are not related to your C#/.NET projects, that might mean your project is not built successfully and wasn’t detected by the CLI. Feel free to reach out to us (contacts at the end of the document) if you think something is not as expected, we are happy to help or clarify something for you.

![Run scan](doc/images/readme_image_3_1_1.png "Run scan")

![Scan results](doc/images/readme_image_3_1_2.png "Scan results")

### Snyk Code issues

Snyk Code analysis shows a list of security vulnerabilities and code issues found in the application code. For more details and examples of how others fixed the issue, select a security vulnerability or a code security issue. Once selected you will see the Snyk suggestion information in a panel.

![ScanCode results example1](doc/images/readme_image_3_1_3.png "ScanCode results example1")

The Snyk Suggestion panel shows the argumentation of the Snyk engine using for example variable names of your code and the line numbers in red. You can also see:

* Links to external resources to explain the bug pattern in more detail (the More info link).
* Tags that were assigned by Snyk, such as Security (the issue found is a security issue), Database (it is related to database interaction), or In Test (the issue is within the test code).
* Code from open source repositories that might be of help to see how others fixed the issue.

![ScanCode results example2](doc/images/readme_image_3_1_4.png "ScanCode results example2")

## View analysis results

You could filter vulnerabilities by name or by severity.

* Filter by name by typing the name of the vulnerability in the search bar.

![Filter by name](doc/images/readme_image_3_2_1.png "Filter by name")
 
* Filter by severity by selecting one or more of the the severities when you open the search bar filter.

![Scan results](doc/images/readme_image_3_2_2.png "Scan results")

Users could configure Snyk extension by Project settings. 

* Note that the “Scan all projects” option is enabled  by default. It adds --all-projects option for Snyk CLI. This option scans all projects by default.

![Project additional configuraton options](doc/images/readme_image_3_3.png "Project additional configuraton options")

## Extension Configuration

After the plugin is installed, you can set the following configurations for the extension:

- **Token**: the token the extension uses to connect to Snyk. You can manually replace it, if you need to switch
  to another account.
- **Custom endpoint**: Custom Snyk API endpoint for your organization.
- **Ignore unknown CA**: Ignore unknown certificate authorities.
- **Organization**: Specify the ORG_NAME to run Snyk commands tied to a specific organization.
- **Send usage analytics**: To help us improve the extension, you can let your Visual Studio send us information about how it’s working.
- **Project settings**: Specify any additional Snyk CLI parameters.
- **Scan all projects**: Auto-detect all projects in the working directory. It's enabled by default.

In the settings, you can also choose which results you want to receive:

* Open Source vulnerabilities 
* Snyk Code Security vulnerabilities
* Snyk Code Quality issues

![Extension Settings Product Selection](doc/images/readme_image_settings_product_selection.png "Extension Settings Product Selection")

## Known Caveats

### Could not detect supported target files

**Solution** Open Visual Studio Options to go to the Project Settings of the Snyk extension and check Scan all projects. 

![Scan all projects option](doc/images/readme_image_4_1.png "Scan all projects option")

## How to

### How to find the log files

Logs could be found in user AppData directory:
```
%HOMEPATH%\AppData\Local\Snyk\snyk-extension.log
```

### Build process

Close this repository to local machine:
```
git clone https://github.com/snyk/snyk-visual-studio-plugin.git
```

Restore Nuget packages:
```
nuget restore
```

Run build:
```
msbuild -t:Build
```

## Contacts

* If you have any issues please reach out to <support@snyk.io>.

## Conclusion

Thank you for reaching that far :)

It either means you’ve successfully run a scan with the Visual Studio extension or you’ve encountered an issue. Either way we would love to hear about it - so go ahead and use the above contacts. We are looking forward to hearing from you!

## Useful links
* This plugin works with projects written in .NET, Java, JavaScript, and many more languages. [See the full list of languages and package managers Snyk supports](https://support.snyk.io/hc/en-us/sections/360001087857-Language-package-manager-support)                  
* [Bug tracker](https://github.com/snyk/snyk-visual-studio-plugin/issues)
