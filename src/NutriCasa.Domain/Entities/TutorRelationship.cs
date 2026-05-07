using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class TutorRelationship : AuditableEntity
{
    public Guid MinorUserId { get; set; }
    public Guid TutorUserId { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string? MinorIdDocumentUrl { get; set; }
    public string? MinorIdDocumentType { get; set; }
    public string? TutorIdDocumentUrl { get; set; }
    public string? TutorIdDocumentType { get; set; }
    public string? ParentageProofUrl { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ConsentSignedAt { get; set; }
    public DateTime? ConsentRevokedAt { get; set; }

    public User MinorUser { get; set; } = null!;
    public User TutorUser { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}
