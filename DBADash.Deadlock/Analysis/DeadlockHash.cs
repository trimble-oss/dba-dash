using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// The occurrence identity of a deadlock: a SHA-256 of the graph, truncated to <see cref="Bytes"/>.  Stored as
    /// dbo.Deadlocks.DeadlockHash by the collector, and against an AI analysis so the viewer can prefer an answer
    /// about this exact deadlock over one about the same pattern - see <see cref="DeadlockSignature"/> for the
    /// opposite, grouping, identity.
    ///
    /// <para>Hashes <see cref="DeadlockGraph.Xml"/>, which the parser produces by re-serialising the element it
    /// parsed.  That round trip is what makes this stable: whitespace between elements is dropped on the way in, so
    /// the same deadlock read through the event file and through the ring buffer produces the same bytes.  Every
    /// attribute value survives it, which is what keeps two distinct deadlocks apart.</para>
    ///
    /// <para>The normalisation reaches formatting only.  Text inside <c>inputbuf</c> and <c>frame</c> is the
    /// statement itself and is preserved verbatim, trailing spaces included - so if a future read path were to
    /// return that text padded differently, the same deadlock would hash differently.  Nothing does today, and the
    /// alternative - trimming statement text before hashing - would throw away part of what distinguishes one
    /// occurrence from another.</para>
    /// </summary>
    public static class DeadlockHash
    {
        /// <summary>Width of the hash in bytes - dbo.Deadlocks.DeadlockHash is BINARY(16).</summary>
        public const int Bytes = 16;

        public static byte[] Compute(DeadlockGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);
            return Compute(graph.Xml);
        }

        public static byte[] Compute(string? graphXml)
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes(graphXml ?? string.Empty));
            return digest[..Bytes];
        }

        /// <summary>The "0x..." hex form, which CONVERT(BINARY(16), @value, 1) reads.</summary>
        public static string ToHex(byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            return "0x" + Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture);
        }
    }
}
