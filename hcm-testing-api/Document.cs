using System;

namespace hcm_testing_api
{
    public record Document
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Owner { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

        public override string ToString() => $"{Id}: {Title} (Created: {CreatedAt}, Updated: {UpdatedAt})";
    }
}