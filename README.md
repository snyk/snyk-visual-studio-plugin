# Snyk Vulnerability Scanner


### Introduction

Snyk’s Vulnerability Scanner helps you find and fix security vulnerabilities in your projects. Within a few seconds, the extension will provide a list of all the different types of issues identified, bucketed into categories, together with actionable fix advice:

* Open Source Security - known vulnerabilities in both the direct and in-direct (transitive) open source dependencies you are pulling into the project.

#### 1. Software requirements

* Operating system - Windows. 
* Supported versions of Visual Studio: 2015, 2017, 2019. Compatible with Community, Professional and Enterprise.

#### 2. How to install the extension?

**Step 2.1.** Double click on VSIX file and install the extension on the Visual Studio of choice. Select all the versions of Visual Studio on which you want to install Snyk extension (Extension can be installed on all Visual Studio versions at once). 

![Vsix install](https://github.com/snyk/snyk-visual-studio-plugin/blob/feat/tree-view/doc/images/readme_image_2_1.png "Vsix install")

**Step 2.2.** Once installed, open the Snyk tool window by going to View > Other Windows as shown in the screenshot below.

![Snyk Toolwindow menu item](https://github.com/snyk/snyk-visual-studio-plugin/blob/feat/tree-view/doc/images/readme_image_2_2.png "Snyk Toolwindow menu item")

**Step 2.3.** Once the tool window appears, wait while Snyk extension downloads the latest Snyk CLI version.

![Snyk CLI download](https://github.com/snyk/snyk-visual-studio-plugin/blob/feat/tree-view/doc/images/readme_image_2_3.png "Snyk CLI download")

**Step 2.4.1.** By now you should have the extension installed and the Snyk CLI downloaded. Time to authenticate. The first way is to click "Connect Visual Studio to Snyk" link.

![Authenticate from overview panel](https://github.com/snyk/snyk-visual-studio-plugin/blob/feat/tree-view/doc/images/readme_image_2_4.png "Authenticate from overview panel")

**Step 2.4.2.** Or open Visual Studio Options to go to the General Settings of the Snyk extension. 

![Authenticate from Options](./doc/images/readme_image_2_5.PNG "Authenticate from Options")

**Step 2.5.** Authentication can be triggered by pressing the “Authenticate” button. If for some reason the automated way doesn’t work or input user API token by hand.

* If, however, the automated authentication doesn’t work for some reason, please reach out to us. We would be happy to investigate!

![Authenticate button or enter API token](./doc/images/readme_image_2_6.PNG "Authenticate button or enter API token")

**Step 2.6.** You will be taken to the website to verify your identity and connect the IDE extension.  Click the Authenticate button.

![Authenticate on webseite](./doc/images/readme_image_2_7.PNG "Authenticate on webseite")

**Step 2.7.** You will be taken to the website to verify your identity and connect the IDE extension.  Click the **Authenticate** button.

* Once the authentication has been confirmed, please feel free to close the browser and go back to the IDE extension. The Token field should have been populated with the authentication token. With that the authentication part should be done!

![Authentication finished](./doc/images/readme_image_2_8.PNG "Authentication finished")

#### 3. How to use the extension?

* Thank you for installing Snyk’s Visual Studio Extension! By now it should be fully installed. If you have any questions or you feel something is not as it should be, please don’t hesitate to reach out us.
* Let’s now see how to use the extension (continues on the next page).

**Step 3.1.** Open your solution and run Snyk scan. Depending on the size of your solution, time to build a dependency graph, it might take from less than a minute to a couple of minutes to get the vulnerabilities. 

* Note that your solution will have to successfully build in order to allow the CLI to pick up the dependencies (and find the vulnerabilities).
* If you see only NPM vulnerabilities or vulnerabilities that are not related to your C#/.NET projects, that might mean your project is not built successfully and wasn’t detected by the CLI. Feel free to reach out to us (contacts at the end of the document) if you think something is not as expected, we are happy to help or clarify something for you.

![Run scan](./doc/images/readme_image_3_1_1.PNG "Run scan")

![Scan results](./doc/images/readme_image_3_1_2.PNG "Scan results")

**Step 3.2.** You could filter vulnerabilities by name or by severity.

* Filter by name by typing the name of the vulnerability in the search bar.

![Filter by name](./doc/images/readme_image_3_2_1.PNG "Filter by name")
 
* Filter by severity by selecting one or more of the the severities when you open the search bar filter.

![Scan results](./doc/images/readme_image_3_2_2.PNG "Scan results")

**Step 3.3.** Users could configure Snyk extension by Project settings. 

* Note that the “Scan all projects” option is enabled  by default. It adds --all-projects option for Snyk CLI. This option scans all projects by default.

![Project additional configuraton options](./doc/images/readme_image_3_3.PNG "Project additional configuraton options")

#### 4. Known Caveats

##### 4.1 Could not detect supported target files

**Solution** Open Visual Studio Options to go to the Project Settings of the Snyk extension and check Scan all projects. 

![Scan all projects option](./doc/images/readme_image_4_1.PNG "Scan all projects option")

#### 5. Contacts

* If you have any issues please reach out to <support@snyk.io>.

#### 6. Conclusion

Thank you for reaching that far :)

It either means you’ve successfully run a scan with the Visual Studio extension or you’ve encountered an issue. Either way we would love to hear about it - so go ahead and use the above contacts. We are looking forward to hearing from you!

#### Build process

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
#### Useful links
* This plugin works with projects written in .NET, Java, JavaScript, and many more languages. [See the full list of languages and package managers Snyk supports](https://support.snyk.io/hc/en-us/sections/360001087857-Language-package-manager-support)                  
* [Bug tracker](https://github.com/snyk/snyk-visual-studio-plugin/issues)
