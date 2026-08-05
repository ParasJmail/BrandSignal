namespace BrandSignal.Domain.Entities;

public class Campaign
{
    // 1. The Barcode (Identity)
    // Automatically creates a unique random tracking number for this case file.
    public Guid Id {get; set;} = Guid.NewGuid();

    // 2. The Instructions (Inputs)
    // The name of the company and what search phrase we are auditing.
    public string CompanyName {get; set;} = string.Empty;
    public string TargetKeyword {get; set;} = string.Empty;

    // 3. The State (Tracking)
    // Tells the frontend what to show: "Pending", "Processing", "Completed", or "Failed"
    public string Status {get; set;} = "pending";

    // 4. The Empty Pockets (AI Outputs)
    // These start blank and get filled in by the Node.js detective later.
    public int VisibilityScore { get; set; } = 0; // Will hold the percentage (e.g., 40)
    public string AuditSummary { get; set; } = string.Empty; // Will hold the AI explanation text

    // 5. The Timeline (Clocks)
    // Keeps track of when the user requested it and when the AI finished.
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    // The '?' means it stays empty (null) until the audit is completely done.
    public DateTime? AuditedAt {get;set;}

    // Navigation property: One Campaign can have multiple Audit Reports over time
    public ICollection<AuditReport> AuditReports { get; set; } = new List<AuditReport>();
}
