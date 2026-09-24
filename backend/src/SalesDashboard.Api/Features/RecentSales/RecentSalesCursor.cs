using System.Buffers.Binary;
using System.Buffers.Text;
using System.Runtime.InteropServices;

namespace SalesDashboard.Api.Features.RecentSales;

/// <summary>
/// The opaque keyset cursor for the recent-sales feed (RECENT-SALES): the <c>(sold_at, id)</c> of the last row
/// of a page. Encoded as 8 bytes of UTC ticks plus the 16-byte id, Base64Url so it is URL-safe and carries no
/// visible meaning. Decoding is total — any malformed input is rejected rather than throwing.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RecentSalesCursor(DateTime SoldAt, Guid Id)
{
    private const int Size = sizeof(long) + 16;

    /// <summary>Encodes the cursor to its opaque Base64Url string.</summary>
    public string Encode()
    {
        Span<byte> bytes = stackalloc byte[Size];
        BinaryPrimitives.WriteInt64BigEndian(bytes, SoldAt.Ticks);
        Id.TryWriteBytes(bytes[sizeof(long)..]);
        return Base64Url.EncodeToString(bytes);
    }

    /// <summary>Parses an opaque cursor; returns <see langword="false"/> for anything malformed.</summary>
    public static bool TryDecode(string value, out RecentSalesCursor cursor)
    {
        cursor = default;

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[Size];
        if (!Base64Url.TryDecodeFromChars(value, bytes, out var written) || written != Size)
        {
            return false;
        }

        var ticks = BinaryPrimitives.ReadInt64BigEndian(bytes);
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            return false;
        }

        cursor = new RecentSalesCursor(
            new DateTime(ticks, DateTimeKind.Utc),
            new Guid(bytes[sizeof(long)..]));
        return true;
    }
}
