using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace WCFServiceWebRole1
{
    public class User : TableEntity
    {
        public User(string pk, string rk)
        {
            PartitionKey = pk;
            RowKey = rk;
        }

        public User() { }
        public string Login { get; set; }
        public string Password { get; set; }
        public Guid SessionId { get; set; }
    }
}