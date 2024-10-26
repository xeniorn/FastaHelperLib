using System.Text;

namespace FastaHelperLib;

/// <summary>
/// Helper functions for handling and importing fasta files
/// Fasta format based on definition in https://github.com/wrpearson/fasta36/blob/162df0860991d9bd39b8be76f9337d3f8bfac51e/doc/fasta_guide.pdf
/// With additional extensions, allowing arbitrary characters within the sequence, which will be preserved if special flags are used
/// - e.g. - . : * ... are commonly used in alignments, : and / in alphafold
/// Briefly, fasta format comprises one or more fasta entries
/// A fasta entry is defined by:
/// - a single header line starting with the > symbol
/// - zero or more comment lines both before and after the header, starting with # or ;
/// - any number of sequence lines that can contain any symbol, except they cannot start with >,#,;
/// Stipulations
/// - Sequence and header can't be empty
/// - Comments "below" a sequence are treated as an invalid headerless entry
/// - whitespace is generally ignored, treated like any other character
/// </summary>
public static class FastaHelper
{
// const string FastaHeaderSplitPattern="\n>";
    public const string FastaHeaderSymbol = @">";
    public const string FastaCommentSymbol_Ladder = @"#";
    public const string FastaCommentSymbol_Semicolon = @";";

    public static async IAsyncEnumerable<ParseFastaPartialResult> ProcessMultiFastaStream(
        Stream fastaStream,
        SequenceType sequenceType, 
        string keepExtraCharacters = "")
    {
        var comments = new List<string>();
        var header = String.Empty;
        var sequenceParts = new List<string>();

        var hasHeaderLine = false;
        var hasSequenceLines = false;

        var reader = new StreamReader(fastaStream);

        var firstLine = await reader.ReadLineAsync();
        var isEmpty = (firstLine is null);

        if (isEmpty) yield break;

        var nextLine = firstLine!;
        bool isLastLine = false;

        while (!isLastLine)
        {
            var line = nextLine!;
            nextLine = await reader.ReadLineAsync();
            isLastLine = (nextLine is null);

            ParseFastaPartialResult MakeResult(SequenceType seqType, string keepChars, string header,
                List<string> sequenceLines, List<string> commentLines)
            {
                var newEntry = FastaHelper.GenerateEntry(seqType, header, string.Join("", sequenceLines),
                    commentLines, keepChars);

                //hasSequence doesn't know if any of these lines are valid sequences until it's parsed in the new entry
                if (hasHeaderLine && hasSequenceLines && newEntry.Sequence.Length > 0)
                {
                    return new ParseFastaPartialResult(true, newEntry);
                }
                else
                {
                    return new ParseFastaPartialResult(false, newEntry);
                }
            }

            var isCommentLine = line.StartsWith(FastaCommentSymbol_Ladder) ||
                                line.StartsWith(FastaCommentSymbol_Semicolon);
            var isHeaderLine = line.StartsWith(FastaHeaderSymbol);

            if (isHeaderLine || isCommentLine)
            {
                //resolve existing entry and open a new one
                if (isHeaderLine && hasHeaderLine || isCommentLine && hasSequenceLines)
                {
                    var result = MakeResult(sequenceType, keepExtraCharacters, header, sequenceParts, comments);
                    yield return result;

                    hasSequenceLines = false;
                    hasHeaderLine = false;

                    comments = new();
                    sequenceParts = new();
                    header = string.Empty;
                }

                if (isHeaderLine)
                {
                    hasHeaderLine = true;
                    header = line;
                }
                else if (isCommentLine)
                {
                    comments.Add(line);
                }
                else
                {
                    throw new Exception("Unreachable");
                }
            }
            else // not a special line, so it's a sequence line
            {
                sequenceParts.Add(line);
                hasSequenceLines = true;
            }

            //must save the last one, there will be no further loops
            if (isLastLine)
            {
                yield return MakeResult(sequenceType, keepExtraCharacters, header, sequenceParts, comments);
            }
        }

        yield break;
    }

