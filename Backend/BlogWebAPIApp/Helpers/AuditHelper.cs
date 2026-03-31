using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace BlogWebAPIApp.Helpers
{
    /// <summary>
    /// Builds AuditLog entries from EF ChangeTracker entries.
    /// Called inside SaveChangesAsync before the actual save so we can
    /// capture OldValues for Modified/Deleted entries.
    /// </summary>
    public static class AuditHelper
    {
        // Navigation / shadow properties we never want to serialise
        private static readonly HashSet<string> _ignoredProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "PasswordHash", "ConcurrencyStamp", "SecurityStamp"
        };

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Snapshot tracked entries BEFORE SaveChanges is called.
        /// Returns a list of partially-built AuditLog objects (NewValues for
        /// Added entries is filled in after the save when PKs are generated).
        /// </summary>
        public static List<AuditLog> SnapshotChanges(
            IEnumerable<EntityEntry> entries,
            Guid?   userId,
            string? ipAddress,
            string? userAgent)
        {
            var logs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                // Skip the audit log itself to avoid infinite recursion
                if (entry.Entity is AuditLog) continue;

                // Only track meaningful state changes
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;

                var entityName = entry.Entity.GetType().Name;
                var action     = entry.State switch
                {
                    EntityState.Added    => AuditActions.Create,
                    EntityState.Modified => AuditActions.Update,
                    EntityState.Deleted  => AuditActions.Delete,
                    _                    => "Unknown"
                };

                var log = new AuditLog
                {
                    UserId     = userId,
                    Action     = action,
                    EntityName = entityName,
                    EntityId   = GetPrimaryKey(entry),
                    IpAddress  = ipAddress,
                    UserAgent  = userAgent,
                    Timestamp  = DateTime.UtcNow,
                    Status     = AuditStatus.Success,
                    Description = $"{action} on {entityName}"
                };

                // Capture old values for Modified / Deleted
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                    log.OldValues = SerializeProperties(entry.Properties, useOriginal: true);

                // For Added: NewValues will be set after save (PK may be DB-generated)
                // For Modified: capture current (new) values now
                if (entry.State is EntityState.Modified)
                    log.NewValues = SerializeChangedProperties(entry.Properties);

                logs.Add(log);
            }

            return logs;
        }

        /// <summary>
        /// After SaveChanges, fill in NewValues for Added entries (PK is now known).
        /// </summary>
        public static void FillAddedNewValues(
            List<AuditLog> pendingLogs,
            IEnumerable<EntityEntry> entries)
        {
            var addedEntries = entries
                .Where(e => e.State == EntityState.Unchanged && e.Entity is not AuditLog)
                .ToDictionary(e => e.Entity);

            foreach (var log in pendingLogs.Where(l => l.Action == AuditActions.Create))
            {
                // Match by entity name — find the corresponding entry
                var entry = addedEntries.Values
                    .FirstOrDefault(e => e.Entity.GetType().Name == log.EntityName);

                if (entry is null) continue;

                log.EntityId  = GetPrimaryKey(entry);
                log.NewValues = SerializeProperties(entry.Properties, useOriginal: false);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string GetPrimaryKey(EntityEntry entry)
        {
            var keyValues = entry.Metadata.FindPrimaryKey()
                ?.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "")
                .ToArray();

            return keyValues is { Length: > 0 }
                ? string.Join("|", keyValues)
                : "unknown";
        }

        /// <summary>Serialise all scalar properties, skipping ignored ones and navigations.</summary>
        private static string SerializeProperties(
            IEnumerable<PropertyEntry> properties,
            bool useOriginal)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                if (_ignoredProperties.Contains(prop.Metadata.Name)) continue;
                if (prop.Metadata.IsKey() && useOriginal) continue; // skip PK in old values

                var value = useOriginal ? prop.OriginalValue : prop.CurrentValue;
                dict[prop.Metadata.Name] = value;
            }

            return JsonSerializer.Serialize(dict, _jsonOpts);
        }

        /// <summary>For Modified entries: only serialise properties that actually changed.</summary>
        private static string SerializeChangedProperties(IEnumerable<PropertyEntry> properties)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                if (_ignoredProperties.Contains(prop.Metadata.Name)) continue;
                if (!prop.IsModified) continue;

                dict[prop.Metadata.Name] = prop.CurrentValue;
            }

            return dict.Count > 0
                ? JsonSerializer.Serialize(dict, _jsonOpts)
                : "{}";
        }
    }
}
