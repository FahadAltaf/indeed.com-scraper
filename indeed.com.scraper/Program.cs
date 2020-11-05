using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace indeed.com.scraper
{
    class Program
    {
        static List<string> entries = new List<string>();
        static void Main(string[] args)
        {
            using (var driver = new ChromeDriver())
            {
                Console.WriteLine("Loading https://www.indeed.com/...");
                driver.Navigate().GoToUrl("https://www.indeed.com/");

                driver.FindElement(By.Id("text-input-what")).SendKeys("director of rehabilitation");
                driver.FindElement(By.Id("text-input-where")).SendKeys(Keys.Control + "a");
                driver.FindElement(By.Id("text-input-where")).SendKeys("United States");
                driver.FindElement(By.XPath("//*[@id=\"whatWhereFormId\"]/div[3]/button")).Click();

                Thread.Sleep(3000);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(driver.PageSource);

                var count = doc.DocumentNode.SelectSingleNode("//div[@id='searchCountPages']");
                var total = Convert.ToInt32(count.InnerText.Split(new string[] { "of" }, StringSplitOptions.RemoveEmptyEntries)[1].Replace("jobs", "").Replace(",", "").Trim());
              //  ExtractData(doc);
                int pages = (total + 50 - 1) / 50;
                string url = driver.Url;
                for (int i = 0; i < pages; i++)
                {
                    Console.WriteLine($"Processing page {i+1}");
                    string start = $"&start={(i * 50)}&limit=50";
                    var newUrl = url.Split(new string[] { "&vjk" },StringSplitOptions.RemoveEmptyEntries)[0] + start;
                    driver.Navigate().GoToUrl(newUrl);
                    Thread.Sleep(3000);
                    doc = new HtmlDocument();
                    doc.LoadHtml(driver.PageSource);
                    ExtractData(doc);
                }
            }
        }

        private static void ExtractData(HtmlDocument doc)
        {

            var nodes = doc.DocumentNode.SelectNodes("//div[@class='jobsearch-SerpJobCard unifiedRow row result clickcard']");
            var node = doc.DocumentNode.SelectSingleNode("//div[@class='jobsearch-SerpJobCard unifiedRow row result clickcard vjs-highlight']");
            if (nodes != null && node != null)
                nodes.Add(node);
            if (nodes != null && nodes.Count > 0)
                foreach (var record in nodes)
                {
                    var detailsDoc = new HtmlDocument();
                    detailsDoc.LoadHtml(record.InnerHtml);
                    var title = detailsDoc.DocumentNode.SelectSingleNode("/h2[1]/a[1]");
                    if (title != null)
                    {
                        entries.Add(title.Attributes.FirstOrDefault(x=>x.Name=="href").Value);
                        Console.WriteLine(title.Attributes.FirstOrDefault(x => x.Name == "href").Value);
                    }

                }
        }
    }
}
