using CsvHelper;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public string Rating { get; set; }
        public string FullDescription { get; set; }
        public string CurrentTime { get; set; }
    }
    class Program
    {
        static string keyword = "epic analyst";
        static string searchedLocation = "United States";
        static bool getDescription = false;
        static string age = "";
        static List<DataModel> entries = new List<DataModel>();
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
                        //"--silent-launch",
                        //"--no-startup-window",
                        //"no-sandbox",
                        //"headless",
                        //"incognito"
                });

                ChromeDriverService defaultService = ChromeDriverService.CreateDefaultService();
                defaultService.HideCommandPromptWindow = true;
                bool showGUI = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("showGUI"));
                using (IWebDriver driver = (showGUI) ? new ChromeDriver() : (IWebDriver)new ChromeDriver(defaultService, options))
                {
                    driver.Manage().Window.Maximize();
                    Console.WriteLine("Loading https://www.indeed.com/...");
                    driver.Navigate().GoToUrl("https://www.indeed.com/");

                    driver.FindElement(By.Id("text-input-what")).SendKeys(keyword);
                    driver.FindElement(By.Id("text-input-where")).SendKeys(Keys.Control + "a");
                    driver.FindElement(By.Id("text-input-where")).SendKeys(searchedLocation);
                    driver.FindElement(By.XPath("//*[@id=\"whatWhereFormId\"]/div[3]/button")).Click();

                    Thread.Sleep(3000);

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(driver.PageSource);

                    var count = doc.DocumentNode.SelectSingleNode("//div[@id='searchCountPages']");
                    if (count == null) { Console.WriteLine("No results found against your search."); }
                    else
                    {
                        var total = Convert.ToInt32(count.InnerText.Split(new string[] { "of" }, StringSplitOptions.RemoveEmptyEntries)[1].Replace("jobs", "").Replace(",", "").Trim());
                        //  ExtractData(doc);
                        int pages = (total + 50 - 1) / 50;
                        string url = driver.Url;
                        for (int i = 0; i < pages; i++)
                        {
                            Console.WriteLine($"Processing page {i + 1}");
                            string start = $"&start={(i * 50)}&limit=50";
                            if(!string.IsNullOrEmpty(age))
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
                            var newUrl = url.Split(new string[] { "&vjk" }, StringSplitOptions.RemoveEmptyEntries)[0] + start;
                            driver.Navigate().GoToUrl(newUrl);
                            Thread.Sleep(3000);

                            ExtractData(driver);
                        }
                    }

                    driver.Close();
                    Thread.Sleep(3000);
                    driver?.Dispose();
                }

                if (entries.Count > 0)
                {
                    var today = DateTime.Now;
                    string name = $"{today.Year}{today.Month}{today.Day}{today.Hour}{today.Minute}{today.Second}.csv";
                    using (var writer = new StreamWriter(name))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(entries);
                    }
                    Console.WriteLine("Data exported successfully");
                }
                else
                    Console.WriteLine("We have nothing to export");

            }
            catch (Exception ex)
            {
                Console.WriteLine("We are unable to continue. Reason: " + ex.Message);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static void ExtractData(IWebDriver driver)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(driver.PageSource);

            var nodes = doc.DocumentNode.SelectNodes("//div[@class='jobsearch-SerpJobCard unifiedRow row result clickcard']");
            var node = doc.DocumentNode.SelectSingleNode("//div[@class='jobsearch-SerpJobCard unifiedRow row result clickcard vjs-highlight']");
            if (nodes != null && node != null)
                nodes.Insert(0,node);
            if (nodes != null && nodes.Count > 0)
                foreach (var record in nodes)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        var entry = new DataModel() { SearchedKeyword = keyword, SearchedLocation = searchedLocation, CurrentTime= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                        var detailsDoc = new HtmlDocument();
                        detailsDoc.LoadHtml(record.InnerHtml);
                        var title = detailsDoc.DocumentNode.SelectSingleNode("//h2[@class='title']");
                        if (title != null)
                        {
                            entry.Url = "https://www.indeed.com" + title.ChildNodes[1].Attributes.FirstOrDefault(x => x.Name == "href").Value;
                            entry.Title = HttpUtility.HtmlDecode(title.InnerText.Replace("\nnew", "").Replace("\n", "").Replace("\r", ""));
                            Console.WriteLine(entry.Title);
                        }

                        var company = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='company']");
                        if (company != null)
                        {
                            entry.CompanyName = HttpUtility.HtmlDecode(company.InnerText.Replace("\n", "").Replace("\r", ""));

                        }

                        var rating = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='ratingsContent']");
                        if (rating == null)
                        {
                            rating = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='ratingsContent']");
                        }
                        if (rating != null)
                        {
                            entry.Rating = HttpUtility.HtmlDecode(rating.InnerText.Replace("\n", "").Replace("\r", ""));
                        }

                        var location = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='location accessible-contrast-color-location']");
                        if (location == null)
                        {
                            location = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='location accessible-contrast-color-location']");
                        }
                        if (location != null)
                        {
                            entry.Location = HttpUtility.HtmlDecode(location.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                        var description = detailsDoc.DocumentNode.SelectSingleNode("//div[@class='summary']");
                        if (description != null)
                        {
                            entry.ShortDescription = HttpUtility.HtmlDecode(description.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                        var postedOn = detailsDoc.DocumentNode.SelectSingleNode("//span[@class='date ']");
                        if (postedOn != null)
                        {
                            entry.PostTime = HttpUtility.HtmlDecode(postedOn.InnerText.Replace("\n", "").Replace("\r", ""));
                        }
                        if (getDescription)
                        {
                            if (!record.Attributes.FirstOrDefault(x=>x.Name=="class").Value.Contains("vjs-highlight"))
                            {
                                driver.FindElement(By.XPath(record.XPath)).Click();
                                Thread.Sleep(5000);
                            }
                            

                            driver.SwitchTo().Frame(driver.FindElement(By.Id("vjs-container-iframe")));
                            detailsDoc.LoadHtml(driver.PageSource);
                            var descriptionNode = detailsDoc.DocumentNode.SelectSingleNode("//*[@id=\"jobDescriptionText\"]");
                            if (descriptionNode != null)
                            {
                                entry.FullDescription = HttpUtility.HtmlDecode(descriptionNode.InnerText);
                            }
                            driver.SwitchTo().DefaultContent();
                        }
                        
                        entries.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        
                        Console.WriteLine("Unable to extract details. Reason: " + ex.Message);
                    }
                }
        }
    }
}
