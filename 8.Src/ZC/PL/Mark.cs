using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PLC;

namespace PL
{

    public class Mark
    {
        //private string p;

        public Mark(string address)
        {
            // TODO: Complete member initialization
            //this.p = p;
        }
        public ItemDefine ItemDefine { get; set; }
        public bool IsMarked
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
