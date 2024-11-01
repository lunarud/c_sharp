versionedRecordSchema.pre('save', function(next) {
    if (this.locked) {
        return next(new Error("Cannot modify a locked document"));
    }
    next();
});