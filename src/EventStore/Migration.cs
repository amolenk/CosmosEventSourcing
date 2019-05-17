using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace EventStore
{
    public class Migration
    {
        private readonly DocumentClient _client;
        private readonly string _database;
        private readonly string _container;

        public Migration(DocumentClient client, string database, string container)
        {
            _client = client;
            _database = database;
            _container = container;
        }

        public async Task RunAsync()
        {
            await DeleteStoredProcedureAsync("spAppendToStream");
            await CreateStoredProcedureAsync("spAppendToStream");
        }

        private async Task CreateStoredProcedureAsync(string sprocId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(_database, _container);

            StoredProcedure newStoredProcedure = new StoredProcedure
            {
                Id = sprocId,
                Body = File.ReadAllText($@"js/{sprocId}.js")
            };


            await _client.CreateStoredProcedureAsync(uri, newStoredProcedure);
        }

        private async Task DeleteStoredProcedureAsync(string sprocId)
        {
            var uri = UriFactory.CreateStoredProcedureUri(_database, _container, sprocId);

            try
            {
                await _client.DeleteStoredProcedureAsync(uri);
            }
            catch (DocumentClientException ex) when (ex.Error.Code == "NotFound")
            {
            }
        }
    }
}