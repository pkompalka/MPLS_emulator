using System.Xml;


namespace Node
{
    public class XmlReader
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
        
        public XmlNodeList GetNameOfType(string name)
        {
            return XmlDoc.GetElementsByTagName(name);
        }

        public string GetAttributeValue(int number, string ElementName, string AttributeName)
        {
            XmlNode Node = XmlDoc.GetElementsByTagName(ElementName).Item(number);
            return Node.Attributes.GetNamedItem(AttributeName).InnerText;
        }
    }
}
