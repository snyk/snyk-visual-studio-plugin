# Deprecation Notice
## Effective Date: 20 August 2024
As of 20 August 2024, we will no longer support Visual Studio 2015, 2017, and 2019. Please upgrade to Visual Studio 2022 to continue receiving updates, security patches, and technical support.

# Visual Studio extension

The Snyk Visual Studio extension provides analysis of your code and open-source dependencies. Download the plugin at any time free of charge and use it with any Snyk account. Scan your code early in the development lifecycle to help you pass security reviews and avoid costly fixes later in the development cycle.

Snyk scans for vulnerabilities and returns results with security issues categorized by issue type and severity.

For open source, you receive automated algorithm-based fix suggestions for both direct and transitive dependencies.

This single plugin provides a Java vulnerability scanner, a custom code vulnerability scanner, and an open-source security scanner.

Snyk scans for the following types of issues:

* [**Open Source Security**](https://snyk.io/product/open-source-security-management/) - security vulnerabilities and license issues in both direct and indirect (transitive) open-source dependencies pulled into the Snyk Project.\
  See also the [Snyk Open Source docs](https://docs.snyk.io/scan-applications/snyk-open-source).
* [**Code Security**](https://snyk.io/product/snyk-code/) - security vulnerabilities. See also the Snyk Code docs.\
  See also the [Snyk Code docs](https://docs.snyk.io/scan-applications/snyk-code)_**.**_

In using the Visual Studio extension, you have the advantage of relying on the [Snyk Vulnerability Database](https://security.snyk.io/). You also have available the [Snyk Code AI Engine](https://docs.snyk.io/scan-with-snyk/snyk-code#ai-engine).

This page explains installation of the Visual Studio extension. **After you complete the steps on this page**, you will continue by following the instructions in the other Visual studio extension docs, starting with _**Visual Studio extension configuration**_.

The following are also available:

* [Bug tracker](https://github.com/snyk/snyk-visual-studio-plugin/issues)
* [Github repository](https://github.com/snyk/snyk-visual-studio-plugin)

The plugin runs on Windows.

## Supported Visual Studio versions

Supported versions of Visual Studio are 2015, 2017, 2019, and 2022 (version 17.0.5 and above).

## Supported languages, package managers, and frameworks

Supported languages and frameworks include C#, JavaScript, TypeScript, Java, Go , Ruby, Python, PHP, Scala, Swift, Objective-C, unmanaged C/C++ and .NET.

* For Snyk Open Source: the Visual Studio extension supports all the languages and package managers supported by Snyk Open Source and the CLI. See the full list on the page [Supported languages, frameworks, and feature availability overview, in the Open Source section](https://docs.snyk.io/scan-applications/supported-languages-and-frameworks/supported-languages-frameworks-and-feature-availability-overview#open-source-and-licensing-snyk-open-source).
* For Snyk Code: the Visual Studio extension supports all the [languages and frameworks supported by Snyk Code](https://docs.snyk.io/scan-applications/supported-languages-and-frameworks/supported-languages-frameworks-and-feature-availability-overview#code-analysis-snyk-code).

## Supported operating systems and architecture


Snyk plugins are not supported on any operating system that has reached End Of Life (EOL) with the distributor.&#x20;


You can use the Snyk Visual Studio extension in the following environments:

* Windows: 386, AMD64, and ARM64
* MacOS: Visual Studio Windows plugin in a Windows virtual machine inside a Mac with an ARM64 processor

## Install the extension

You can install the Snyk extension directly from the IDE; open **Extensions > Manage Extensions**.

<figure><img src="https://github.com/snyk/user-docs/raw/HEAD/docs/.gitbook/assets/readme_image_2_1_1.png" alt="Manage extensions menu"><figcaption><p>Manage extensions menu</p></figcaption></figure>

Search for _Snyk_ and select **Download** to download the Snyk Security extension.

After you install, use Snyk through the **Extensions > Snyk** menu. (On Visual Studio versions older than 2019, Snyk is part of the top menu bar).

<figure><img src="https://github.com/snyk/user-docs/raw/HEAD/docs/.gitbook/assets/image (351) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt="Snyk extensions menu"><figcaption><p>Snyk extensions menu</p></figcaption></figure>

You can also open the Snyk tool window using **View > Other Windows > Snyk**_._

Once the tool window opens, wait while the Snyk extension downloads the latest Snyk CLI version.

<figure><img src="https://github.com/snyk/user-docs/raw/HEAD/docs/.gitbook/assets/readme_image_2_3.png" alt="Snyk tool window, CLI downloading"><figcaption><p>Snyk tool window, CLI downloading</p></figcaption></figure>

After you install the extension and the CLI you must authenticate. You can use the **Connect Visual Studio to Snyk** link. For more information and additional ways to authenticate see [Visual Studio extension authentication](https://docs.snyk.io/ide-tools/visual-studio-extension/visual-studio-extension-authentication).

## Support

If you need help, submit a request to [Snyk Support](https://support.snyk.io/hc/en-us/requests/new).
