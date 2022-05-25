using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WCFServiceWebRole1
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        public bool Create(string login, string password)
        {
            CloudTable table = RetrieveCloudTable("users");

            TableOperation checkIfUserExistsOp = TableOperation.Retrieve<User>(login, password);
            var existsRes = table.Execute(checkIfUserExistsOp);
            User u = (User)existsRes.Result;

            if(u != null)
            {
                return false;
            }

            var user = new User(login, password) { Login = login, Password = password, SessionId = Guid.Empty };

            var insertOp = TableOperation.Insert(user);
            var insertRes = table.Execute(insertOp);
            var iRes = insertRes.Result;

            if(iRes == null)
            {
                return false;
            }

            return true;
        }

        public Guid Login(string login, string password)
        {
            CloudTable table = RetrieveCloudTable("users");

            TableOperation checkIfUserExistsOp = TableOperation.Retrieve<User>(login, password);
            var existsRes = table.Execute(checkIfUserExistsOp);
            User user = (User)existsRes.Result;

            if(user == null)
            {
                return Guid.Empty;
            }

            var sessionId = Guid.NewGuid();
            user.SessionId = sessionId;

            var loginOp = TableOperation.Replace(user);
            table.Execute(loginOp);

            return sessionId;
        }

        public bool Logout(string login)
        {
            CloudTable table = RetrieveCloudTable("users");

            TableQuery<User> query = new TableQuery<User>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, login));

            var queryRes = table.ExecuteQuery(query);
            var user = queryRes.SingleOrDefault();

            if(user == null)
            {
                return false;
            }

            user.SessionId = Guid.Empty;

            var logoutOp = TableOperation.Replace(user);
            table.Execute(logoutOp);

            return true;
        }

        public bool Put(string name, string contents, Guid sessionId)
        {
            CloudTable table = RetrieveCloudTable("users");
            CloudBlobContainer blobContainer = RetrieveBlobContainer("files");

            TableQuery<User> query = new TableQuery<User>()
                .Where(TableQuery.GenerateFilterConditionForGuid("SessionId", QueryComparisons.Equal, sessionId));

            var queryRes = table.ExecuteQuery(query);
            var user = queryRes.SingleOrDefault();

            if (user == null)
            {
                return false;
            }

            var blobName = user.Login + "-" + name;
            var blob = blobContainer.GetBlockBlobReference(blobName);

            if (blob == null)
            {
                return false;
            }

            var bytes = new ASCIIEncoding().GetBytes(contents);
            var stream = new System.IO.MemoryStream(bytes);
            blob.UploadFromStream(stream);

            return true;
        }

        public string Get(string name, Guid sessionId)
        {
            CloudTable table = RetrieveCloudTable("users");
            CloudBlobContainer blobContainer = RetrieveBlobContainer("files");
            
            TableQuery<User> query = new TableQuery<User>()
                .Where(TableQuery.GenerateFilterConditionForGuid("SessionId", QueryComparisons.Equal, sessionId));

            var queryRes = table.ExecuteQuery(query);
            var user = queryRes.SingleOrDefault();

            if (user == null)
            {
                return string.Empty;
            }

            var blobName = user.Login + "-" + name;
            var blob = blobContainer.GetBlockBlobReference(blobName);

            if (blob == null)
            {
                return string.Empty;
            }

            var stream = new System.IO.MemoryStream();
            blob.DownloadToStream(stream);
            string contents = Encoding.UTF8.GetString(stream.ToArray());

            return contents;
        }

        private CloudTable RetrieveCloudTable(string name)
        {
            var account = CloudStorageAccount.DevelopmentStorageAccount;
            CloudTableClient tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference(name);
            table.CreateIfNotExists();

            return table;
        }

        private CloudBlobContainer RetrieveBlobContainer(string name)
        {
            var account = CloudStorageAccount.DevelopmentStorageAccount;
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(name);
            blobContainer.CreateIfNotExists();

            return blobContainer;
        }

        public bool DeleteBlob(string name)
        {
            CloudBlobContainer blobContainer = RetrieveBlobContainer(name);
            var blob = blobContainer.GetBlockBlobReference(name);
            blob.DeleteIfExists();

            return true;
        }

        public bool DeleteTable(string name)
        {
            var table = RetrieveCloudTable(name);
            table.DeleteIfExists();

            return true;
        }

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }
    }
}
