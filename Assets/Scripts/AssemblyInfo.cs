using System.Runtime.CompilerServices;

// Supaya test bisa memakai helper internal (misal SetColorsForTest).
[assembly: InternalsVisibleTo("Blocky.Tests")]
[assembly: InternalsVisibleTo("Blocky.PlayTests")]
