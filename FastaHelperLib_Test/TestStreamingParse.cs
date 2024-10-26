using System.Text;
using FastaHelperLib;

namespace FastaHelperLib_Test;

public class TestStreamingParse
{
    [Fact]
    public async Task Works()
    {
        var fastaContents = string.Join("\n",
            @">header1",
            @"ACDEFGHIKLMNPQRSTVWY",
            @"",
            @">header2",
            @"ACDEFGHIKLMNPQRSTVWYY",
            @"");

        var buffer = Encoding.ASCII.GetBytes(fastaContents);
        using var stream = new MemoryStream(buffer);

        var res = new List<FastaEntry>();

        await foreach (var x in FastaHelper.ProcessMultiFastaStream(stream, SequenceType.Protein))
        {
            res.Add(x.Fasta);
        }

        Assert.Equal(2, res.Count);

        Assert.Equal("header1", res[0].HeaderWithoutSymbol);
        Assert.Equal("header2", res[1].HeaderWithoutSymbol);

        Assert.Equal("ACDEFGHIKLMNPQRSTVWY", res[0].Sequence);
        Assert.Equal("ACDEFGHIKLMNPQRSTVWYY", res[1].Sequence);
    }
}