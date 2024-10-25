using System.Collections;
using FastaHelperLib;

namespace FastaHelperLib_Test;


public class ValidMultiFastaDataForExport : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            FastaEntry.Generate(
                SequenceType.Protein,
                @">header1",
                @"ACDEFGHIKLMNPQRSTVWY"),
            "\n",
            null,
            string.Join("\n",
                @">header1",
                @"ACDEFGHIKLMNPQRSTVWY",
                @"")
        };


        yield return new object[]
        {
            FastaEntry.Generate(
                SequenceType.Protein,
                @">header1",
                @"ACDEFGHIKLMNPQRSTVWY"),
            "\n",
            5,
            string.Join("\n",
                @">header1",
                @"ACDEF",
                @"GHIKL",
                @"MNPQR",
                @"STVWY",
                @"")
        };

        yield return new object[]
        {
            FastaEntry.Generate(
                SequenceType.Protein,
                @">header1",
                @"ACDE"),
            "\n",
            5,
            string.Join("\n",
                @">header1",
                @"ACDE",
                @"")
        };

        yield return new object[]
        {
            FastaEntry.Generate(
                SequenceType.Protein,
                @">header1",
                @"ACDE"),
            "\n",
            4,
            string.Join("\n",
                @">header1",
                @"ACDE",
                @"")
        };

        yield return new object[]
        {
            FastaEntry.Generate(
                SequenceType.Protein,
                @">header1",
                @"ACDE"),
            "\n",
            3,
            string.Join("\n",
                @">header1",
                @"ACD",
                @"E",
                @"")
        };

    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class TestOuptut
{
    [Theory]
    [ClassData(typeof(ValidMultiFastaDataForExport))]
    public void GenerateFastaString(FastaEntry entry, string newLine, int? splitLines, string expected)
    {
        var res = entry.ToString(newLine, splitLines, false);

        Assert.Equal(expected, res);

    }
}

public class TestFastaHelper
{
    [Theory]
    [ClassData(typeof(ValidMultiFastaData))]
    public async Task StandardFastaInputsReturnValidResults(string fastaText, IList<(string header, string sequence)> expectedResults)
    {
        var res = await FastaHelper.GetFastaEntriesIfValidAsync(fastaText, SequenceType.Protein, keepCharacters: string.Empty);

        Assert.NotNull(res);
        Assert.Equal(expectedResults.Count, res.Count);

        foreach (var ((header, sequence), resultEntry) in expectedResults.Zip(res))
        {
            Assert.Equal(sequence, resultEntry.Sequence);
            Assert.Equal(header, resultEntry.HeaderWithoutSymbol);
        }
    }


    [Theory]
    [ClassData(typeof(ValidMultiFastaDataWithComments))]
    public async Task StandardFastaInputsWithCommentsReturnValidResults(string fastaText, IList<(string header, string sequence, List<string> comments)> expectedResults)
    {
        var res = await FastaHelper.GetFastaEntriesIfValidAsync(fastaText, SequenceType.Protein, keepCharacters: string.Empty);

        Assert.NotNull(res);
        Assert.Equal(expectedResults.Count, res.Count);

        foreach (var ((header, sequence, comments), resultEntry) in expectedResults.Zip(res))
        {
            Assert.Equal(sequence, resultEntry.Sequence);
            Assert.Equal(header, resultEntry.HeaderWithoutSymbol);
                
            Assert.Equal(comments.Count, resultEntry.Comments.Count);
            foreach (var (expected, resultComment) in comments.Zip(resultEntry.Comments))
            {
                Assert.Equal(expected, resultComment);
            }
        }
    }
        
}