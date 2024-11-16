using System;
using System.Collections.Generic;
using System.Text;

namespace WWRAPI {
    public interface ISerializable {
        void Deserialize<DType>(string str, out DType data);
        string Serialize<DType>(DType data);
    }
}