    public static MultiFastaProcessResult ProcessMultiFasta(string text, SequenceType sequenceType,  string keepCharacters = "")
    {
        var lines = FastaHelper.RectifyNewlines(text).Split("\n");

        //var entries = text1.Split(FastaHeaderSplitPattern);

        var validEntries = new List<FastaEntry>();
        var invalidEntries = new List<FastaEntry>();
        var comments = new List<string>();
        var header = String.Empty;
        var sequenceParts= new List<string>();
        
        var hasHeaderLine = false;
        var hasSequenceLines = false;
        
        var counter = 0;
        var totalLines = lines.Length;

        foreach (var line in lines)
        {
            void ProcessIntoEntryAndSave(SequenceType sequenceType1, string keepChars, string header1, List<string> list, List<string> comments1, List<FastaEntry> fastaEntries,
                List<FastaEntry> invalidEntries1)
            {
                // make sure to create a new list, so that the mutable list isn't passed by reference!
                var newEntry = FastaHelper.GenerateEntry(sequenceType1, header1, String.Join("", list),
                    new List<string>(comments1), keepChars);

                //hasSequence doesn't know if any of these lines are valid sequences until it's parsed in the new entry
                if (hasHeaderLine && hasSequenceLines && newEntry.Sequence.Length > 0)
                {
                    fastaEntries.Add(newEntry);
                }
                else
                {
                    invalidEntries1.Add(newEntry);
                }
            }

            counter++;
            var isCommentLine = line.StartsWith(FastaCommentSymbol_Ladder) ||
                                line.StartsWith(FastaCommentSymbol_Semicolon);
            var isHeaderLine = line.StartsWith(FastaHeaderSymbol);
            var isLastLine = counter == totalLines;

            if (isHeaderLine || isCommentLine)
            {
                //resolve existing entry and open a new one
                if (isHeaderLine && hasHeaderLine || isCommentLine && hasSequenceLines)
                {
                    ProcessIntoEntryAndSave(sequenceType, keepCharacters, header, sequenceParts, comments, validEntries, invalidEntries);

                    hasSequenceLines = false;
                    hasHeaderLine = false;

                    // safer to make new ones than clear, even if performance is worse
                    comments = new();
                    sequenceParts = new();
                    header = String.Empty;
                }

                if (isHeaderLine)
                {
                    hasHeaderLine = true;
                    header = line;
                }
                else if (isCommentLine)
                {
                    comments.Add(line);
                }
                else
                {
                    throw new Exception("Unreachable");
                }
            }
            else // not a special line, so it's a sequence line
            {
                sequenceParts.Add(line);
                hasSequenceLines = true;
            }

            if (isLastLine)
            {
                ProcessIntoEntryAndSave(sequenceType, keepCharacters, header, sequenceParts, comments, validEntries, invalidEntries);
            }

        }

        var res = new MultiFastaProcessResult() { ValidFastas = validEntries, InvalidFastas = invalidEntries };

        return res;

    }

    private static FastaEntry GenerateEntry(SequenceType sequenceType, string header, string sequence, IEnumerable<string> comments, string keepCharacters)
    {
        return FastaEntry.Generate(sequenceType, header, sequence, comments, keepCharacters);
    }

