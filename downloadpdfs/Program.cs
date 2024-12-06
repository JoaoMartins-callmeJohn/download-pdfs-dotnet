using Autodesk.ModelDerivative.Model;
using Autodesk.ModelDerivative;
using System;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Web;

namespace downloadpdfs
{
	internal class Program
	{
		static ModelDerivativeClient modelDerivativeClient = null!;
		static async Task Main(string[] args)
		{
			Console.WriteLine("Paste your token:");
			string access_token = Console.ReadLine();

			Console.WriteLine("Paste your design base64 encoded urn:");
			string urn = Console.ReadLine();

      Console.WriteLine("Paste the path for the PDFs");
			string path = Console.ReadLine();

			StaticAuthenticationProvider staticAuthenticationProvider = new StaticAuthenticationProvider(access_token);
			// Instantiate ModelDerivativeClient using the auth provider
			modelDerivativeClient = new ModelDerivativeClient(authenticationProvider: staticAuthenticationProvider);
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
			
		}
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

		//download a file from a url and save in a path
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
	}
}
