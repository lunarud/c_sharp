const mongoose = require('mongoose');

const versionedRecordSchema = new mongoose.Schema({
    originalId: { type: String, required: true },  // Unique ID for the original document
    version: { type: Number, required: true },      // Version number
    data: { type: Object, required: true },         // Document data
    createdAt: { type: Date, required: true, default: Date.now },
    locked: { type: Boolean, default: true },       // Immutability flag
    author: { type: String, required: true },
    auditTrail: [{                                  // Tracks access and modifications
        action: String,
        timestamp: { type: Date, default: Date.now },
        userId: String
    }]
});

// Define indexes for fast retrieval
versionedRecordSchema.index({ originalId: 1, version: -1 }); // Retrieve latest version quickly

const VersionedRecord = mongoose.model('VersionedRecord', versionedRecordSchema);