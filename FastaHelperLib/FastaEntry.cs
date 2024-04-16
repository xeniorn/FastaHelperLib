using System.CodeDom.Compiler;
using System.Xml.Linq;

namespace FastaHelperLib;

public class RapidFastaEntry : FastaEntry
{
    public static RapidFastaEntry Generate(SequenceType sequenceType, string header, string sequenceString)
    {
        var a = new RapidFastaEntry(sequenceType) { Header = header };
        a.Sequence = a.ParseSequenceString(sequenceString);
        return a;
    }

    protected RapidFastaEntry(SequenceType sequenceType) : base(sequenceType)
    {
    }


}

public class FastaEntry : IComparable<FastaEntry>
{
    private string _header = String.Empty;
    public const char HeaderSymbol = '>';

    protected FastaEntry(SequenceType sequenceType, string keepCharacters = "")
    {
        switch (sequenceType)
        {
            case SequenceType.Protein:
                AlphabetSymbols = "ACDEFGHIKLMNPQRSTVWXYacdefghiklmnpqrstvwxy";
                break; ;
            case SequenceType.Dna:
                AlphabetSymbols = "ACGTacgt";
                break;

            case SequenceType.Rna:
                AlphabetSymbols = "ACGTacgt";
                break;

            case SequenceType.Other:
                AlphabetSymbols = "";
                break;

            default:
                throw new NotImplementedException("Sequence type not implemented");
        }
        ExtraSymbols = keepCharacters;
    }

    public string AlphabetSymbols { get; init; }
    public List<string> Comments { get; set; } = new List<string>();
    public string ExtraSymbols { get; init; }

    private string _headerWithoutSymbol;
    /// <summary>
    /// Will always trim start and end whitespace on set
    /// Handles inputs with and without the header symbol included the same way
    /// </summary>
    public string Header
    {
        get => $"{HeaderSymbol}{_headerWithoutSymbol}";
        set => _headerWithoutSymbol = value.TrimStart().TrimStart(HeaderSymbol).Trim();
    }
    
    /// <summary>
    /// Will always trim start and end whitespace on set
    /// Handles inputs with and without the header symbol included the same way
    /// </summary>
    public string HeaderWithoutSymbol
    {
        get => _headerWithoutSymbol;
        set => _headerWithoutSymbol = value.TrimStart().TrimStart(HeaderSymbol).Trim();
    }

    public string Sequence { get; set; } = String.Empty;
    public SequenceType SequenceType { get; init; }
    public static FastaEntry Generate(SequenceType sequenceType, string header, string sequenceString, IEnumerable<string>? comments = null, string keepCharacters = "")
    {
        var a = new FastaEntry(sequenceType, keepCharacters) { Header = header, Comments = new List<string>(comments ?? Enumerable.Empty<string>()) };
        a.Sequence = a.ParseSequenceString(sequenceString);
        return a;
    }

    public static FastaEntry GenerateFrom(Protein protein)
    {
        return new FastaEntry(SequenceType.Protein)
        {
            Header = protein.Id,
            Sequence = protein.Sequence
        };
    }

    protected string ParseSequenceString(string sequenceString)
    {
        var allowedSymbols = AlphabetSymbols.ToCharArray().Concat(ExtraSymbols.ToCharArray()).Distinct();
        var str = string.Concat(sequenceString.Where(x => allowedSymbols.Contains(x)));
        return str ?? string.Empty;
    }

    public int CompareTo(FastaEntry? other)
    {
        if (other is null) return -1;
        if (this.Equals(other)) return 0;

        if (this.Sequence.Length > other.Sequence.Length) return -1;
        if (this.Sequence.Length < other.Sequence.Length) return 1;

        //equal length
        return String.Compare(this.Sequence, other.Sequence, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        var fastaWithoutComments = $"{Header}{Environment.NewLine}{Sequence}{Environment.NewLine}";

        if (Comments.Any())
        {
            var commentLines = String.Join(Environment.NewLine, Comments.Select(x => $"{DefaultCommentSymbol}{x}"));
            return $"{commentLines}{Environment.NewLine}{fastaWithoutComments}";
        }
        else
        {
            return fastaWithoutComments;
        }
    }

    public const string DefaultCommentSymbol = "#";


    public static FastaEntry CloneFrom(FastaEntry source)
    {
        var a = new FastaEntry(source.SequenceType, source.ExtraSymbols)
        {
            Header = source.Header,
            Sequence = source.Sequence,
            Comments = source.Comments,
        };

        return a;
    }

    public static FastaEntry? TruncatedCloneFrom(FastaEntry source, int startResidueIndex, int endResidueIndex, bool renameHeader = true)
    {
        if (startResidueIndex > source.Sequence.Length) return null;
        if (endResidueIndex < 1 ) return null;
        if (endResidueIndex < startResidueIndex) return null;

        if (startResidueIndex < 1) startResidueIndex = 1;
        if (endResidueIndex > source.Sequence.Length) endResidueIndex = source.Sequence.Length;

        var truncSpecifier = $"[{startResidueIndex}-{endResidueIndex}]";
        var finalLen = 1 + endResidueIndex - startResidueIndex;

        var a = new FastaEntry(source.SequenceType, source.ExtraSymbols)
        {
            Header = renameHeader ? $"{source.Header}{truncSpecifier}" : source.Header,
            Sequence = source.Sequence.Substring(startResidueIndex-1, finalLen),
            Comments = source.Comments,
        };

        return a;
    }
}