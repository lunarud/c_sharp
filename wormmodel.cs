using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Security.Cryptography;
using System.Text;

namespace WormStore.Models
{
    public class WormDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("document_type")]
        public string DocumentType { get; set; }

        [BsonElement("data_hash")]
        public string DataHash { get; set; }

        [BsonElement("data_blob")]
        public byte[] DataBlob { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("created_by")]
        public string CreatedBy { get; set; }

        [BsonElement("metadata")]
        public BsonDocument Metadata { get; set; }

        [BsonElement("access_control")]
        public BsonDocument AccessControl { get; set; }

        [BsonElement("legal_hold")]
        public bool LegalHold { get; set; }

        [BsonElement("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [BsonElement("retention_policy_id")]
        public Guid RetentionPolicyId { get; set; }

        [BsonElement("read_count")]
        public int ReadCount { get; set; } = 0;

        [BsonElement("last_accessed_at")]
        public DateTime? LastAccessedAt { get; set; }

        [BsonElement("immutable_id")]
        public Guid? ImmutableId { get; set; }

        // Method to compute data hash (SHA-256) from the data blob
        public void ComputeHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                DataHash = BitConverter.ToString(sha256.ComputeHash(DataBlob)).Replace("-", "").ToLower();
            }
        }

        // Method to validate the document fields
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DocumentType) &&
                   !string.IsNullOrEmpty(CreatedBy) &&
                   DataBlob != null && DataBlob.Length > 0 &&
                   RetentionPolicyId != Guid.Empty;
        }
    }
}