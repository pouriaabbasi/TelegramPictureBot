namespace TelegramPhotoBot.Domain.Entities;

/// <summary>
/// Records when a model accepts the platform's terms and conditions.
/// Stores the exact content and timestamp for legal purposes.
/// </summary>
public class ModelTermsAcceptance : BaseEntity
{
    /// <summary>
    /// The model who accepted the terms
    /// </summary>
    public Guid ModelId { get; private set; }
    
    /// <summary>
    /// Navigation property to Model
    /// </summary>
    public virtual Model Model { get; private set; } = null!;
    
    /// <summary>
    /// Exact date and time when terms were accepted (UTC)
    /// </summary>
    public DateTime AcceptedAt { get; private set; }
    
    /// <summary>
    /// Version of the terms (e.g., "1.0", "1.1")
    /// </summary>
    public string TermsVersion { get; private set; } = null!;
    
    /// <summary>
    /// Full content of the terms that were shown and accepted
    /// Stored for legal proof and audit trail
    /// </summary>
    public string TermsContent { get; private set; } = null!;
    
    /// <summary>
    /// Whether this is the latest version the model has accepted
    /// Set to false when a newer version is accepted
    /// </summary>
    public bool IsLatestVersion { get; private set; }
    
    /// <summary>
    /// Optional notes or metadata
    /// </summary>
    public string? Notes { get; private set; }

    // Private constructor for EF Core
    private ModelTermsAcceptance() { }

    /// <summary>
    /// Create a new terms acceptance record
    /// </summary>
    public ModelTermsAcceptance(
        Guid modelId,
        string termsVersion,
        string termsContent)
    {
        if (string.IsNullOrWhiteSpace(termsVersion))
            throw new ArgumentException("Terms version cannot be empty", nameof(termsVersion));
        
        if (string.IsNullOrWhiteSpace(termsContent))
            throw new ArgumentException("Terms content cannot be empty", nameof(termsContent));

        ModelId = modelId;
        AcceptedAt = DateTime.UtcNow;
        TermsVersion = termsVersion;
        TermsContent = termsContent;
        IsLatestVersion = true;
    }

    /// <summary>
    /// Mark this acceptance as outdated (when a newer version is accepted)
    /// </summary>
    public void MarkAsOldVersion()
    {
        IsLatestVersion = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Add notes to this acceptance record
    /// </summary>
    public void AddNotes(string notes)
    {
        Notes = notes;
        MarkAsUpdated();
    }
}
