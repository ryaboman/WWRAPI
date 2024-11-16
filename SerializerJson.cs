using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WWRAPI {
    public class SerializerJson : ISerializable{
        
        public void Deserialize<DType>(string str, out DType data) {
            data = JsonConvert.DeserializeObject<DType>(str);
        }

        public string Serialize<DType>(DType data) {
            string json = JsonConvert.SerializeObject(data);
            return json;
        }
    }
}
