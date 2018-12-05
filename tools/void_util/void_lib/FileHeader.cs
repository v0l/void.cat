using System;
using System.Collections.Generic;
using System.Text;

namespace void_lib
{


    public class FileHeader
    {
        public string name { get; set; }
        public string mime { get; set; }
        public ulong len { get; set; }
    }
}
