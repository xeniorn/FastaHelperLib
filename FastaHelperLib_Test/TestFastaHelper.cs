using FastaHelperLib;

namespace FastaHelperLib_Test
{
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
}