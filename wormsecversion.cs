versionedRecordSchema.pre('save', function(next) {
    if (this.locked) {
        return next(new Error("Cannot modify a locked document"));
    }
    next();
});

versionedRecordSchema.methods.logAccess = function(action, userId) {
    this.auditTrail.push({
        action,
        userId,
        timestamp: new Date()
    });
    return this.save();
};