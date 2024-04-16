using System.Collections;

namespace FastaHelperLib_Test;

public class ValidMultiFastaData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            ">header1\nACGTQ",
            new List<(string header, string sequence)>
            {
                (@"header1", @"ACGTQ")
            }
        };

        yield return new object[]
        {
            ">header1\nACGTQ\n>header2\nACGTQTR",
            new List<(string header, string sequence)>
            {
                (@"header1", @"ACGTQ"),
                (@"header2", @"ACGTQTR"),
            }
        };

        yield return new object[]
        {
            "\n\n\n>header1\nACGTQ\n\n\n\n>header2\nACGTQTR\n\n\n",
            new List<(string header, string sequence)>
            {
                (@"header1", @"ACGTQ"),
                (@"header2", @"ACGTQTR"),
            }
        };

        yield return new object[]
        {
            "\n\n\n>header1\nACG\nTQ\n\n\n\n>header2\nA\nC\nG\nT\nQ\nT\nR\n\n\n",
            new List<(string header, string sequence)>
            {
                (@"header1", @"ACGTQ"),
                (@"header2", @"ACGTQTR"),
            }
        };

    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}