using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace ERX.Utils
{
    public class CDATA : IXmlSerializable
    {
        // Fields
        private string text;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CDATA()
        {
        }

        public CDATA(string text)
        {
            this.text = text;
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            this.text = reader.ReadElementContentAsString();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteCData(this.text);
        }

        //属性
        public string Text
        {
            get
            {
                return this.text;
            }
        }
    }


}
