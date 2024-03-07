using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using team3.Models;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Linq;

namespace team3.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index(string location = "")
    {
        if (string.IsNullOrEmpty(location))
        {

            return View();
        }

        using (HttpClient client = new HttpClient())
        {
            try
            {
                ViewData["Location"] = location;

                //API address variable
                String URLString = @"https://opendata.fmi.fi/wfs?service=WFS&version=2.0.0&request=getFeature&storedquery_id=fmi%3A%3Aforecast%3A%3Aedited%3A%3Aweather%3A%3Ascandinavia%3A%3Apoint%3A%3Atimevaluepair&place=" + location.Trim().ToLower() + "&fbclid=IwAR0WkICmGBIoxd2uOLMAy5Jeh4UD1nuB1TRyz83oGNuKY6KToeJ_kYRXWyM";


                // Call API by http method
                HttpResponseMessage response = await client.GetAsync(URLString);
                response.EnsureSuccessStatusCode(); // Throw an exception if the status code is not a success code.
                string content = await response.Content.ReadAsStringAsync();

                XmlReader xmlReader = XmlReader.Create(new StringReader(content));


                // Necessary variables
                string currTempurature = "mts-1-1-Temperature";
                string currWindSpeed = "mts-1-1-windspeedms";
                DateTime now = new DateTime();
                XElement el = new XElement("root");
                float temper = 0;
                float wind = 0;

                // While loop through currently data
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "wml2:MeasurementTimeseries")
                    {
                        if (xmlReader.HasAttributes)
                        {
                            if (xmlReader.GetAttribute("gml:id") != null)
                            {
                                string atrr = xmlReader.GetAttribute("gml:id").ToLower().Trim();
                                if (atrr == currWindSpeed.ToLower().Trim())
                                {
                                    el = (XElement)XNode.ReadFrom(xmlReader);
                                    IEnumerable<XElement> names =
                                                (from e in el.Elements()
                                                 select e).ToList();
                                    foreach (XElement n in names)
                                    {
                                        string a = n.Value;
                                        a = a.Replace("\n", "").Replace("\t", "").Trim();

                                        now = DateTime.Parse(a.Split(' ')[0]).AddHours(-4);
                                        int ssnow = now.Hour;
                                        if (ssnow == DateTime.Now.Hour && now.Date == DateTime.Now.Date)
                                        {
                                            wind = float.Parse(a.Split('Z')[1].Trim());
                                            ViewData["DateNow"] = now;
                                            ViewData["WindSpeed"] = a.Split('Z')[1].Trim();
                                        }
                                    }

                                }
                                
                                if (atrr == currTempurature.ToLower().Trim())
                                {
                                    el = (XElement)XNode.ReadFrom(xmlReader);
                                    IEnumerable<XElement> names =
                                                (from e in el.Elements()
                                                 select e).ToList();
                                    foreach (XElement n in names)
                                    {
                                        string a = n.Value;
                                        a = a.Replace("\n", "").Replace("\t", "").Trim();

                                        now = DateTime.Parse(a.Split(' ')[0]).AddHours(-4);
                                        int ssnow = now.Hour;
                                        if (ssnow == DateTime.Now.Hour && now.Date == DateTime.Now.Date)
                                        {
                                            temper = float.Parse(a.Split('Z')[1].Trim());
                                            ViewData["DateNow"] = now;
                                            ViewData["Temperature"] = a.Split('Z')[1].Trim();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Feel like calculation 
                ViewData["FeelLike"] = 13.2 + 0.6215 * temper - 11.37 * Math.Pow(wind, 0.16) + 0.3965 * temper * Math.Pow(wind, 0.16);
                xmlReader.Close();
            }
            catch (Exception ex)
            {
                ViewData["Error"] = "Not Found";
            }
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}