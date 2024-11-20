db.records.aggregate([
  // Sort the records globally by `id` and `timestamp`
  { 
    $sort: { id: 1, timestamp: -1 } 
  },
  // Group by `id` and collect the top 2 records for each group
  { 
    $group: { 
      _id: "$id",
      topRecords: { $push: "$$ROOT" } // Collect all records for each `id`
    }
  },
  // Slice the top 2 records for each group
  { 
    $project: { 
      _id: 1, 
      topRecords: { $slice: ["$topRecords", 2] } 
    }
  }
]);