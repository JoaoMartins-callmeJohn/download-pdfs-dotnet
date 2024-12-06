# download-pdfs-dotnet
![Platforms](https://img.shields.io/badge/platform-Windows|MacOS-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET%20-8-blue.svg)
[![License](http://img.shields.io/:license-MIT-blue.svg)](http://opensource.org/licenses/MIT)
[![oAuth2](https://img.shields.io/badge/oAuth2-v1-green.svg)](http://developer.autodesk.com/)
[![Data-Management](https://img.shields.io/badge/Data%20Management-v2-green.svg)](http://developer.autodesk.com/)
[![BIM360](https://img.shields.io/badge/BIM360-v1-green.svg)](http://developer.autodesk.com/)
[![ACC](https://img.shields.io/badge/ACC-v1-green.svg)](http://developer.autodesk.com/)

## Introduction
Sample to download pdfs from manifest using .NET SDK
This sample covers the steps after you already acquired a token with proper permissions and a urn to retrieve the manifest.

## Te approach

First we obtain the manifest
```cs
public static async Task<List<ManifestDerivative>> GetManifestAsync(string urn)
{
		// fetch manifest response
		try
		{
			Manifest manifestResponse = await modelDerivativeClient.GetManifestAsync(urn, region: Region.US);
			// query for urn, progress etc...
			string manifestUrn = manifestResponse.Urn;
			string progress = manifestResponse.Progress;
			// get list of derivatives. Query further to get children etc.
			List<ManifestDerivative> derivatives = manifestResponse.Derivatives;
			return derivatives;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error obtaining the manifest from urn {urn}");
			throw;
		}
}
```

Once we get the urns for the PDFs, we need to generate the download URLs
```cs
public static async Task<string> GetDerivativeDownloadURLAsync(string urn, string pdfUrn)
{
		try
		{
			DerivativeDownload derivativeDownload = await modelDerivativeClient.GetDerivativeUrlAsync(pdfUrn, urn, Region.US);
			// the below returns a downloadable url including the coookies
			return derivativeDownload.Url;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error obtaining download url for urn: {pdfUrn}");
			throw;
		}
}
```

Last but not least, we download the PDFs using the URLs and save in a local path
```cs
public static async Task DownloadFileAsync(string url, string path)
{
		using (HttpClient client = new HttpClient())
		{
			using (HttpResponseMessage response = await client.GetAsync(url))
			{
				using (HttpContent content = response.Content)
				{
					// read the content and save to a file
					byte[] data = await content.ReadAsByteArrayAsync();
					File.WriteAllBytes(path, data);
					Console.WriteLine($"File {path} downloaded and saved!");
				}
			}
		}
}
```

Putting everything together:
```cs
try
{
		List<ManifestDerivative> derivatives = await GetManifestAsync(urn);
		ManifestDerivative svfDerivative = derivatives.Where(derivative => derivative.OutputType == "svf").ToList().First();
		List<ManifestResources> twodResources = svfDerivative.Children.Where(child => child.Role == "2d").ToList();
		//code to create tasks array that will download the pdfs and wait for all to finish
		List<Task> tasks = new List<Task>();
		foreach (ManifestResources twodResource in twodResources)
		{
				try
				{
					ManifestResources pdfResource = twodResource.Children.First(ManifestResources => ManifestResources.Mime == "application/pdf");
					string downloadPDFURL = await GetDerivativeDownloadURLAsync(urn, pdfResource.Urn);
					tasks.Add(DownloadFileAsync(downloadPDFURL, Path.Combine(path, $"{pdfResource.Urn.Split("/").Last()}.pdf")));
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
		}
		await Task.WhenAll(tasks.ToArray());
		Console.WriteLine("PDFs downloaded successfully!");
		Console.ReadKey();
}
catch (Exception ex)
{
		Console.WriteLine(ex.Message);
}
```
# Setup

## Prerequisites

1. **APS Account**: Learn how to create a APS Account, activate subscription and create an app at [this tutorial](http://aps.autodesk.com/tutorials/#/account/).
2. **Visual Studio**: Community or Pro.
3. **.NET** basic knowledge with C#

## Running locally

Clone this project or download it. It's recommended to install [GitHub desktop](https://desktop.github.com/). To clone it via command line, use the following (**Terminal** on MacOSX/Linux, **Git Shell** on Windows):

    git clone https://github.com/joaomartins-callmejohn/download-pdfs-dotnet

**Visual Studio** (Windows):

Replace **client_id** and **client_secret** with your own keys.
You can do it directly in the 'Properties/lauchSettings.json' file or through Visual Studio UI under the debug properties.

# Further Reading

### Troubleshooting

1. **Incorrect urn**: The urn must be passed as base64 encoded string

2. **Not able to read the file**: The file path must be pasted without ""

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

## Written by

Jo�o Martins [in/jpornelas](https://linkedin.com/in/jpornelas)
