using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Wyhb.Joe.Common;

namespace Wyhb.Joe.Selenium
{
    public class WebDriverExt
    {
        private static IWebDriver driver;
        private static WebDriverWait wait;
        public static TimeSpan Timeout = TimeSpan.FromSeconds(10);
        public static TimeSpan Interval = TimeSpan.FromSeconds(5);

        public static IWebDriver CreateInstance(BrowserName browserName)
        {
            switch (browserName)
            {
                case BrowserName.None:
                    throw new ArgumentException(string.Format("Not Definition. BrowserName:{0}", browserName));

                case BrowserName.Chrome:
                    driver = new ChromeDriver();
                    wait = new WebDriverWait(driver, Timeout);
                    break;

                case BrowserName.Firefox:
                    var driverService = FirefoxDriverService.CreateDefaultService();
                    driverService.FirefoxBinaryPath = @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe";
                    driverService.HideCommandPromptWindow = true;
                    driverService.SuppressInitialDiagnosticInformation = true;
                    driver = new FirefoxDriver(driverService);
                    wait = new WebDriverWait(driver, Timeout);
                    break;

                case BrowserName.InternetExplorer:
                    driver = new InternetExplorerDriver();
                    wait = new WebDriverWait(driver, Timeout);
                    break;

                case BrowserName.Edge:
                    driver = new EdgeDriver();
                    wait = new WebDriverWait(driver, Timeout);
                    break;

                case BrowserName.Safari:
                    driver = new SafariDriver();
                    wait = new WebDriverWait(driver, Timeout);
                    break;

                default:
                    throw new ArgumentException(string.Format("Not Definition. BrowserName:{0}", browserName));
            }
            return driver;
        }

        public static void TakeFullScreenshot(string path, string testCase)
        {
            Directory.CreateDirectory(path);
            var bounds = Screen.GetBounds(Point.Empty);
            using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                bitmap.Save(path + Const.STR_YEN_MARK + testCase + Const.STR_UNDERSCORE + DateTime.Now.ToString(Const.FORMAT_YYYYMMDD_HHMMSSFFF) + Const.STR_DOT + Const.STR_PNG, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        public static void ScrollToTop()
        {
            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("window.scrollTo(0, 0);");
        }

        public static void ScrollToDown()
        {
            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        }

        public static void MoveToElement(Point point)
        {
            var js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("window.scrollTo(" + point.X + "," + point.Y + ");");
        }

        public static void TakeScreenshot(string path, string testCase)
        {
            Directory.CreateDirectory(path);
            GetEntireScreenshot().Save(path + Const.STR_YEN_MARK + testCase + Const.STR_UNDERSCORE + DateTime.Now.ToString(Const.FORMAT_YYYYMMDD_HHMMSSFFF) + Const.STR_DOT + Const.STR_PNG, System.Drawing.Imaging.ImageFormat.Png);
        }

        public static Image GetEntireScreenshot()
        {
            var totalWidth = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return document.body.scrollWidth");
            var totalHeight = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return document.body.scrollHeight");
            var viewportWidth = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return window.innerWidth");
            var viewportHeight = (int)(long)((IJavaScriptExecutor)driver).ExecuteScript("return window.innerHeight");

            if (totalWidth < viewportWidth)
            {
                totalWidth = viewportWidth;
            }
            if (totalHeight < viewportHeight)
            {
                totalHeight = viewportHeight;
            }
            if (totalWidth <= viewportWidth && totalHeight <= viewportHeight)
            {
                ScrollToTop();
                return ScreenshotToImage(driver.TakeScreenshot());
            }
            var rectangles = new List<Rectangle>();
            for (var y = 0; y < totalHeight; y += viewportHeight)
            {
                var newHeight = viewportHeight;
                if (y + viewportHeight > totalHeight)
                {
                    newHeight = totalHeight - y;
                }
                for (var x = 0; x < totalWidth; x += viewportWidth)
                {
                    var newWidth = viewportWidth;
                    if (x + viewportWidth > totalWidth)
                    {
                        newWidth = totalWidth - x;
                    }
                    rectangles.Add(new Rectangle(x, y, newWidth, newHeight));
                }
            }
            var stitchedImage = new Bitmap(totalWidth, totalHeight);
            var previous = Rectangle.Empty;
            foreach (var rectangle in rectangles)
            {
                if (previous != Rectangle.Empty)
                {
                    var xDiff = rectangle.Right - previous.Right;
                    var yDiff = rectangle.Bottom - previous.Bottom;
                    ((IJavaScriptExecutor)driver).ExecuteScript(String.Format("window.scrollBy({0}, {1})", xDiff, yDiff));
                }
                var screenshot = driver.TakeScreenshot();
                var screenshotImage = ScreenshotToImage(screenshot);
                var sourceRectangle = new Rectangle(viewportWidth - rectangle.Width, viewportHeight - rectangle.Height, rectangle.Width, rectangle.Height);
                using (var graphics = Graphics.FromImage(stitchedImage))
                {
                    graphics.DrawImage(screenshotImage, rectangle, sourceRectangle, GraphicsUnit.Pixel);
                }
                previous = rectangle;
            }
            ScrollToTop();
            return stitchedImage;
        }

        public static IWebElement ElementExists(string xpathToFind)
        {
            var by = By.XPath(xpathToFind);
            IWebElement element = null;
            var timeSpan = TimeSpan.FromSeconds(1);
            while (timeSpan < Timeout)
            {
                element = wait.Until(ExpectedConditions.ElementExists(by));
                if (element.Enabled && element.Displayed)
                {
                    timeSpan = timeSpan + Timeout;
                    MoveToElement(((RemoteWebElement)element).LocationOnScreenOnceScrolledIntoView);
                }
                else
                {
                    timeSpan.Add(TimeSpan.FromSeconds(1));
                }
            }
            return element;
        }

        public static IWebElement ElementToBeClickable(string xpathToFind)
        {
            var by = By.XPath(xpathToFind);
            IWebElement element = null;
            var timeSpan = TimeSpan.FromSeconds(1);
            while (timeSpan < Timeout)
            {
                element = wait.Until(ExpectedConditions.ElementToBeClickable(by));
                if (element.Enabled && element.Displayed)
                {
                    timeSpan = timeSpan + Timeout;
                    MoveToElement(((RemoteWebElement)element).LocationOnScreenOnceScrolledIntoView);
                }
                else
                {
                    timeSpan.Add(TimeSpan.FromSeconds(1));
                }
            }
            return element;
        }

        public static void Polling(TimeSpan timeSpan)
        {
            Thread.Sleep(timeSpan);
        }

        private static bool TryFindElement(string xpathToFind, out IWebElement element)
        {
            var rtn = true;
            element = null;
            try
            {
                element = wait.Until(ExpectedConditions.ElementExists(By.XPath(xpathToFind)));
            }
            catch (NoSuchElementException ex)
            {
                rtn = false;
            }
            return rtn;
        }

        private static bool IsElementVisible(IWebElement element)
        {
            return element.Displayed && element.Enabled;
        }

        private static Image ScreenshotToImage(Screenshot screenshot)
        {
            Image screenshotImage;
            using (var memStream = new MemoryStream(screenshot.AsByteArray))
            {
                screenshotImage = Image.FromStream(memStream);
            }
            return screenshotImage;
        }
    }
}