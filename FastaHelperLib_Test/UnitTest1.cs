using System.Collections;
using FastaHelperLib;

namespace FastaHelperLib_Test
{

    public class ValidMultiFastaDataWithComments : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                "#comment 1\n# comment 2\n>header1\nACGTQ\n; comment 3##\n#comment 4\n;\n>header2\nACGTQTR\n>header3\nACGTQTRA",
                new List<(string header, string sequence, List<string> comments)>
                {
                    (@"header1", @"ACGTQ", new List<string>(){"#comment 1", "# comment 2"}),
                    (@"header2", @"ACGTQTR", new List<string>(){"; comment 3##", "#comment 4",";"}),
                    (@"header3", @"ACGTQTRA", new List<string>(){}),
                }
            };
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
}