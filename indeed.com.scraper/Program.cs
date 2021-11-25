using CsvHelper;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace indeed.com.scraper
{
    public class DataModel
    {
        public string SearchedKeyword { get; set; }
        public string SearchedLocation { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string CompanyName { get; set; }
        public string ShortDescription { get; set; }
        public string Location { get; set; }
        public string PostTime { get; set; }
        public string EstimatedSalery { get; set; }
        public string JobType { get; set; }
        public string Rating { get; set; }
        public string FullDescription { get; set; }
        public string CurrentTime { get; set; }
    }
    class Program
    {
        static DateTime today = DateTime.Now;
        static string keyword = "epic analyst";
        static string searchedLocation = "United States";
        static bool getDescription = false;
        static string age = "";
        static List<DataModel> records = new List<DataModel>();
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Enter your search query: ");
                keyword = Console.ReadLine();
                Console.WriteLine("Enter location to be searched: ");
                searchedLocation = Console.ReadLine();

                Console.WriteLine("Do you want to scrape full description? (if yes press y else just press enter): ");
                var selection = Console.ReadLine();

                if (selection.ToLower() == "y")
                    getDescription = true;

                Console.WriteLine("Select Rnage");
                Console.WriteLine("Today(Select 1)");
                Console.WriteLine("3 Days(Select 3)");
                Console.WriteLine("7 Days(Select 7)");
                Console.WriteLine("14 Days(Select 14)");
                Console.WriteLine("To ignnore just press enter.");
                age = Console.ReadLine();

                ChromeOptions options = new ChromeOptions();
                options.AddArguments((IEnumerable<string>)new List<string>()
                {
                    "--silent-launch",
                    "--no-startup-window",
                    "no-sandbox",
                    "headless",
                    "incognito"
                });
              
                    ChromeDriverService defaultService = ChromeDriverService.CreateDefaultService();
                defaultService.HideCommandPromptWindow = true;
                bool showGUI = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("showGUI"));
                using (IWebDriver driver = (showGUI) ? new ChromeDriver() : (IWebDriver)new ChromeDriver(defaultService, options))
                {
                    try
                    {
                        driver.Manage().Window.Maximize();
                        Console.WriteLine("Loading https://www.indeed.com/...");
                        driver.Navigate().GoToUrl("https://www.indeed.com/");

                        driver.FindElement(By.Id("text-input-what")).SendKeys(keyword);
                        Thread.Sleep(1500);
                        driver.FindElement(By.Id("text-input-where")).SendKeys(Keys.Control + "a");
                        Thread.Sleep(1500);
                        driver.FindElement(By.Id("text-input-where")).SendKeys(searchedLocation);
                        Thread.Sleep(3000);
                        try
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("document.getElementById('whatWhereFormId').submit();");
                        }
                        catch { }

                        try
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("document.getElementById('jobsearch').submit();");
                        }
                        catch { }


                        Thread.Sleep(3000);

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(driver.PageSource);

                        var count = doc.DocumentNode.SelectSingleNode("//div[@id='searchCountPages']");
                        if (count == null) { Console.WriteLine("No results found against your search."); }
                        else
                        {

                            //  ExtractData(doc);
                            int pages = 1;
                            string url = driver.Url;
                            for (int i = 0; i < pages; i++)
                            {
                                Console.WriteLine($"Processing page {i + 1}");
                                string start = $"&start={(i * 50)}";
                                if (!string.IsNullOrEmpty(age))
                                    switch (age)
                                    {
                                        case "1":
                                            start += "&fromage=1";
                                            break;
                                        case "3":
                                            start += "&fromage=3";
                                            break;
                                        case "7":
                                            start += "&fromage=7";
                                            break;
                                        case "14":
                                            start += "&fromage=14";
                                            break;
                                        default:
                                            break;
                                    }
                                var parts = url.Split(new string[] { "&_ga" }, StringSplitOptions.RemoveEmptyEntries);
                                var newUrl =(parts[0]+ start);
                                driver.Navigate().GoToUrl($"{newUrl}{"&_ga"+parts[1]}");
                                Thread.Sleep(3000);
                                if (i == 0)
                                {
                                    doc = new HtmlDocument();
                                    doc.LoadHtml(driver.PageSource);

                                    count = doc.DocumentNode.SelectSingleNode("//div[@id='searchCountPages']");
                                    var total = Convert.ToInt32(count.InnerText.Split(new string[] { "of" }, StringSplitOptions.RemoveEmptyEntries)[1].Replace("jobs", "").Replace(",", "").Trim());
                                    pages = (total + 50 - 1) / 50;
                                }

                                ExtractData(driver);
                                Export();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: "+ex.Message);
                    }

                    driver.Close();
                    Thread.Sleep(3000);
                    driver?.Dispose();
                }
            
                if (records.Count > 0)
                {
                    Export();
                }
                else
                    Console.WriteLine("We have nothing to export");

            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("This version of ChromeDriver only supports Chrome"))
                {
                    Console.WriteLine("Please update your chrome browser.");
                }
                else
                    Console.WriteLine("We are unable to continue. Reason: " + ex.Message);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        public static bool CheckForLatestDrivers()
        {
            KillAlreadyRunningDriver();
            var versionInfo = FileVersionInfo.GetVersionInfo(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe");
            string currentChromeVersion = versionInfo.FileVersion;

            bool hasLatestDriver = false;
            string currentVersion = string.Empty;
            string versionFileName = "version.txt";
            if (File.Exists(versionFileName))
                currentVersion = File.ReadAllText(versionFileName);
            else
                File.Create("versionFileName");


            HtmlWeb web = new HtmlWeb();
            var doc = web.Load("https://sites.google.com/a/chromium.org/chromedriver/downloads");
            for (int i = 1; i <= 3; i++)
            {
                var linkNode = doc.DocumentNode.SelectSingleNode($"//*[@id=\"sites-canvas-main-content\"]/table/tbody/tr/td/div/div[1]/ul/li[{i}]/a");
                if (linkNode != null)
                {
                    var latestVersion = linkNode.InnerText.Replace("ChromeDriver ", "");
                    if (latestVersion.Split('.')[0] == currentChromeVersion.Split('.')[0])
                    {
                        if (latestVersion == currentVersion)
                        {
                            Console.WriteLine("You have the latest version of chrome driver");
                            hasLatestDriver = true;
                            break;
                        }
                        else
                        {
                            // Download drivers
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile($"https://chromedriver.storage.googleapis.com/{latestVersion}/chromedriver_win32.zip", "chromedriver_win32.zip");
                                    if (File.Exists("chromedriver.exe"))
                                    {
                                        KillAlreadyRunningDriver();

                                        File.Delete("chromedriver.exe");
                                    }
                                    ZipFile.ExtractToDirectory("chromedriver_win32.zip", Environment.CurrentDirectory);
                                    File.WriteAllText(versionFileName, latestVersion);
                                    hasLatestDriver = true;
                                }
                                Console.WriteLine("Driver downloaded successfully.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unable to download latest version. Reason: " + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Downloading latest version {latestVersion}");
                        if (latestVersion.Split('.')[0] == currentChromeVersion.Split('.')[0])
                        {
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile($"https://chromedriver.storage.googleapis.com/{latestVersion}/chromedriver_win32.zip", "chromedriver_win32.zip");
                                    if (File.Exists("chromedriver.exe"))
                                    {
                                        Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");
                                        foreach (var chromeDriverProcess in chromeDriverProcesses)
                                        {
                                            var path = chromeDriverProcess.MainModule.FileName;
                                            if (path == Path.Combine(Environment.CurrentDirectory, "chromedriver.exe"))
                                            {
                                                chromeDriverProcess.Kill();
                                            }
                                        }

                                        File.Delete("chromedriver.exe");
                                    }
                                    ZipFile.ExtractToDirectory("chromedriver_win32.zip", Environment.CurrentDirectory);
                                    File.WriteAllText(versionFileName, latestVersion);
                                    hasLatestDriver = true;
                                }
                                Console.WriteLine("Driver downloaded successfully.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unable to download latest version. Reason: " + ex.Message);
                            }
                        }

                    }
                }
            }







            return hasLatestDriver;
        }

        private static void KillAlreadyRunningDriver()
        {
            Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");
            foreach (var chromeDriverProcess in chromeDriverProcesses)
            {
                var path = chromeDriverProcess.MainModule.FileName;
                if (path == Path.Combine(Environment.CurrentDirectory, "chromedriver.exe"))
                {
                    chromeDriverProcess.Kill();
                }
            }
        }

        private static void Export()
        {
            string name = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}{today.Second}.csv";
            using (var writer = new StreamWriter(name))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            Console.WriteLine("Data exported successfully");
        }

        private static void ExtractData(IWebDriver driver)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(driver.PageSource);

            var node = doc.DocumentNode.SelectSingleNode("//div[@id='mosaic-provider-jobcards']");
            if (node != null)
            {
                foreach (var record in node.ChildNodes.Where(x=>x.Name=="a"))
                {
                   // Thread.Sleep(1000);
                    try
                    {
                        var entry = new DataModel()
                        {
                            SearchedKeyword = keyword,
                            SearchedLocation = searchedLocation,
                            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        var detailsDoc = new HtmlDocument();
                        detailsDoc.LoadHtml(record.InnerHtml);
                        var title = detailsDoc.DocumentNode.SelectSingleNode("//h2[@class='jobTitle jobTitle-color-purple jobTitle-newJob']");
                        if (title == null)
                        {
                            title = detailsDoc.DocumentNode.SelectSingleNode("//h2[@class='jobTitle jobTitle-color-purple']");
                        }
                        if (title != null)
                        {
                            entry.Url = HttpUtility.HtmlDecode("https://www.indeed.com" + record.Attributes.FirstOrDefault(x => x.Name == "href").Value);
                            entry.Title = HttpUtility.HtmlDecode((title.ChildNodes.Count==1)? title.ChildNodes[0].InnerText.Replace("\n", "").Replace("\r", "") : title.ChildNodes[1].InnerText.Replace("\n", "").Replace("\r", ""));
                            Console.WriteLine(entry.Title);
                        }

                        var company = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='companyName']");
                        if (company != null)
                        {
                            entry.CompanyName = HttpUtility.HtmlDecode(company.InnerText.Replace("\n", "").Replace("\r", ""));

                        }

                        var rating = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='ratingNumber']");
                        if (rating != null)
                        {
                            entry.Rating = HttpUtility.HtmlDecode(rating.InnerText.Replace("\n", "").Replace("\r", ""));
                        }

                        var location = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='companyLocation']");
                        if (location != null)
                        {
                            entry.Location = HttpUtility.HtmlDecode(location.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                        var description = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='job-snippet']");
                        if (description != null)
                        {
                            entry.ShortDescription = HttpUtility.HtmlDecode(description.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                        var postedOn = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='date']");
                        if (postedOn != null)
                        {
                            entry.PostTime = HttpUtility.HtmlDecode(postedOn.InnerText.Replace("Posted","").Replace("\n", "").Replace("\r", ""));
                        }
                        var estimatedSalery = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='estimated-salary']");
                        if (estimatedSalery != null)
                        {
                            entry.EstimatedSalery = HttpUtility.HtmlDecode(estimatedSalery.InnerText.Replace("\n", "").Replace("\r", ""));
                        }

                        var jobType = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='attribute_snippet']");
                        if (jobType != null)
                        {
                            entry.JobType = HttpUtility.HtmlDecode(jobType.InnerText.Replace("\n", "").Replace("\r", ""));
                        }



                        records.Add(entry);
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine("Unable to extract details. Reason: " + ex.Message);
                    }
                }
                if (getDescription)
                    foreach (var entry in records)
                    {
                        try
                        {
                            Console.WriteLine($"Loading {entry.Url}");
                            driver.Navigate().GoToUrl(entry.Url);
                            Thread.Sleep(2000);

                            doc.LoadHtml(driver.PageSource);

                            var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[@id=\"jobDescriptionText\"]");
                            if (descriptionNode != null)
                            {
                                entry.FullDescription = HttpUtility.HtmlDecode(descriptionNode.InnerText);
                            }
                        }
                        catch (Exception)
                        {

                        }

                       // Program.records.Add(entry);
                    }
            }
        }
    }
}
