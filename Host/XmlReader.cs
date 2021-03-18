using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Net;

namespace Host
{
    class XmlReader
    {
        public XmlDocument XmlDoc;

        public XmlReader(string PathFile)
        {
            XmlDoc = new XmlDocument();
            XmlDoc.Load(PathFile);
        }

        public int GetNumberOfItems(string name)
        {
            return XmlDoc.GetElementsByTagName(name).Count;
        }

        public string GetElementValue(int number, string name)
        {
            return XmlDoc.GetElementsByTagName(name).Item(number).InnerText;
        }

        public string GetAttributeValue(int number, string ElementName, string AttributeName)
        {
            XmlNode Node = XmlDoc.GetElementsByTagName(ElementName).Item(number);
            return Node.Attributes.GetNamedItem(AttributeName).InnerText;
        }
        
        public List<DistantHosts> GetItemsForSelectedDistantHost(string hostname, string name, string ipaddress)
        {
            List<DistantHosts> hostlist = new List<DistantHosts>();
            XmlNodeList items = XmlDoc.SelectNodes("//host[@NAME ='" + hostname + "']//DISTANTHOST");

            foreach (XmlNode xn in items)
            {
                DistantHosts distant = new DistantHosts();
                distant.DistantIPAddress = IPAddress.Parse(xn.Attributes[ipaddress].InnerText);
                distant.DistantName = xn.Attributes[name].InnerText;
                hostlist.Add(distant);
            }
            return hostlist;
        }
    }
}
