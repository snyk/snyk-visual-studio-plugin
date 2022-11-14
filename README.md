# Visual Studio extension

The Snyk Visual Studio extension provides analysis of your code and open source dependencies.

Snyk scans for vulnerabilities and returns results with security issues categorized by issue type and severity.

For open source, you receive automated algorithm-based fix suggestions for both direct and transitive dependencies.

This single plugin provides a Java vulnerability scanner, a custom code vulnerability scanner, and an open-source security scanner.

Snyk scans for the following types of issues:

* [**Open Source Security**](https://snyk.io/product/open-source-security-management/) - security vulnerabilities and license issues in both direct and indirect (transitive) open-source dependencies pulled into the Snyk Project. \
  See also the [Open Source docs](https://docs.snyk.io/products/snyk-open-source).
* [**Code Security and Code Quality**](https://snyk.io/product/snyk-code/) - security vulnerabilities and quality issues in your code. See also the Snyk Code docs. \
  See also the [Snyk Code docs](https://docs.snyk.io/products/snyk-code).

This page explains installation of the Visual Studio extension. **After you complete the steps on this page**, you will continue by following the instructions in the other Visual studio extension docs:

* [Visual Studio extension configuration](https://docs.snyk.io/ide-tools/visual-studio-extension/visual-studio-extension-configuration)
* [Visual Studio extension authentication](https://docs.snyk.io/ide-tools/visual-studio-extension/visual-studio-extension-authentication)
* [Run an analysis with Visual Studio extension](https://docs.snyk.io/ide-tools/visual-studio-extension/run-an-analysis-with-visual-studio-extension)
* [View analysis results from Visual Studio extension](https://docs.snyk.io/ide-tools/visual-studio-extension/view-analysis-results-from-visual-studio-extension)
* [Troubleshooting and known issues with Visual Studio extension](https://docs.snyk.io/ide-tools/visual-studio-extension/troubleshooting-and-known-issues-with-visual-studio-extension)

The following are also available:

* [Bug tracker](https://github.com/snyk/snyk-visual-studio-plugin/issues)
* [Github repository](https://github.com/snyk/snyk-visual-studio-plugin)

## Supported languages, package managers, and frameworks

Supported languages and frameworks include C#, JavaScript, TypeScript, Java, Go , Ruby, Python, PHP, Scala, Swift, Objective-C, and .NET.

* For Snyk Open Source: the Visual Studio extension supports all the languages and package managers supported by Snyk Open Source and the CLI. See the full list [in the docs](https://docs.snyk.io/products/snyk-open-source/language-and-package-manager-support).
* For Snyk Code: the Visual Studio extension supports all the [languages and frameworks supported by Snyk Code](https://docs.snyk.io/products/snyk-code/snyk-code-language-and-framework-support#language-support-with-snyk-code-ai-engine).

## Software requirements

* Operating system - Windows
* Supported versions of Visual Studio: 2015, 2017, 2019, 2022 (version 17.0.5 and above). The extension is compatible with Community, Professional, and Enterprise plans.

## Install the extension

You can install the Snyk extension directly from the IDE; open **Extensions > Manage Extensions**.

![Manage extensions menu](https://github.com/snyk/user-docs/raw/HEAD/docs/.gitbook/assets/readme\_image\_2\_1\_1.png)

Search for _Snyk_ and select **Download** to download the Snyk Security - Code and Open Source Dependencies extension.

Once installed, use Snyk through the **Extensions > Snyk** menu (on Visual Studio versions older than 2019, Snyk is part of the top menu bar).

<img src="https://github.com/snyk/user-docs/raw/HEAD/docs/.gitbook/assets/image (351) (1) (1) (1).png" alt="" />

You can also open the Snyk tool window using **View > Other Windows > Snyk**_._

Once the tool window opens, wait while the Snyk extension downloads the latest Snyk CLI version.

![Snyk tool window, CLI downloading](https://github.com/snyk/user-docs/raw/HEAD/docs/.gitbook/assets/readme\_image\_2\_3.png)

After you install the extension and the CLI you must authenticate. You can use the **Connect Visual Studio to Snyk** link. For more information and additional ways to authenticate see [Visual Studio extension authentication](https://docs.snyk.io/ide-tools/visual-studio-extension/visual-studio-extension-authentication).

## Support

If you need help, submit a [request](https://support.snyk.io/hc/en-us/requests/new) to Snyk Support.
