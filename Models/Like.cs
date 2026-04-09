using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class Like : Record
    {
        public int UserID { get; set; }
        public int MediaID { get; set; }

    }
}