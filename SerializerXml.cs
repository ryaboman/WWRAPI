using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace WWRAPI {
    public class SerializerXml : ISerializable {
        public void Deserialize<DType>(string str, out DType data) {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DType));
            using (TextReader reader = new StringReader(str)) {
                data = (DType)xmlSerializer.Deserialize(reader);
            }
        }

        public string Serialize<DType>(DType data) {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DType));
            var xml = "";
            using (var sww = new StringWriter()) {
                using (XmlWriter writer = XmlWriter.Create(sww)) {
                    xmlSerializer.Serialize(writer, data);
                    xml = sww.ToString();
                }
            }
            return xml;
        }
    }
}
