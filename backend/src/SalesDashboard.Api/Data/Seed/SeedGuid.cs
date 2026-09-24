using Bogus;

namespace SalesDashboard.Api.Data.Seed;

/// <summary>
/// Builds deterministic UUID v7 values (RFC 9562) from a seeded <see cref="Randomizer"/>.
/// The framework <c>Guid.CreateVersion7()</c> uses the wall clock and a cryptographic RNG, so re-running
/// the seed would produce new ids; this reproduces the same ids for the same run day (D13). The 48-bit
/// timestamp keeps ids ordered by the moment they encode — for a sale that moment is its <c>SoldAt</c>.
/// </summary>
internal static class SeedGuid
{
    /// <param name="timestamp">The instant encoded into the leading 48 bits (Unix milliseconds).</param>
    /// <param name="random">The seeded source for the 74 random bits; advances the sequence deterministically.</param>
    public static Guid Create(DateTimeOffset timestamp, Randomizer random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var unixMs = timestamp.ToUnixTimeMilliseconds();

        Span<byte> bytes = stackalloc byte[16];

        // Bytes 0-5: 48-bit Unix-milliseconds timestamp, big-endian.
        bytes[0] = (byte)(unixMs >> 40);
        bytes[1] = (byte)(unixMs >> 32);
        bytes[2] = (byte)(unixMs >> 24);
        bytes[3] = (byte)(unixMs >> 16);
        bytes[4] = (byte)(unixMs >> 8);
        bytes[5] = (byte)unixMs;

        // Bytes 6-15: random, then overwrite the version and variant bit fields.
        random.Bytes(10).CopyTo(bytes[6..]);

        // Byte 6 high nibble = version 7.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);

        // Byte 8 top two bits = variant 0b10.
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, bigEndian: true);
    }
}
