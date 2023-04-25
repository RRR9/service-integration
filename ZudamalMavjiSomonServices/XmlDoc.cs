using System.Xml.Linq;

namespace ZudamalMavjiSomonServices
{
    static class XmlDoc
    {

        static public string CreateCardnoRequest(string cardno)
        {
            string result;
            string s;
            s = new XDocument(
                new XElement("business", new XAttribute("id", 6001),
                    new XElement("body",
                        new XElement("cardno", cardno)))
            ).ToString();
            result = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + s;
            return result;
        }

        static public string CreatePaymentRequest(string cardno, string paymentId, string providerSum)
        {
            string result = "";
            string s = "";
            s = new XDocument(
                new XElement("business", new XAttribute("id", "6005"),
                    new XElement("body",
                        new XElement("orderinfo", new XAttribute("cardno", cardno),
                        new XElement("product", new XAttribute("prdcode", "1"), new XAttribute("ordnum", ""), new XAttribute("pay", (decimal.Parse(providerSum) * 100).ToString().Replace(",", "."))
                        )),
                        new XElement("total", (decimal.Parse(providerSum) * 100).ToString().Replace(",", ".")),
                        new XElement("tradeno", paymentId)
                    )
                )
            ).ToString();
            result = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + s;
            return result;
        }

    }
}
