using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CrawlerApp
{
    public enum contentType{
        InnerText,
        InnerHtml
    }

    class Program
    {
        static void Main(string[] args)
        {
            /*
             * NOTE:
             * In order to get the header params/values.
             * Make sure to try to browse first the url in the broswer. 
             * Inspect and take note of the header variables. Then use the values and simulate in your scraper.
             */
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36");
            headers.Add(HttpRequestHeader.Referer, "");
            headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            headers.Add(HttpRequestHeader.AcceptEncoding, "System.Text.Encoding.UTF8");
            headers.Add(HttpRequestHeader.AcceptCharset, "ISO-8859-1");
            headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.8");

            WebClientHelper wch = new WebClientHelper();
            //https://www.amazon.com/adidas-Womens-Original-NMD-XR1-Shoes/dp/B06XKGT2GX/ref=pd_sbs_309_13?_encoding=UTF8&pd_rd_i=B06XKGT2GX&pd_rd_r=AG2R659SZM6VPN0XMXP5&pd_rd_w=XND74&pd_rd_wg=3FIK2&psc=1&refRID=AG2R659SZM6VPN0XMXP5
            //https://www.amazon.com/Adidas-PrimeKnit-Womens-Collegiate-BB3685/dp/B06XFR19WD/ref=pd_sbs_309_6?_encoding=UTF8&pd_rd_i=B06XFR19WD&pd_rd_r=0RAMAAAEM8WDPM03F26G&pd_rd_w=lgqeY&pd_rd_wg=VIMEu&refRID=0RAMAAAEM8WDPM03F26G
            //https://www.amazon.com/NMD-XR1-Color-Black-Running-BA7231/dp/B01NANDZCG/ref=pd_sbs_309_6?_encoding=UTF8&pd_rd_i=B01NANDZCG&pd_rd_r=NR7XFSB9P10ENADKNJTH&pd_rd_w=s98uu&pd_rd_wg=8e6sK&refRID=NR7XFSB9P10ENADKNJTH
            //https://www.amazon.com/adidas-Originals-NMD-R2-Primeknit-BA7252/dp/B06XXM5HSD/ref=pd_sbs_309_1?_encoding=UTF8&pd_rd_i=B06XXM5HSD&pd_rd_r=6RN0CJRVRHJ8YHNSD258&pd_rd_w=MUV5Y&pd_rd_wg=uEu6E&refRID=6RN0CJRVRHJ8YHNSD258
            string url = "https://www.amazon.com/adidas-Originals-NMD-R2-Primeknit-BA7252/dp/B06XXM5HSD/ref=pd_sbs_309_1?_encoding=UTF8&pd_rd_i=B06XXM5HSD&pd_rd_r=6RN0CJRVRHJ8YHNSD258&pd_rd_w=MUV5Y&pd_rd_wg=uEu6E&refRID=6RN0CJRVRHJ8YHNSD258";
            string html = wch.GetHtmlString(url, headers, null);

            ProductScraper scraper = new ProductScraper(url, html);
            scraper.Parse();

            //Print Result
            if (scraper.Product != null)
            {
                foreach (KeyValuePair<string,string> item in scraper.Product)
                    Console.WriteLine(string.Format("{0} : {1}", item.Key, item.Value));

                Console.Read();
            }

        }
    }

    public class WebClientHelper : System.Net.WebClient
    {
        public string GetHtmlString(string url, WebHeaderCollection headers, WebProxy proxy = null)
        {
            string html = string.Empty;
            try
            {
                this.Headers = headers;
                this.Proxy = proxy;
                html = this.DownloadString(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return html;
        }
    }

    public class Scraper
    {
        public string url { get; private set; }
        public string html { get; private set; }
        public HtmlDocument html_doc { get; private set; }
        public Dictionary<string, string> Product { get; set; }

        public Scraper(string url, string source_data)
        {
            this.url = url;
            this.html_doc = new HtmlDocument();
            this.html_doc.LoadHtml(source_data);
            this.html_doc.OptionFixNestedTags = true;
            this.Product = new Dictionary<string, string> {
                { "url", url }
            };
        }

        public string GetValueByXpath(contentType returnType, string xPath)
        {
            string value = string.Empty;
            try
            {
                HtmlNode html_node = this.html_doc.DocumentNode.SelectSingleNode(xPath);
                if (html_node != null)
                    value = (returnType == contentType.InnerText) ? html_node.InnerText : html_node.InnerHtml;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            
            return value.Trim();
        }

        public HtmlNodeCollection GetHtmlNodesByXpath(string xPath)
        {
            try
            {
                return this.html_doc.DocumentNode.SelectNodes(xPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public string GetAttrValueByXpath(string attrName, string xPath)
        {
            string value = string.Empty;
            try
            {
                HtmlNode html_node = this.html_doc.DocumentNode.SelectSingleNode(xPath);
                if (html_node != null && html_node.HasAttributes
                    && html_node.Attributes[attrName] != null
                    && !string.IsNullOrEmpty(html_node.Attributes[attrName].Value))
                    value = html_node.Attributes[attrName].Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return value.Trim();
        }

        public virtual string GetProductImages(string xPath)
        {
            string value = string.Empty;
            try
            {
                HtmlNodeCollection html_nodes = this.html_doc.DocumentNode.SelectNodes(xPath);
                if (html_nodes != null) {
                    List<string> img_list = new List<string>();
                    foreach (HtmlNode item in html_nodes)
                    {
                        if (item != null && item.HasAttributes && item.Attributes["src"] != null)
                            img_list.Add(item.Attributes["src"].Value);
                    }
                    value = string.Join(",", img_list);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return value;
        }

        public virtual string GetProductShippingWeight(string xPath)
        {
            string value = string.Empty;
            try
            {
                HtmlNodeCollection nodes = this.GetHtmlNodesByXpath(xPath);
                if (nodes == null) return value;

                var data = nodes.Where(hn => hn.InnerText.ToLower().Contains("shipping weight")).FirstOrDefault();
                if (data != null && data.GetType().Name.Equals("HtmlNode"))
                {
                    Match match = Regex.Match(data.InnerText, @"(\d+(\.\d{1,2})?) (lb.|lbs|lb|pounds|pound|kgs|kg|kilogram|oz|ounce|liter|g.|gm|grams|gram)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (match != null && match.Success)
                        value = match.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return value;
        }
    }

    public class ProductScraper: Scraper
    {

        public ProductScraper(string url, string html): base(url, html)
        { }

        public void Parse()
        {   
            //Get Product Title
            Product.Add("title", this.GetValueByXpath(contentType.InnerText, "//span[@id='productTitle']"));

            //Get Product Description
            Product.Add("description", this.GetValueByXpath(contentType.InnerHtml, "//div[@id='feature-bullets']"));

            //Get Product Price
            Product.Add("price", this.GetValueByXpath(contentType.InnerText, "//span[@id='priceblock_ourprice']"));

            //Get Product Thumb Images
            Product.Add("images", this.GetProductImages("//div[@id='altImages']/ul/li//img"));

            //Get Product Shipping Weight
            Product.Add("shipping_weight", this.GetProductShippingWeight("//div[@id='detailBullets_feature_div']//ul/li"));
        }
    }

}