    private static string RectifyNewlines(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static bool IsValidFasta(string text)
    {
        return true;
    }

    public static List<FastaEntry>? GetFastaEntriesIfValid(string text, SequenceType protein, string keepCharacters = "")
    {
        if (!FastaHelper.IsValidFasta(text)) return null;

        var multiFastaProcResult = FastaHelper.ProcessMultiFasta(text, protein, keepCharacters);

        if (!multiFastaProcResult.ValidFastas.Any()) return null;

        return multiFastaProcResult.ValidFastas;
            
    }

    public static async Task<List<FastaEntry>?> GetFastaEntriesIfValidAsync(string? fastaString, SequenceType sequenceType,
        string keepCharacters = "")
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fastaString ?? string.Empty));
        return await GetFastaEntriesIfValidAsync(stream, sequenceType, keepCharacters);
    }

    public static async Task<List<FastaEntry>?> GetFastaEntriesIfValidAsync(Stream fastaStream, SequenceType sequenceType, string keepCharacters = "")
    {
        try
        {
            var multiFastaProcResult = await FastaHelper.ProcessMultiFastaAsync(fastaStream, sequenceType, keepCharacters);

            if (!multiFastaProcResult.ValidFastas.Any()) return null;

            return multiFastaProcResult.ValidFastas;

        }
        catch (Exception ex)
        {
            return null;
        }
    }

    private static async Task<MultiFastaProcessResult> ProcessMultiFastaAsync(Stream fastaStream, SequenceType sequenceType, string keepCharacters)
    {
        //var entries = text1.Split(FastaHeaderSplitPattern);

        var validEntries = new List<FastaEntry>();
        var invalidEntries = new List<FastaEntry>();
        var comments = new List<string>();
        var header = String.Empty;
        var sequenceParts = new List<string>();

        var hasHeaderLine = false;
        var hasSequenceLines = false;
        
        var reader = new StreamReader(fastaStream);
        
        var firstLine = await reader.ReadLineAsync();
        var isEmpty = (firstLine is null);

        if (isEmpty) return new MultiFastaProcessResult();

        var nextLine = firstLine!;
        bool isLastLine = false;

        while (!isLastLine)
        {
            var line = nextLine!;
            nextLine = await reader.ReadLineAsync();
            isLastLine = (nextLine is null);
            
            void ProcessIntoEntryAndSave(SequenceType seqType, string keepChars, string header,
                List<string> sequenceLines, List<string> commentLines, ref List<FastaEntry> validEntries,
                ref List<FastaEntry> invalidEntries)
            {
                var newEntry = FastaHelper.GenerateEntry(seqType, header, String.Join("", sequenceLines),
                    commentLines, keepChars);

                //hasSequence doesn't know if any of these lines are valid sequences until it's parsed in the new entry
                if (hasHeaderLine && hasSequenceLines && newEntry.Sequence.Length > 0)
                {
                    validEntries.Add(newEntry);
                }
                else
                {
                    invalidEntries.Add(newEntry);
                }
            }
            
            var isCommentLine = line.StartsWith(FastaCommentSymbol_Ladder) ||
                                line.StartsWith(FastaCommentSymbol_Semicolon);
            var isHeaderLine = line.StartsWith(FastaHeaderSymbol);

            if (isHeaderLine || isCommentLine)
            {
                //resolve existing entry and open a new one
                if (isHeaderLine && hasHeaderLine || isCommentLine && hasSequenceLines)
                {
                    ProcessIntoEntryAndSave(sequenceType, keepCharacters, header, sequenceParts, comments, ref validEntries,
                        ref invalidEntries);

                    hasSequenceLines = false;
                    hasHeaderLine = false;

                    comments = new();
                    sequenceParts = new();
                    header = String.Empty;
                }

                if (isHeaderLine)
                {
                    hasHeaderLine = true;
                    header = line;
                }
                else if (isCommentLine)
                {
                    comments.Add(line);
                }
                else
                {
                    throw new Exception("Unreachable");
                }
            }
            else // not a special line, so it's a sequence line
            {
                sequenceParts.Add(line);
                hasSequenceLines = true;
            }

            //must save the last one, there will be no further loops
            if (isLastLine)
            {
                ProcessIntoEntryAndSave(sequenceType, keepCharacters, header, sequenceParts, comments, ref validEntries,
                    ref invalidEntries);
            }
        }

        var res = new MultiFastaProcessResult() { ValidFastas = validEntries, InvalidFastas = invalidEntries };
        return res;
    }

    public static async IAsyncEnumerable<string> GenerateMultiFastaDataAsync(List<Protein> proteinBatch)
    {
        foreach (var protein in proteinBatch)
        {
            var fasta = FastaEntry.GenerateFrom(protein);
            yield return fasta.ToString()! ?? string.Empty;
        }
    }

    public static FastaEntry GetFastaEntryFromStandardSingleFastaRapid(string fastaString,
        SequenceType sequenceType)
    {
        var lines = fastaString.Split('\n');
        var header = lines.First();
        var sequence = String.Join("", lines.Skip(1));
        return GenerateRapidEntry(sequenceType, header, sequence);
    }

    private static FastaEntry GenerateRapidEntry(SequenceType sequenceType, string header, string sequence)
    {
        return RapidFastaEntry.Generate(sequenceType, header, sequence);
    }
}

public record ParseFastaPartialResult(bool Success, FastaEntry Fasta);

