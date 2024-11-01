using MongoDB.Driver;
using WormStore.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WormStore.Services
{
    public class WormStoreService
    {
        private readonly IMongoCollection<WormDocument> _wormDocuments;

        public WormStoreService(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _wormDocuments = database.GetCollection<WormDocument>(settings.Value.CollectionName);
        }

        // Create
        public async Task<WormDocument> CreateAsync(WormDocument document)
        {
            if (!document.IsValid())
                throw new ArgumentException("Invalid document data. Ensure all required fields are populated.");

            document.ComputeHash();
            await _wormDocuments.InsertOneAsync(document);
            return document;
        }

        // Read (Get by Id)
        public async Task<WormDocument> GetByIdAsync(string id)
        {
            var document = await _wormDocuments.Find(doc => doc.Id == id).FirstOrDefaultAsync();
            if (document != null)
            {
                document.ReadCount++;
                document.LastAccessedAt = DateTime.UtcNow;
                await UpdateAsync(document.Id, document);
            }
            return document;
        }

        // Update (limited to specific fields)
        public async Task UpdateAsync(string id, WormDocument documentIn)
        {
            var document = await _wormDocuments.Find(doc => doc.Id == id).FirstOrDefaultAsync();
            if (document == null || document.LegalHold)
                throw new InvalidOperationException("Cannot update a document under legal hold or if it doesn't exist.");

            document.ReadCount = documentIn.ReadCount;
            document.LastAccessedAt = documentIn.LastAccessedAt;

            await _wormDocuments.ReplaceOneAsync(doc => doc.Id == id, document);
        }

        // Delete (only if legal hold is false and retention period expired)
        public async Task DeleteAsync(string id)
        {
            var document = await _wormDocuments.Find(doc => doc.Id == id && !doc.LegalHold && doc.ExpiryDate <= DateTime.UtcNow).FirstOrDefaultAsync();
            if (document == null)
                throw new InvalidOperationException("Cannot delete document under legal hold or if retention period has not expired.");

            await _wormDocuments.DeleteOneAsync(doc => doc.Id == id);
        }

        // List All Documents
        public async Task<List<WormDocument>> GetAllAsync()
        {
            return await _wormDocuments.Find(doc => true).ToListAsync();
        }
    }
}